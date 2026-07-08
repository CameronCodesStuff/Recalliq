namespace RecallIQ.Core.Interfaces;

public interface IOcrService : IDisposable
{
    Task<string> ExtractTextFromImageAsync(string imagePath, CancellationToken cancellationToken = default);
    Task<string> ExtractTextFromImageAsync(byte[] imageData, CancellationToken cancellationToken = default);
    Task InitializeAsync(string tessDataPath, CancellationToken cancellationToken = default);
    bool IsInitialized { get; }
}
