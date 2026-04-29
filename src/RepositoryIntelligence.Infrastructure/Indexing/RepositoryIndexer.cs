using RepositoryIntelligence.Application.Interfaces;
using RepositoryIntelligence.Domain.Enums;
using RepositoryIntelligence.Domain.Models;
using System.Security.Cryptography;
using System.Text;

namespace RepositoryIntelligence.Infrastructure.Indexing;

public sealed class RepositoryIndexer : IRepositoryIndexer
{
    private readonly IFileScanner _fileScanner;
    private readonly IEnumerable<ICodeParser> _parsers;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorSearchService _vectorSearchService;
    private readonly IMetadataStore _metadataStore;

    public RepositoryIndexer(
        IFileScanner fileScanner,
        IEnumerable<ICodeParser> parsers,
        IChunkingService chunkingService,
        IEmbeddingService embeddingService,
        IVectorSearchService vectorSearchService,
        IMetadataStore metadataStore)
    {
        _fileScanner = fileScanner;
        _parsers = parsers;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _vectorSearchService = vectorSearchService;
        _metadataStore = metadataStore;
    }

    public async Task<IndexingSummary> IndexRepositoryAsync(
        IndexRepositoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var start = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(command.RepositoryPath))
            throw new ArgumentException("RepositoryPath cannot be empty.", nameof(command));
        if (!Directory.Exists(command.RepositoryPath))
            throw new ArgumentException($"Repository path does not exist: {command.RepositoryPath}", nameof(command));

        await _metadataStore.InitializeAsync(cancellationToken);

        if (command.Mode == IndexingMode.Full)
        {
            await _metadataStore.ClearRepositoryAsync(command.RepositoryName, command.BranchName, cancellationToken);
        }
        else
        {
            // Reload embeddings into vector store to recover from process restarts.
            // Embeddings are deterministic so they can safely be regenerated from stored text.
            await ReloadEmbeddingsAsync(command, cancellationToken);
        }

        var summary = new IndexingSummary();

        // Load existing indexed docs (for incremental comparison)
        var existingDocs = await _metadataStore.GetAllDocumentsAsync(command.RepositoryName, command.BranchName, cancellationToken);
        var existingByPath = existingDocs
            .Where(d => !d.IsDeleted)
            .ToDictionary(d => d.FilePath, StringComparer.OrdinalIgnoreCase);

        var currentFilePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Scan and process current files
        await foreach (var absolutePath in _fileScanner.ScanAsync(command.RepositoryPath, cancellationToken))
        {
            var relativePath = Path.GetRelativePath(command.RepositoryPath, absolutePath)
                .Replace('\\', '/');

            currentFilePaths.Add(relativePath);

            var content = await File.ReadAllTextAsync(absolutePath, cancellationToken);
            var hash = ComputeHash(content);
            var fileInfo = new FileInfo(absolutePath);

            if (existingByPath.TryGetValue(relativePath, out var existingDoc))
            {
                if (existingDoc.ContentHash == hash)
                {
                    // Unchanged
                    summary.UnchangedFiles++;
                    continue;
                }

                // Count chunks before deleting them (count will be 0 after deletion)
                var chunksToDelete = await _metadataStore.GetChunksByDocumentIdAsync(existingDoc.Id, cancellationToken);
                summary.DeletedChunks += chunksToDelete.Count;

                // Updated file — remove old data
                await _metadataStore.DeleteChunksByDocumentIdAsync(existingDoc.Id, cancellationToken);
                await _metadataStore.DeleteSymbolsByDocumentIdAsync(existingDoc.Id, cancellationToken);
                await _metadataStore.DeleteDependenciesBySourceFileAsync(command.RepositoryName, command.BranchName, relativePath, cancellationToken);
                await _vectorSearchService.DeleteByDocumentIdAsync(existingDoc.Id, cancellationToken);

                var updatedDoc = new RepositoryDocument
                {
                    Id = existingDoc.Id,
                    RepositoryName = command.RepositoryName,
                    BranchName = command.BranchName,
                    FilePath = relativePath,
                    AbsolutePath = absolutePath,
                    Language = DetectLanguage(absolutePath),
                    ContentHash = hash,
                    FileSizeBytes = fileInfo.Length,
                    LastModifiedUtc = fileInfo.LastWriteTimeUtc,
                    IndexedAtUtc = DateTime.UtcNow
                };

                await _metadataStore.UpsertDocumentAsync(updatedDoc, cancellationToken);
                var addedChunks = await ParseAndIndexAsync(content, absolutePath, relativePath, command, updatedDoc.Id, cancellationToken);
                summary.AddedChunks += addedChunks;
                summary.UpdatedFiles++;
            }
            else
            {
                // New file
                var docId = Guid.NewGuid();
                var newDoc = new RepositoryDocument
                {
                    Id = docId,
                    RepositoryName = command.RepositoryName,
                    BranchName = command.BranchName,
                    FilePath = relativePath,
                    AbsolutePath = absolutePath,
                    Language = DetectLanguage(absolutePath),
                    ContentHash = hash,
                    FileSizeBytes = fileInfo.Length,
                    LastModifiedUtc = fileInfo.LastWriteTimeUtc,
                    IndexedAtUtc = DateTime.UtcNow
                };

                await _metadataStore.UpsertDocumentAsync(newDoc, cancellationToken);
                var addedChunks = await ParseAndIndexAsync(content, absolutePath, relativePath, command, docId, cancellationToken);
                summary.AddedChunks += addedChunks;
                summary.AddedFiles++;
            }
        }

