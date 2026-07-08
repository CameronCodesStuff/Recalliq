using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using RecallIQ.Core.Interfaces;
using CoreDocumentType = RecallIQ.Core.Enums.DocumentType;

namespace RecallIQ.Indexing.Parsers;

public sealed class DocxDocumentParser : IDocumentParser
{
    private readonly ILogger<DocxDocumentParser> _logger;

    public IReadOnlyCollection<CoreDocumentType> SupportedTypes { get; } = new[] { CoreDocumentType.Docx };

    public DocxDocumentParser(ILogger<DocxDocumentParser> logger)
    {
        _logger = logger;
    }

    public bool CanParse(string filePath)
    {
        return Path.GetExtension(filePath).Equals(".docx", StringComparison.OrdinalIgnoreCase);
    }

    public Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sb = new StringBuilder();
            try
            {
                using var doc = WordprocessingDocument.Open(filePath, false);
                var body = doc.MainDocumentPart?.Document?.Body;
                if (body != null)
                {
                    foreach (var paragraph in body.Descendants<Paragraph>())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var text = paragraph.InnerText;
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            sb.AppendLine(text);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse DOCX: {Path}", filePath);
            }
            return sb.ToString();
        }, cancellationToken);
    }
}
