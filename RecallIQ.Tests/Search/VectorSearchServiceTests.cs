using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RecallIQ.Core.Enums;
using RecallIQ.Core.Interfaces;
using RecallIQ.Core.Models;
using RecallIQ.Search;
using RecallIQ.Storage;
using Xunit;

namespace RecallIQ.Tests.Search;

public class VectorSearchServiceTests
{
    private readonly Mock<ILogger<VectorSearchService>> _logger = new();
    private readonly Mock<IStorageService> _storage = new();
    private readonly Mock<IEmbeddingService> _embedding = new();

    [Fact]
    public async Task Search_EmptyQuery_ReturnsEmpty()
    {
        var service = CreateService();
        var results = await service.SearchAsync("");
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_WhitespaceQuery_ReturnsEmpty()
    {
        var service = CreateService();
        var results = await service.SearchAsync("   ");
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_WithResults_ReturnsSortedByScore()
    {
        var queryEmb = CreateNormalizedVector(0.5f);
        var highMatchEmb = VectorOperations.SerializeEmbedding(CreateNormalizedVector(0.5f));
        var lowMatchEmb = VectorOperations.SerializeEmbedding(CreateNormalizedVector(-0.5f));

        _embedding.Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryEmb);

        var chunks = new List<DocumentChunk>
        {
            new() { Id = 1, DocumentId = 1, ChunkIndex = 0, Content = "High match", Embedding = highMatchEmb, StartOffset = 0, EndOffset = 10 },
            new() { Id = 2, DocumentId = 2, ChunkIndex = 0, Content = "Low match", Embedding = lowMatchEmb, StartOffset = 0, EndOffset = 9 }
        };

        _storage.Setup(s => s.GetAllChunksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(chunks);
        _storage.Setup(s => s.GetDocumentByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IndexedDocument { Id = 1, FilePath = "/high.pdf", FileName = "high.pdf", FileExtension = ".pdf" });
        _storage.Setup(s => s.GetDocumentByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IndexedDocument { Id = 2, FilePath = "/low.pdf", FileName = "low.pdf", FileExtension = ".pdf" });
        _storage.Setup(s => s.InsertActivityAsync(It.IsAny<ActivityEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var results = await service.SearchAsync("test query", minScore: -1.0);

        results.Should().HaveCount(2);
        results[0].RelevanceScore.Should().BeGreaterThan(results[1].RelevanceScore);
    }

    [Fact]
    public async Task Search_RespectsMaxResults()
    {
        var queryEmb = CreateNormalizedVector(0.1f);
        _embedding.Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryEmb);

        var chunks = Enumerable.Range(1, 10).Select(i => new DocumentChunk
        {
            Id = i, DocumentId = i, ChunkIndex = 0, Content = $"Chunk {i}",
            Embedding = VectorOperations.SerializeEmbedding(CreateNormalizedVector(0.1f * i)),
            StartOffset = 0, EndOffset = 5
        }).ToList();

        _storage.Setup(s => s.GetAllChunksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(chunks);
        for (int i = 1; i <= 10; i++)
        {
            var idx = i;
            _storage.Setup(s => s.GetDocumentByIdAsync(idx, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new IndexedDocument { Id = idx, FilePath = $"/{idx}.pdf", FileName = $"{idx}.pdf", FileExtension = ".pdf" });
        }
        _storage.Setup(s => s.InsertActivityAsync(It.IsAny<ActivityEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var results = await service.SearchAsync("test", maxResults: 3, minScore: -1.0);

        results.Count.Should().BeLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task Search_DeduplicatesDocuments()
    {
        var queryEmb = CreateNormalizedVector(0.3f);
        _embedding.Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryEmb);

        var sameSerialized = VectorOperations.SerializeEmbedding(CreateNormalizedVector(0.3f));
        var chunks = new List<DocumentChunk>
        {
            new() { Id = 1, DocumentId = 1, ChunkIndex = 0, Content = "Chunk A", Embedding = sameSerialized, StartOffset = 0, EndOffset = 7 },
            new() { Id = 2, DocumentId = 1, ChunkIndex = 1, Content = "Chunk B", Embedding = sameSerialized, StartOffset = 8, EndOffset = 15 }
        };

        _storage.Setup(s => s.GetAllChunksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(chunks);
        _storage.Setup(s => s.GetDocumentByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IndexedDocument { Id = 1, FilePath = "/doc.pdf", FileName = "doc.pdf", FileExtension = ".pdf" });
        _storage.Setup(s => s.InsertActivityAsync(It.IsAny<ActivityEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var results = await service.SearchAsync("test", minScore: -1.0);

        results.Should().ContainSingle();
    }

    private VectorSearchService CreateService()
    {
        return new VectorSearchService(_logger.Object, _storage.Object, _embedding.Object);
    }

    private static float[] CreateNormalizedVector(float seed)
    {
        var vec = new float[384];
        var rng = new Random(BitConverter.SingleToInt32Bits(seed));
        for (int i = 0; i < vec.Length; i++)
            vec[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
        float mag = MathF.Sqrt(vec.Sum(x => x * x));
        if (mag > 0) for (int i = 0; i < vec.Length; i++) vec[i] /= mag;
        return vec;
    }
}
