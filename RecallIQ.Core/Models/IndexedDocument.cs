using RecallIQ.Core.Enums;

namespace RecallIQ.Core.Models;

public sealed class IndexedDocument
{
    public long Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public long FileSizeBytes { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public DateTime LastModifiedUtc { get; set; }
    public DateTime IndexedAtUtc { get; set; }
    public int ChunkCount { get; set; }
    public bool IsOcrProcessed { get; set; }
}
