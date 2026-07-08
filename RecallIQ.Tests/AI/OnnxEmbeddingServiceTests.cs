using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RecallIQ.AI;
using Xunit;

namespace RecallIQ.Tests.AI;

public class OnnxEmbeddingServiceTests : IDisposable
{
    private readonly OnnxEmbeddingService _service;

    public OnnxEmbeddingServiceTests()
    {
        var logger = new Mock<ILogger<OnnxEmbeddingService>>();
        _service = new OnnxEmbeddingService(logger.Object);
    }

    [Fact]
    public async Task GenerateEmbedding_WithoutModel_UsesHashFallback()
    {
        await _service.InitializeAsync("nonexistent_model.onnx");

        var embedding = await _service.GenerateEmbeddingAsync("test text");

        embedding.Should().NotBeNull();
        embedding.Should().HaveCount(384);
    }

    [Fact]
    public async Task GenerateEmbedding_SameText_ReturnsSameEmbedding()
    {
        await _service.InitializeAsync("nonexistent.onnx");

        var emb1 = await _service.GenerateEmbeddingAsync("hello world");
        var emb2 = await _service.GenerateEmbeddingAsync("hello world");

        emb1.Should().BeEquivalentTo(emb2);
    }

    [Fact]
    public async Task GenerateEmbedding_DifferentTexts_ReturnsDifferentEmbeddings()
    {
        await _service.InitializeAsync("nonexistent.onnx");

        var emb1 = await _service.GenerateEmbeddingAsync("cats are great");
        var emb2 = await _service.GenerateEmbeddingAsync("quantum physics theory");

        emb1.Should().NotBeEquivalentTo(emb2);
    }

    [Fact]
    public async Task GenerateEmbeddings_Batch_ReturnsCorrectCount()
    {
        await _service.InitializeAsync("nonexistent.onnx");

        var texts = new[] { "text one", "text two", "text three" };
        var embeddings = await _service.GenerateEmbeddingsAsync(texts);

        embeddings.Should().HaveCount(3);
        foreach (var emb in embeddings)
        {
            emb.Should().HaveCount(384);
        }
    }

    [Fact]
    public async Task GenerateEmbedding_IsNormalized()
    {
        await _service.InitializeAsync("nonexistent.onnx");

        var embedding = await _service.GenerateEmbeddingAsync("test normalization");
        float magnitude = MathF.Sqrt(embedding.Sum(x => x * x));

        magnitude.Should().BeApproximately(1.0f, 0.01f);
    }

    [Fact]
    public void EmbeddingDimension_DefaultIs384()
    {
        _service.EmbeddingDimension.Should().Be(384);
    }

    [Fact]
    public async Task GenerateEmbedding_EmptyString_DoesNotThrow()
    {
        await _service.InitializeAsync("nonexistent.onnx");
        var act = async () => await _service.GenerateEmbeddingAsync("");
        await act.Should().NotThrowAsync();
    }

    public void Dispose()
    {
        _service.Dispose();
    }
}
