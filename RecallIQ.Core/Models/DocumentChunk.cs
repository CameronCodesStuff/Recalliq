namespace RecallIQ.Core.Models;

public sealed class DocumentChunk
{
    public long Id { get; set; }
    public long DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public byte[] Embedding { get; set; } = Array.Empty<byte>();
    public int StartOffset { get; set; }
    public int EndOffset { get; set; }
}
