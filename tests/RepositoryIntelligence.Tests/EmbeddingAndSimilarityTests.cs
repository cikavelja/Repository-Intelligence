using RepositoryIntelligence.Infrastructure.Embeddings;
using RepositoryIntelligence.Infrastructure.Search;
using Xunit;

namespace RepositoryIntelligence.Tests;

public sealed class EmbeddingAndSimilarityTests
{
    private readonly LocalHashEmbeddingService _service = new();

    [Fact]
    public void GenerateEmbedding_ReturnsDeterministicResult()
    {
        var text = "Payment is not triggered after order creation";
        var v1 = _service.GenerateEmbedding(text);
        var v2 = _service.GenerateEmbedding(text);

        Assert.Equal(v1, v2);
    }

    [Fact]
    public void GenerateEmbedding_ReturnsCorrectDimensions()
    {
        var v = _service.GenerateEmbedding("some text");
        Assert.Equal(_service.Dimensions, v.Length);
    }

    [Fact]
    public void GenerateEmbedding_ReturnsUnitVector()
    {
        var v = _service.GenerateEmbedding("test embedding normalization");
        var magnitude = MathF.Sqrt(v.Sum(x => x * x));
        Assert.True(Math.Abs(magnitude - 1.0f) < 0.001f, $"Magnitude was {magnitude}, expected ~1.0");
    }

    [Fact]
    public void CosineSimilarity_SimilarTexts_ScoreHigherThanUnrelated()
    {
        var payment = _service.GenerateEmbedding("payment order processing trigger");
        var orderCreated = _service.GenerateEmbedding("order created payment trigger processing");
        var inventory = _service.GenerateEmbedding("inventory report stock warehouse product price");

        var simRelated = InMemoryVectorSearchService.CosineSimilarity(payment, orderCreated);
        var simUnrelated = InMemoryVectorSearchService.CosineSimilarity(payment, inventory);

        Assert.True(simRelated > simUnrelated,
            $"Expected related score ({simRelated:F4}) > unrelated score ({simUnrelated:F4})");
    }

    [Fact]
    public void CosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        var v = _service.GenerateEmbedding("identical text");
        var sim = InMemoryVectorSearchService.CosineSimilarity(v, v);
        Assert.True(Math.Abs(sim - 1.0) < 0.001, $"Expected 1.0 but got {sim}");
    }

    [Fact]
    public void GenerateEmbedding_DifferentTexts_ReturnDifferentVectors()
    {
        var v1 = _service.GenerateEmbedding("payment order");
        var v2 = _service.GenerateEmbedding("inventory report");
        Assert.False(v1.SequenceEqual(v2));
    }
}
