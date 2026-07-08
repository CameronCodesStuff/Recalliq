namespace RecallIQ.Core.Models;

public sealed class SearchResult
{
    public long DocumentId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string MatchedParagraph { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
    public int ChunkIndex { get; set; }
    public DateTime IndexedAtUtc { get; set; }
    public string FileExtension { get; set; } = string.Empty;
}
