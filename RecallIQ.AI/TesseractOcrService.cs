using Microsoft.Extensions.Logging;
using RecallIQ.Core.Interfaces;
using Tesseract;

namespace RecallIQ.AI;

public sealed class TesseractOcrService : IOcrService
{
    private readonly ILogger<TesseractOcrService> _logger;
    private TesseractEngine? _engine;
    private bool _disposed;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public bool IsInitialized => _engine != null;

    public TesseractOcrService(ILogger<TesseractOcrService> logger)
    {
        _logger = logger;
    }

    public Task InitializeAsync(string tessDataPath, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(tessDataPath))
            {
                _logger.LogWarning("Tesseract data directory not found at {Path}, OCR will be unavailable", tessDataPath);
                return;
            }

            try
            {
                _engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
                _logger.LogInformation("Tesseract OCR engine initialized from {Path}", tessDataPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize Tesseract, OCR will be unavailable");
            }
        }, cancellationToken);
    }

    public async Task<string> ExtractTextFromImageAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        if (_engine == null) return string.Empty;
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var img = Pix.LoadFromFile(imagePath);
                using var page = _engine.Process(img);
                return page.GetText().Trim();
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR failed for {Path}", imagePath);
            return string.Empty;
        }
        finally { _semaphore.Release(); }
    }

    public async Task<string> ExtractTextFromImageAsync(byte[] imageData, CancellationToken cancellationToken = default)
    {
        if (_engine == null) return string.Empty;
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var img = Pix.LoadFromMemory(imageData);
                using var page = _engine.Process(img);
                return page.GetText().Trim();
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR failed for in-memory image");
            return string.Empty;
        }
        finally { _semaphore.Release(); }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _engine?.Dispose();
        _semaphore.Dispose();
    }
}
