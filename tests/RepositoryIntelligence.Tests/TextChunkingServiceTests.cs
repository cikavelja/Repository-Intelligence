using RepositoryIntelligence.Domain.Enums;
using RepositoryIntelligence.Infrastructure.Parsing;
using Xunit;

namespace RepositoryIntelligence.Tests;

public sealed class TextChunkingServiceTests
{
    private readonly TextChunkingService _service = new();

    [Fact]
    public void ChunkText_SmallFile_ReturnsSingleChunk()
    {
        var content = string.Join('\n', Enumerable.Range(1, 10).Select(i => $"line {i}"));
        var chunks = _service.ChunkText(content, "test.md", "markdown", "repo", "main", Guid.NewGuid());

        Assert.Single(chunks);
        Assert.Equal(1, chunks[0].StartLine);
        Assert.Equal(10, chunks[0].EndLine);
        Assert.Equal(ChunkType.File, chunks[0].ChunkType);
    }

    [Fact]
    public void ChunkText_LargeFile_ReturnsMultipleChunks()
    {
        var content = string.Join('\n', Enumerable.Range(1, 120).Select(i => $"line {i}"));
        var chunks = _service.ChunkText(content, "large.cs", "csharp", "repo", "main", Guid.NewGuid());

        Assert.True(chunks.Count > 1, "Expected multiple chunks for a 120-line file.");
    }

    [Fact]
    public void ChunkText_SetsCorrectLineRanges()
    {
        var content = string.Join('\n', Enumerable.Range(1, 60).Select(i => $"line {i}"));
        var chunks = _service.ChunkText(content, "file.cs", "csharp", "repo", "main", Guid.NewGuid());

        foreach (var chunk in chunks)
        {
            Assert.True(chunk.StartLine >= 1);
            Assert.True(chunk.EndLine >= chunk.StartLine);
        }
    }

    [Fact]
    public void ChunkText_SetsRepositoryAndBranchMetadata()
    {
        var content = "some content";
        var docId = Guid.NewGuid();
        var chunks = _service.ChunkText(content, "file.cs", "csharp", "MyRepo", "feature-branch", docId);

        Assert.All(chunks, c =>
        {
            Assert.Equal("MyRepo", c.RepositoryName);
            Assert.Equal("feature-branch", c.BranchName);
            Assert.Equal(docId, c.RepositoryDocumentId);
        });
    }

    [Fact]
    public void ChunkText_AssignsUniqueIds()
    {
        var content = string.Join('\n', Enumerable.Range(1, 120).Select(i => $"line {i}"));
        var chunks = _service.ChunkText(content, "file.cs", "csharp", "repo", "main", Guid.NewGuid());

        var ids = chunks.Select(c => c.Id).ToHashSet();
        Assert.Equal(chunks.Count, ids.Count);
    }
}
