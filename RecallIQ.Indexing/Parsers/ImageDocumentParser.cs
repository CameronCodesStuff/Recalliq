using Microsoft.Extensions.Logging;
using RecallIQ.Core.Enums;
using RecallIQ.Core.Interfaces;

namespace RecallIQ.Indexing.Parsers;

public sealed class ImageDocumentParser : IDocumentParser
{
    private readonly ILogger<ImageDocumentParser> _logger;
    private readonly IOcrService _ocrService;

    public IReadOnlyCollection<DocumentType> SupportedTypes { get; } =
        new[] { DocumentType.Png, DocumentType.Jpg, DocumentType.Tiff };

    public ImageDocumentParser(ILogger<ImageDocumentParser> logger, IOcrService ocrService)
    {
        _logger = logger;
        _ocrService = ocrService;
    }

    public bool CanParse(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext is ".png" or ".jpg" or ".jpeg" or ".tif" or ".tiff";
    }

    public async Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!_ocrService.IsInitialized)
        {
            _logger.LogWarning("OCR not initialized, cannot extract text from image: {Path}", filePath);
            return string.Empty;
        }

        try
        {
            return await _ocrService.ExtractTextFromImageAsync(filePath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to OCR image: {Path}", filePath);
            return string.Empty;
        }
    }
}
