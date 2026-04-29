using RepositoryIntelligence.Domain.Enums;
using RepositoryIntelligence.Domain.Models;
using RepositoryIntelligence.Infrastructure.Analysis;
using RepositoryIntelligence.Infrastructure.Embeddings;
using RepositoryIntelligence.Infrastructure.FileSystem;
using RepositoryIntelligence.Infrastructure.Indexing;
using RepositoryIntelligence.Infrastructure.Parsing;
using RepositoryIntelligence.Infrastructure.Search;
using RepositoryIntelligence.Infrastructure.Storage;
using Xunit;

namespace RepositoryIntelligence.Tests;

public sealed class IssueAnalyzerTests : IDisposable
{
    private readonly string _sampleECommercePath =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples", "SampleECommerce"));
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.db");
    private readonly SqliteMetadataStore _store;
    private readonly InMemoryVectorSearchService _vectorStore;
    private readonly RepositoryIndexer _indexer;
    private readonly IssueAnalyzer _analyzer;

    public IssueAnalyzerTests()
    {
        _store = new SqliteMetadataStore(_dbPath);
        _vectorStore = new InMemoryVectorSearchService();
        var chunker = new TextChunkingService();
        var parser = new RoslynCodeParser(chunker);
        _indexer = new RepositoryIndexer(
            new LocalFileScanner(new GitIgnoreStylePathFilter()),
            new GitIgnoreStylePathFilter(),
            [parser],
            chunker,
            new LocalHashEmbeddingService(),
            _vectorStore,
            _store);
        var searchService = new SearchService(new LocalHashEmbeddingService(), _vectorStore, _store);
        _analyzer = new IssueAnalyzer(searchService, _store);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    [Fact]
    public async Task AnalyzeAsync_RecommendsExpectedFiles_ForPaymentIssue()
    {
        // Skip if sample directory not found
        if (!Directory.Exists(_sampleECommercePath))
        {
            // Look relative to solution root
            var altPath = Path.GetFullPath("../../../../samples/SampleECommerce");
            if (!Directory.Exists(altPath))
            {
                // Try another relative path
                var searchPath = FindSamplePath();
                if (searchPath is null) return; // skip
                await IndexSample(searchPath);
            }
            else
            {
                await IndexSample(altPath);
            }
        }
        else
        {
            await IndexSample(_sampleECommercePath);
        }

        var result = await _analyzer.AnalyzeAsync(new AnalyzeIssueCommand
        {
            RepositoryName = "SampleECommerce",
            BranchName = "main",
            Issue = "Payment is not triggered after order creation"
        });

        Assert.NotEmpty(result.RecommendedFiles);

        var filePaths = result.RecommendedFiles.Select(r => r.FilePath.ToLowerInvariant()).ToList();

        // At least one of the key payment/order files should appear in results
        var expectedKeywords = new[] { "order", "payment", "event", "handler", "listener" };
        bool hasRelevantFile = filePaths.Any(fp => expectedKeywords.Any(kw => fp.Contains(kw)));
        Assert.True(hasRelevantFile, $"Expected at least one payment/order related file. Got: {string.Join(", ", filePaths)}");
    }

    private async Task IndexSample(string path)
    {
        await _indexer.IndexRepositoryAsync(new IndexRepositoryCommand
        {
            RepositoryPath = path,
            RepositoryName = "SampleECommerce",
            BranchName = "main",
            Mode = IndexingMode.Full
        });
    }

    private static string? FindSamplePath()
    {
        var current = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(current, "samples", "SampleECommerce");
            if (Directory.Exists(candidate)) return candidate;
            current = Directory.GetParent(current)?.FullName ?? current;
        }
        return null;
    }
}
