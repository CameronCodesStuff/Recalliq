using RecallIQ.Core.Enums;

namespace RecallIQ.Core.Interfaces;

public interface IDocumentParser
{
    IReadOnlyCollection<DocumentType> SupportedTypes { get; }
    Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default);
    bool CanParse(string filePath);
}
