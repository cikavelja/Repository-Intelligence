using Dapper;
using Microsoft.Data.Sqlite;
using RepositoryIntelligence.Application.Interfaces;
using RepositoryIntelligence.Domain.Enums;
using RepositoryIntelligence.Domain.Models;

namespace RepositoryIntelligence.Infrastructure.Storage;

public sealed class SqliteMetadataStore : IMetadataStore, IDisposable
{
    private readonly string _connectionString;

    public SqliteMetadataStore(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";
    }

    private SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        conn.Execute("PRAGMA journal_mode=WAL;");
        conn.Execute("PRAGMA foreign_keys=ON;");
        return conn;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS RepositoryDocuments (
                Id TEXT PRIMARY KEY,
                RepositoryName TEXT NOT NULL,
                BranchName TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                AbsolutePath TEXT NOT NULL,
                Language TEXT NOT NULL,
                ContentHash TEXT NOT NULL,
                FileSizeBytes INTEGER NOT NULL,
                LastModifiedUtc TEXT NOT NULL,
                IndexedAtUtc TEXT NOT NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                DeletedAtUtc TEXT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS IX_RepoDocs_Repo_Branch_Path
                ON RepositoryDocuments (RepositoryName, BranchName, FilePath);

            CREATE TABLE IF NOT EXISTS CodeChunks (
                Id TEXT PRIMARY KEY,
                RepositoryDocumentId TEXT NOT NULL,
                RepositoryName TEXT NOT NULL,
                BranchName TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                Language TEXT NOT NULL,
                ChunkType INTEGER NOT NULL,
                SymbolName TEXT NOT NULL,
                StartLine INTEGER NOT NULL,
                EndLine INTEGER NOT NULL,
                Text TEXT NOT NULL,
                ContentHash TEXT NOT NULL,
                EmbeddingId TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Chunks_DocId ON CodeChunks (RepositoryDocumentId);
            CREATE INDEX IF NOT EXISTS IX_Chunks_Repo_Branch_Path ON CodeChunks (RepositoryName, BranchName, FilePath);

            CREATE TABLE IF NOT EXISTS CodeSymbols (
                Id TEXT PRIMARY KEY,
                RepositoryDocumentId TEXT NOT NULL,
                RepositoryName TEXT NOT NULL,
                BranchName TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                SymbolName TEXT NOT NULL,
                SymbolType INTEGER NOT NULL,
                Namespace TEXT NOT NULL,
                StartLine INTEGER NOT NULL,
                EndLine INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Symbols_DocId ON CodeSymbols (RepositoryDocumentId);

            CREATE TABLE IF NOT EXISTS CodeDependencies (
                Id TEXT PRIMARY KEY,
                RepositoryName TEXT NOT NULL,
                BranchName TEXT NOT NULL,
                SourceFilePath TEXT NOT NULL,
                SourceSymbol TEXT NOT NULL,
                TargetFilePath TEXT NOT NULL,
                TargetSymbol TEXT NOT NULL,
                DependencyType INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Deps_Source ON CodeDependencies (RepositoryName, BranchName, SourceFilePath);
            """);
    }

    // ---- Repository Documents ----

    public async Task UpsertDocumentAsync(RepositoryDocument doc, CancellationToken cancellationToken = default)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync("""
            INSERT INTO RepositoryDocuments
                (Id, RepositoryName, BranchName, FilePath, AbsolutePath, Language, ContentHash,
                 FileSizeBytes, LastModifiedUtc, IndexedAtUtc, IsDeleted, DeletedAtUtc)
            VALUES
                (@Id, @RepositoryName, @BranchName, @FilePath, @AbsolutePath, @Language, @ContentHash,
                 @FileSizeBytes, @LastModifiedUtc, @IndexedAtUtc, @IsDeleted, @DeletedAtUtc)
            ON CONFLICT(RepositoryName, BranchName, FilePath) DO UPDATE SET
                Id = excluded.Id,
                AbsolutePath = excluded.AbsolutePath,
                Language = excluded.Language,
                ContentHash = excluded.ContentHash,
                FileSizeBytes = excluded.FileSizeBytes,
                LastModifiedUtc = excluded.LastModifiedUtc,
                IndexedAtUtc = excluded.IndexedAtUtc,
                IsDeleted = excluded.IsDeleted,
                DeletedAtUtc = excluded.DeletedAtUtc
            """,
            new
            {
                Id = doc.Id.ToString(),
                doc.RepositoryName,
                doc.BranchName,
                doc.FilePath,
                doc.AbsolutePath,
                doc.Language,
                doc.ContentHash,
                doc.FileSizeBytes,
                LastModifiedUtc = doc.LastModifiedUtc.ToString("O"),
                IndexedAtUtc = doc.IndexedAtUtc.ToString("O"),
                IsDeleted = doc.IsDeleted ? 1 : 0,
                DeletedAtUtc = doc.DeletedAtUtc?.ToString("O")
            });
    }

    public async Task<RepositoryDocument?> GetDocumentByPathAsync(string repositoryName, string branchName, string filePath, CancellationToken cancellationToken = default)
    {
        using var conn = OpenConnection();
        var row = await conn.QuerySingleOrDefaultAsync(
            "SELECT * FROM RepositoryDocuments WHERE RepositoryName=@rn AND BranchName=@bn AND FilePath=@fp",
            new { rn = repositoryName, bn = branchName, fp = filePath });
        return row is null ? null : MapDocument(row);
    }

    public async Task<IReadOnlyList<RepositoryDocument>> GetAllDocumentsAsync(string repositoryName, string branchName, CancellationToken cancellationToken = default)
    {
        using var conn = OpenConnection();
        var rows = await conn.QueryAsync(
            "SELECT * FROM RepositoryDocuments WHERE RepositoryName=@rn AND BranchName=@bn",
            new { rn = repositoryName, bn = branchName });
        return rows.Select(r => (RepositoryDocument)MapDocument(r)).ToList();
    }

    public async Task MarkDocumentDeletedAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync(
            "UPDATE RepositoryDocuments SET IsDeleted=1, DeletedAtUtc=@now WHERE Id=@id",
            new { id = documentId.ToString(), now = DateTime.UtcNow.ToString("O") });
    }

    public async Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync("DELETE FROM RepositoryDocuments WHERE Id=@id", new { id = documentId.ToString() });
    }

    // ---- Chunks ----

    public async Task AddChunksAsync(IReadOnlyList<CodeChunk> chunks, CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0) return;
        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();
        foreach (var c in chunks)
        {
            await conn.ExecuteAsync("""
                INSERT OR REPLACE INTO CodeChunks
                    (Id, RepositoryDocumentId, RepositoryName, BranchName, FilePath, Language,
                     ChunkType, SymbolName, StartLine, EndLine, Text, ContentHash, EmbeddingId)
                VALUES
                    (@Id, @RepositoryDocumentId, @RepositoryName, @BranchName, @FilePath, @Language,
                     @ChunkType, @SymbolName, @StartLine, @EndLine, @Text, @ContentHash, @EmbeddingId)
                """,
                new
                {
                    Id = c.Id.ToString(),
                    RepositoryDocumentId = c.RepositoryDocumentId.ToString(),
                    c.RepositoryName,
                    c.BranchName,
                    c.FilePath,
                    c.Language,
                    ChunkType = (int)c.ChunkType,
                    c.SymbolName,
                    c.StartLine,
                    c.EndLine,
                    c.Text,
                    c.ContentHash,
                    c.EmbeddingId
                }, tx);
        }
        tx.Commit();
    }

    public async Task<IReadOnlyList<CodeChunk>> GetChunksByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        using var conn = OpenConnection();
        var rows = await conn.QueryAsync("SELECT * FROM CodeChunks WHERE RepositoryDocumentId=@id", new { id = documentId.ToString() });
        return rows.Select(r => (CodeChunk)MapChunk(r)).ToList();
    }

    public async Task<IReadOnlyList<CodeChunk>> GetChunksByFilePathAsync(string repositoryName, string branchName, string filePath, CancellationToken cancellationToken = default)
    {
        using var conn = OpenConnection();
        var rows = await conn.QueryAsync(
            "SELECT * FROM CodeChunks WHERE RepositoryName=@rn AND BranchName=@bn AND FilePath=@fp",
            new { rn = repositoryName, bn = branchName, fp = filePath });
        return rows.Select(r => (CodeChunk)MapChunk(r)).ToList();
    }

    public async Task DeleteChunksByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync("DELETE FROM CodeChunks WHERE RepositoryDocumentId=@id", new { id = documentId.ToString() });
    }

    public async Task<CodeChunk?> GetChunkByIdAsync(Guid chunkId, CancellationToken cancellationToken = default)
    {
        using var conn = OpenConnection();
        var row = await conn.QuerySingleOrDefaultAsync("SELECT * FROM CodeChunks WHERE Id=@id", new { id = chunkId.ToString() });
        return row is null ? null : MapChunk(row);
    }

    // ---- Symbols ----

    public async Task AddSymbolsAsync(IReadOnlyList<CodeSymbol> symbols, CancellationToken cancellationToken = default)
    {
        if (symbols.Count == 0) return;
        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();
        foreach (var s in symbols)
        {
            await conn.ExecuteAsync("""
                INSERT OR REPLACE INTO CodeSymbols
                    (Id, RepositoryDocumentId, RepositoryName, BranchName, FilePath,
                     SymbolName, SymbolType, Namespace, StartLine, EndLine)
                VALUES
                    (@Id, @RepositoryDocumentId, @RepositoryName, @BranchName, @FilePath,
                     @SymbolName, @SymbolType, @Namespace, @StartLine, @EndLine)
                """,
                new
                {
                    Id = s.Id.ToString(),
                    RepositoryDocumentId = s.RepositoryDocumentId.ToString(),
                    s.RepositoryName,
                    s.BranchName,
                    s.FilePath,
                    s.SymbolName,
                    SymbolType = (int)s.SymbolType,
                    s.Namespace,
                    s.StartLine,
                    s.EndLine
                }, tx);
        }
        tx.Commit();
    }

    public async Task DeleteSymbolsByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync("DELETE FROM CodeSymbols WHERE RepositoryDocumentId=@id", new { id = documentId.ToString() });
    }

    // ---- Dependencies ----

    public async Task AddDependenciesAsync(IReadOnlyList<CodeDependency> dependencies, CancellationToken cancellationToken = default)
    {
        if (dependencies.Count == 0) return;
        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();
        foreach (var d in dependencies)
        {
            await conn.ExecuteAsync("""
                INSERT OR REPLACE INTO CodeDependencies
                    (Id, RepositoryName, BranchName, SourceFilePath, SourceSymbol,
                     TargetFilePath, TargetSymbol, DependencyType)
                VALUES
                    (@Id, @RepositoryName, @BranchName, @SourceFilePath, @SourceSymbol,
                     @TargetFilePath, @TargetSymbol, @DependencyType)
                """,
                new
                {
                    Id = d.Id.ToString(),
                    d.RepositoryName,
                    d.BranchName,
                    d.SourceFilePath,
                    d.SourceSymbol,
                    d.TargetFilePath,
                    d.TargetSymbol,
                    DependencyType = (int)d.DependencyType
                }, tx);
        }
        tx.Commit();
    }

    public async Task DeleteDependenciesBySourceFileAsync(string repositoryName, string branchName, string filePath, CancellationToken cancellationToken = default)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync(
            "DELETE FROM CodeDependencies WHERE RepositoryName=@rn AND BranchName=@bn AND SourceFilePath=@fp",
            new { rn = repositoryName, bn = branchName, fp = filePath });
    }

    // ---- Repository-level ----

    public async Task ClearRepositoryAsync(string repositoryName, string branchName, CancellationToken cancellationToken = default)
    {
        using var conn = OpenConnection();
        using var tx = conn.BeginTransaction();
        await conn.ExecuteAsync("DELETE FROM CodeDependencies WHERE RepositoryName=@rn AND BranchName=@bn", new { rn = repositoryName, bn = branchName }, tx);
        await conn.ExecuteAsync("DELETE FROM CodeSymbols WHERE RepositoryName=@rn AND BranchName=@bn", new { rn = repositoryName, bn = branchName }, tx);
        await conn.ExecuteAsync("DELETE FROM CodeChunks WHERE RepositoryName=@rn AND BranchName=@bn", new { rn = repositoryName, bn = branchName }, tx);
        await conn.ExecuteAsync("DELETE FROM RepositoryDocuments WHERE RepositoryName=@rn AND BranchName=@bn", new { rn = repositoryName, bn = branchName }, tx);
        tx.Commit();
    }

    public async Task<IndexingSummary> GetIndexingSummaryAsync(string repositoryName, string branchName, CancellationToken cancellationToken = default)
    {
        using var conn = OpenConnection();
        var totalDocs = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM RepositoryDocuments WHERE RepositoryName=@rn AND BranchName=@bn",
            new { rn = repositoryName, bn = branchName });
        var deletedDocs = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM RepositoryDocuments WHERE RepositoryName=@rn AND BranchName=@bn AND IsDeleted=1",
            new { rn = repositoryName, bn = branchName });
        var totalChunks = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM CodeChunks WHERE RepositoryName=@rn AND BranchName=@bn",
            new { rn = repositoryName, bn = branchName });
        return new IndexingSummary
        {
            AddedFiles = totalDocs - deletedDocs,
            DeletedFiles = deletedDocs,
            AddedChunks = totalChunks
        };
    }

    public void Dispose() => SqliteConnection.ClearAllPools();

    // ---- Mappers ----

    private static RepositoryDocument MapDocument(dynamic r) => new()
    {
        Id = Guid.Parse((string)r.Id),
        RepositoryName = r.RepositoryName,
        BranchName = r.BranchName,
        FilePath = r.FilePath,
        AbsolutePath = r.AbsolutePath,
        Language = r.Language,
        ContentHash = r.ContentHash,
        FileSizeBytes = (long)r.FileSizeBytes,
        LastModifiedUtc = DateTime.Parse(r.LastModifiedUtc),
        IndexedAtUtc = DateTime.Parse(r.IndexedAtUtc),
        IsDeleted = (long)r.IsDeleted == 1,
        DeletedAtUtc = r.DeletedAtUtc is null ? null : DateTime.Parse(r.DeletedAtUtc)
    };

    private static CodeChunk MapChunk(dynamic r) => new()
    {
        Id = Guid.Parse((string)r.Id),
        RepositoryDocumentId = Guid.Parse((string)r.RepositoryDocumentId),
        RepositoryName = r.RepositoryName,
        BranchName = r.BranchName,
        FilePath = r.FilePath,
        Language = r.Language,
        ChunkType = (ChunkType)(int)(long)r.ChunkType,
        SymbolName = r.SymbolName,
        StartLine = (int)(long)r.StartLine,
        EndLine = (int)(long)r.EndLine,
        Text = r.Text,
        ContentHash = r.ContentHash,
        EmbeddingId = r.EmbeddingId
    };
}
