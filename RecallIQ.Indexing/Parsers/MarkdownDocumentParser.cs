using Markdig;
using Microsoft.Extensions.Logging;
using RecallIQ.Core.Enums;
using RecallIQ.Core.Interfaces;

namespace RecallIQ.Indexing.Parsers;

public sealed class MarkdownDocumentParser : IDocumentParser
{
    private readonly ILogger<MarkdownDocumentParser> _logger;

    public IReadOnlyCollection<DocumentType> SupportedTypes { get; } = new[] { DocumentType.Markdown };

    public MarkdownDocumentParser(ILogger<MarkdownDocumentParser> logger)
    {
        _logger = logger;
    }

    public bool CanParse(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return ext.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".markdown", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var markdown = await File.ReadAllTextAsync(filePath, cancellationToken);
            var plainText = Markdown.ToPlainText(markdown);
            return plainText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Markdown: {Path}", filePath);
            return string.Empty;
        }
    }
}
