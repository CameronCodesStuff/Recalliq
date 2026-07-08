using RecallIQ.Core.Extensions;
using RecallIQ.Core.Interfaces;

namespace RecallIQ.Indexing;

public sealed class DocumentParserFactory
{
    private readonly IReadOnlyList<IDocumentParser> _parsers;

    public DocumentParserFactory(IEnumerable<IDocumentParser> parsers)
    {
        _parsers = parsers.ToList();
    }

    public IDocumentParser? GetParser(string filePath)
    {
        foreach (var parser in _parsers)
        {
            if (parser.CanParse(filePath))
                return parser;
        }
        return null;
    }

    public bool IsSupportedFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return DocumentTypeExtensions.IsSupportedExtension(extension);
    }
}
