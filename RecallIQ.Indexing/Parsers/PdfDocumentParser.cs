using System.Text;
using Microsoft.Extensions.Logging;
using RecallIQ.Core.Enums;
using RecallIQ.Core.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace RecallIQ.Indexing.Parsers;

public sealed class PdfDocumentParser : IDocumentParser
{
    private readonly ILogger<PdfDocumentParser> _logger;
    private readonly IOcrService _ocrService;

    public IReadOnlyCollection<DocumentType> SupportedTypes { get; } = new[] { DocumentType.Pdf };

    public PdfDocumentParser(ILogger<PdfDocumentParser> logger, IOcrService ocrService)
    {
        _logger = logger;
        _ocrService = ocrService;
    }

    public bool CanParse(string filePath)
    {
        return Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sb = new StringBuilder();
            try
            {
                using var document = PdfDocument.Open(filePath);
                foreach (var page in document.GetPages())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var pageText = page.Text;
                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        sb.AppendLine(pageText);
                    }
                }

                if (sb.Length < 50 && _ocrService.IsInitialized)
                {
                    _logger.LogInformation("PDF appears scanned, attempting OCR for {Path}", filePath);
                    var ocrText = await _ocrService.ExtractTextFromImageAsync(
                        await File.ReadAllBytesAsync(filePath, cancellationToken), cancellationToken);
                    if (!string.IsNullOrWhiteSpace(ocrText))
                    {
                        sb.Clear();
                        sb.Append(ocrText);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse PDF: {Path}", filePath);
            }
            return sb.ToString();
        }, cancellationToken);
    }
}
