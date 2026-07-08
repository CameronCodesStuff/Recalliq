namespace RecallIQ.Core.Interfaces;

public interface ITextChunker
{
    IReadOnlyList<(string Text, int StartOffset, int EndOffset)> ChunkText(string text, int chunkSizeTokens = 256, int overlapTokens = 32);
}