        // Detect deleted files
        foreach (var (path, doc) in existingByPath)
        {
            if (!currentFilePaths.Contains(path))
            {
                await _metadataStore.DeleteChunksByDocumentIdAsync(doc.Id, cancellationToken);
                await _metadataStore.DeleteSymbolsByDocumentIdAsync(doc.Id, cancellationToken);
                await _metadataStore.DeleteDependenciesBySourceFileAsync(command.RepositoryName, command.BranchName, path, cancellationToken);
                await _vectorSearchService.DeleteByDocumentIdAsync(doc.Id, cancellationToken);
                await _metadataStore.MarkDocumentDeletedAsync(doc.Id, cancellationToken);
                summary.DeletedFiles++;
            }
        }

        summary.Duration = DateTime.UtcNow - start;
        return summary;
    }

    private async Task<int> ParseAndIndexAsync(
        string content,
        string absolutePath,
        string relativePath,
        IndexRepositoryCommand command,
        Guid docId,
        CancellationToken cancellationToken)
    {
        ParsedFile parsed;
        var parser = _parsers.FirstOrDefault(p => p.CanParse(absolutePath));
        if (parser != null)
        {
            parsed = await parser.ParseAsync(absolutePath, content, command.RepositoryName, command.BranchName, docId, cancellationToken);
        }
        else
        {
            var language = DetectLanguage(absolutePath);
            var chunks = _chunkingService.ChunkText(content, relativePath, language, command.RepositoryName, command.BranchName, docId);
            parsed = new ParsedFile
            {
                FilePath = relativePath,
                Language = language,
                Chunks = chunks,
                Symbols = [],
                Dependencies = []
            };
        }

        // Fix relative paths in chunks (Roslyn parser uses absolute path)
        var normalizedChunks = parsed.Chunks.Select(c => { c.FilePath = relativePath; return c; }).ToList();
        var normalizedSymbols = parsed.Symbols.Select(s => { s.FilePath = relativePath; return s; }).ToList();
        var normalizedDeps = parsed.Dependencies.Select(d => { d.SourceFilePath = relativePath; return d; }).ToList();

        await _metadataStore.AddChunksAsync(normalizedChunks, cancellationToken);
        await _metadataStore.AddSymbolsAsync(normalizedSymbols, cancellationToken);
        await _metadataStore.AddDependenciesAsync(normalizedDeps, cancellationToken);

        // Generate and store embeddings
        foreach (var chunk in normalizedChunks)
        {
            var embedding = _embeddingService.GenerateEmbedding(chunk.Text);
            var metadata = new Dictionary<string, string>
            {
                ["repositoryName"] = command.RepositoryName,
                ["branchName"] = command.BranchName,
                ["filePath"] = relativePath,
                ["chunkId"] = chunk.Id.ToString(),
                ["repositoryDocumentId"] = docId.ToString(),
                ["symbolName"] = chunk.SymbolName,
                ["language"] = chunk.Language
            };
            await _vectorSearchService.UpsertAsync(chunk.EmbeddingId, embedding, metadata, cancellationToken);
        }

        return normalizedChunks.Count;
    }

    /// <summary>
    /// Regenerates embeddings for all existing non-deleted chunks and upserts them into the
    /// vector store. Called at the start of every incremental index to recover after a process
    /// restart, where the in-memory vector store would otherwise be empty.
    /// </summary>
    private async Task ReloadEmbeddingsAsync(IndexRepositoryCommand command, CancellationToken cancellationToken)
    {
        var docs = await _metadataStore.GetAllDocumentsAsync(command.RepositoryName, command.BranchName, cancellationToken);
        foreach (var doc in docs.Where(d => !d.IsDeleted))
        {
            var chunks = await _metadataStore.GetChunksByDocumentIdAsync(doc.Id, cancellationToken);
            foreach (var chunk in chunks)
            {
                var embedding = _embeddingService.GenerateEmbedding(chunk.Text);
                await _vectorSearchService.UpsertAsync(chunk.EmbeddingId, embedding,
                    new Dictionary<string, string>
                    {
                        ["repositoryName"] = command.RepositoryName,
                        ["branchName"] = command.BranchName,
                        ["filePath"] = chunk.FilePath,
                        ["chunkId"] = chunk.Id.ToString(),
                        ["repositoryDocumentId"] = doc.Id.ToString(),
                        ["symbolName"] = chunk.SymbolName,
                        ["language"] = chunk.Language
                    }, cancellationToken);
            }
        }
    }

    private static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes);
    }

    private static string DetectLanguage(string filePath) => Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".cs" => "csharp",
        ".csproj" => "xml",
        ".json" => "json",
        ".md" => "markdown",
        ".ts" => "typescript",
        ".tsx" => "typescript",
        ".js" => "javascript",
        ".jsx" => "javascript",
        ".sql" => "sql",
        ".yml" or ".yaml" => "yaml",
        _ => "text"
    };
}
