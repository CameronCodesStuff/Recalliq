using Microsoft.Extensions.Logging;
using RecallIQ.Core.Enums;
using RecallIQ.Core.Interfaces;

namespace RecallIQ.Indexing.Parsers;

public sealed class TextDocumentParser : IDocumentParser
{
    private readonly ILogger<TextDocumentParser> _logger;

    public IReadOnlyCollection<DocumentType> SupportedTypes { get; } = new[] { DocumentType.Txt };

    public TextDocumentParser(ILogger<TextDocumentParser> logger)
    {
        _logger = logger;
    }

    public bool CanParse(string filePath)
    {
        return Path.GetExtension(filePath).Equals(".txt", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            return await File.ReadAllTextAsync(filePath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read text file: {Path}", filePath);
            return string.Empty;
        }
    }
}
