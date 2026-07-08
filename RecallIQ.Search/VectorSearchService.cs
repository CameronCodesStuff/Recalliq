using Microsoft.Extensions.Logging;
using RecallIQ.Core.Interfaces;
using RecallIQ.Core.Models;
using RecallIQ.Storage;

namespace RecallIQ.Search;

public sealed class VectorSearchService : ISearchService
{
    private readonly ILogger<VectorSearchService> _logger;
    private readonly IStorageService _storage;
    private readonly IEmbeddingService _embeddingService;

    public VectorSearchService(
        ILogger<VectorSearchService> logger,
        IStorageService storage,
        IEmbeddingService embeddingService)
    {
        _logger = logger;
        _storage = storage;
        _embeddingService = embeddingService;
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query, int maxResults = 20, double minScore = 0.25,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<SearchResult>();

        _logger.LogInformation("Searching for: {Query}", query);

        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
        var allChunks = await _storage.GetAllChunksAsync(cancellationToken);

        var scored = new List<(DocumentChunk Chunk, float Score)>();

        foreach (var chunk in allChunks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var chunkEmbedding = VectorOperations.DeserializeEmbedding(chunk.Embedding);
            var score = VectorOperations.CosineSimilarity(queryEmbedding, chunkEmbedding);

            if (score >= minScore)
            {
                scored.Add((chunk, score));
            }
        }

        scored.Sort((a, b) => b.Score.CompareTo(a.Score));

        var results = new List<SearchResult>();
        var seenDocuments = new HashSet<long>();

        foreach (var (chunk, score) in scored)
        {
            if (results.Count >= maxResults) break;

            if (!seenDocuments.Add(chunk.DocumentId)) continue;

            var doc = await _storage.GetDocumentByIdAsync(chunk.DocumentId, cancellationToken);
            if (doc == null) continue;

            results.Add(new SearchResult
            {
                DocumentId = doc.Id,
                FilePath = doc.FilePath,
                FileName = doc.FileName,
                MatchedParagraph = chunk.Content,
                RelevanceScore = Math.Round(score, 4),
                ChunkIndex = chunk.ChunkIndex,
                IndexedAtUtc = doc.IndexedAtUtc,
                FileExtension = doc.FileExtension
            });
        }

        await _storage.InsertActivityAsync(new ActivityEntry
        {
            Type = Core.Enums.ActivityType.SearchPerformed,
            Message = $"Search: \"{query}\" — {results.Count} result(s)"
        }, CancellationToken.None);

        _logger.LogInformation("Search returned {Count} results for: {Query}", results.Count, query);
        return results;
    }
}
