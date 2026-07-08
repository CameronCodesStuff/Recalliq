namespace RecallIQ.Core.Interfaces;

public interface IEmbeddingService : IDisposable
{
    int EmbeddingDimension { get; }
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<float[][]> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default);
    Task InitializeAsync(string modelPath, CancellationToken cancellationToken = default);
    bool IsInitialized { get; }
}
