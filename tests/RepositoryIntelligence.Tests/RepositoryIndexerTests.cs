using RepositoryIntelligence.Domain.Enums;
using RepositoryIntelligence.Domain.Models;
using RepositoryIntelligence.Infrastructure.Embeddings;
using RepositoryIntelligence.Infrastructure.FileSystem;
using RepositoryIntelligence.Infrastructure.Indexing;
using RepositoryIntelligence.Infrastructure.Parsing;
using RepositoryIntelligence.Infrastructure.Search;
using RepositoryIntelligence.Infrastructure.Storage;
using Xunit;

namespace RepositoryIntelligence.Tests;

public sealed class RepositoryIndexerTests : IDisposable
{
    private readonly string _tempRepo = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");
    private readonly SqliteMetadataStore _store;
    private readonly InMemoryVectorSearchService _vectorStore;
    private readonly RepositoryIndexer _indexer;

    public RepositoryIndexerTests()
    {
        Directory.CreateDirectory(_tempRepo);
        _store = new SqliteMetadataStore(_dbPath);
        _vectorStore = new InMemoryVectorSearchService();
        var chunker = new TextChunkingService();
        var parser = new RoslynCodeParser(chunker);
        _indexer = new RepositoryIndexer(
            new LocalFileScanner(new GitIgnoreStylePathFilter()),
            [parser],
            chunker,
            new LocalHashEmbeddingService(),
            _vectorStore,
            _store);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        Directory.Delete(_tempRepo, recursive: true);
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    private IndexRepositoryCommand MakeCommand(IndexingMode mode = IndexingMode.Full) => new()
    {
        RepositoryPath = _tempRepo,
        RepositoryName = "TestRepo",
        BranchName = "main",
        Mode = mode
    };

    [Fact]
    public async Task FullIndex_IndexesNewFiles()
    {
        File.WriteAllText(Path.Combine(_tempRepo, "Service.cs"), "public class Service { }");

        var summary = await _indexer.IndexRepositoryAsync(MakeCommand(IndexingMode.Full));

        Assert.Equal(1, summary.AddedFiles);
        Assert.True(summary.AddedChunks > 0);
    }

    [Fact]
    public async Task FullIndex_ClearsAndRebuilds()
    {
        File.WriteAllText(Path.Combine(_tempRepo, "A.cs"), "public class A {}");
        await _indexer.IndexRepositoryAsync(MakeCommand(IndexingMode.Full));

        // Second full index with different files
        File.Delete(Path.Combine(_tempRepo, "A.cs"));
        File.WriteAllText(Path.Combine(_tempRepo, "B.cs"), "public class B {}");
        var summary2 = await _indexer.IndexRepositoryAsync(MakeCommand(IndexingMode.Full));

        Assert.Equal(1, summary2.AddedFiles);
        Assert.Equal(0, summary2.UnchangedFiles);
    }

    [Fact]
    public async Task IncrementalIndex_DetectsUnchangedFiles()
    {
        File.WriteAllText(Path.Combine(_tempRepo, "Stable.cs"), "public class Stable {}");
        await _indexer.IndexRepositoryAsync(MakeCommand(IndexingMode.Full));

        var summary2 = await _indexer.IndexRepositoryAsync(MakeCommand(IndexingMode.Incremental));

        Assert.Equal(1, summary2.UnchangedFiles);
        Assert.Equal(0, summary2.AddedFiles);
    }

    [Fact]
    public async Task IncrementalIndex_DetectsUpdatedFiles()
    {
        var filePath = Path.Combine(_tempRepo, "Updating.cs");
        File.WriteAllText(filePath, "public class V1 {}");
        await _indexer.IndexRepositoryAsync(MakeCommand(IndexingMode.Full));

        File.WriteAllText(filePath, "public class V2 { public void DoWork() {} }");
        var summary2 = await _indexer.IndexRepositoryAsync(MakeCommand(IndexingMode.Incremental));

        Assert.Equal(1, summary2.UpdatedFiles);
        Assert.Equal(0, summary2.UnchangedFiles);
    }

    [Fact]
    public async Task IncrementalIndex_DetectsDeletedFiles()
    {
        var filePath = Path.Combine(_tempRepo, "ToDelete.cs");
        File.WriteAllText(filePath, "public class ToDelete {}");
        await _indexer.IndexRepositoryAsync(MakeCommand(IndexingMode.Full));

        File.Delete(filePath);
        var summary2 = await _indexer.IndexRepositoryAsync(MakeCommand(IndexingMode.Incremental));

        Assert.Equal(1, summary2.DeletedFiles);
    }

    [Fact]
    public async Task DeletedFiles_DoNotAppearInSearchResults()
    {
        var filePath = Path.Combine(_tempRepo, "DeleteMe.cs");
        File.WriteAllText(filePath, "public class DeleteMe { public void PaymentTrigger() {} }");
        await _indexer.IndexRepositoryAsync(MakeCommand(IndexingMode.Full));

        File.Delete(filePath);
        await _indexer.IndexRepositoryAsync(MakeCommand(IndexingMode.Incremental));

        var searchService = new SearchService(new LocalHashEmbeddingService(), _vectorStore, _store);
        var results = await searchService.SearchAsync(new SearchCodeCommand
        {
            RepositoryName = "TestRepo",
            BranchName = "main",
            Query = "PaymentTrigger DeleteMe",
            MaxResults = 10
        });

        Assert.DoesNotContain(results, r => r.FilePath.Contains("DeleteMe"));
    }

    [Fact]
    public async Task Search_RehydratesVectors_FromPersistedChunksInFreshService()
    {
        File.WriteAllText(Path.Combine(_tempRepo, "OrderHandler.cs"), "public class OrderHandler { public void TriggerPayment() {} }");
        await _indexer.IndexRepositoryAsync(MakeCommand(IndexingMode.Full));

        var freshVectorStore = new InMemoryVectorSearchService();
        var freshSearchService = new SearchService(new LocalHashEmbeddingService(), freshVectorStore, _store);

        var results = await freshSearchService.SearchAsync(new SearchCodeCommand
        {
            RepositoryName = "TestRepo",
            BranchName = "main",
            Query = "trigger payment order handler",
            MaxResults = 5
        });

        Assert.Contains(results, r => r.FilePath.EndsWith("OrderHandler.cs", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FullIndex_ClearsStaleVectorsBeforeReindexing()
    {
        for (var index = 0; index < 10; index++)
        {
            File.WriteAllText(
                Path.Combine(_tempRepo, $"Old{index}.cs"),
                $"public class Old{index} {{ public void PaymentListener() {{ }} }}");
        }

        await _indexer.IndexRepositoryAsync(MakeCommand(IndexingMode.Full));

        foreach (var file in Directory.GetFiles(_tempRepo, "*.cs"))
        {
            File.Delete(file);
        }

        File.WriteAllText(Path.Combine(_tempRepo, "NewListener.cs"), "public class NewListener { public void PaymentListener() {} }");

        await _indexer.IndexRepositoryAsync(MakeCommand(IndexingMode.Full));

        var searchService = new SearchService(new LocalHashEmbeddingService(), _vectorStore, _store);
        var results = await searchService.SearchAsync(new SearchCodeCommand
        {
            RepositoryName = "TestRepo",
            BranchName = "main",
            Query = "PaymentListener",
            MaxResults = 1
        });

        Assert.Single(results);
        Assert.Equal("NewListener.cs", results[0].FilePath);
    }

    [Fact]
    public async Task IncrementalIndex_IgnoresIgnoredFolders()
    {
        var binDir = Path.Combine(_tempRepo, "bin");
        Directory.CreateDirectory(binDir);
        File.WriteAllText(Path.Combine(binDir, "Generated.cs"), "// generated");
        File.WriteAllText(Path.Combine(_tempRepo, "Real.cs"), "public class Real {}");

        var summary = await _indexer.IndexRepositoryAsync(MakeCommand(IndexingMode.Full));

        Assert.Equal(1, summary.AddedFiles);
    }
}
