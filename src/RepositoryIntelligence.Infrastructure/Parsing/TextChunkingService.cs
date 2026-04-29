using RepositoryIntelligence.Application.Interfaces;
using RepositoryIntelligence.Domain.Enums;
using RepositoryIntelligence.Domain.Models;
using System.Security.Cryptography;
using System.Text;

namespace RepositoryIntelligence.Infrastructure.Parsing;

public sealed class TextChunkingService : IChunkingService
{
    private const int ChunkSizeLines = 50;
    private const int OverlapLines = 5;

    public IReadOnlyList<CodeChunk> ChunkText(
        string content,
        string filePath,
        string language,
        string repositoryName,
        string branchName,
        Guid repositoryDocumentId)
    {
        var lines = content.Split('\n');
        var chunks = new List<CodeChunk>();

        if (lines.Length <= ChunkSizeLines)
        {
            // Whole file as one chunk
            chunks.Add(MakeChunk(content, filePath, language, repositoryName, branchName, repositoryDocumentId, 1, lines.Length, ChunkType.File));
            return chunks;
        }

        int start = 0;
        while (start < lines.Length)
        {
            int end = Math.Min(start + ChunkSizeLines - 1, lines.Length - 1);
            var chunkLines = lines[start..(end + 1)];
            var chunkText = string.Join('\n', chunkLines);

            chunks.Add(MakeChunk(chunkText, filePath, language, repositoryName, branchName, repositoryDocumentId, start + 1, end + 1, ChunkType.TextBlock));

            if (end >= lines.Length - 1) break;
            start = Math.Max(start + ChunkSizeLines - OverlapLines, start + 1);
        }

        return chunks;
    }

    private static CodeChunk MakeChunk(
        string text,
        string filePath,
        string language,
        string repositoryName,
        string branchName,
        Guid repositoryDocumentId,
        int startLine,
        int endLine,
        ChunkType chunkType)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        var id = Guid.NewGuid();
        return new CodeChunk
        {
            Id = id,
            RepositoryDocumentId = repositoryDocumentId,
            RepositoryName = repositoryName,
            BranchName = branchName,
            FilePath = filePath,
            Language = language,
            ChunkType = chunkType,
            StartLine = startLine,
            EndLine = endLine,
            Text = text,
            ContentHash = hash,
            EmbeddingId = id.ToString()
        };
    }
}
