using RecallIQ.Core.Interfaces;

namespace RecallIQ.AI;

public sealed class TextChunker : ITextChunker
{
    private static readonly char[] SentenceEnders = ['.', '!', '?', '\n'];

    public IReadOnlyList<(string Text, int StartOffset, int EndOffset)> ChunkText(
        string text, int chunkSizeTokens = 256, int overlapTokens = 32)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<(string, int, int)>();

        int charsPerToken = 4;
        int chunkSizeChars = chunkSizeTokens * charsPerToken;
        int overlapChars = overlapTokens * charsPerToken;

        var chunks = new List<(string Text, int StartOffset, int EndOffset)>();
        int position = 0;

        while (position < text.Length)
        {
            int end = Math.Min(position + chunkSizeChars, text.Length);

            if (end < text.Length)
            {
                int sentenceEnd = text.LastIndexOfAny(SentenceEnders, end - 1, Math.Min(end - position, chunkSizeChars));
                if (sentenceEnd > position + chunkSizeChars / 2)
                {
                    end = sentenceEnd + 1;
                }
            }

            var chunkText = text[position..end].Trim();
            if (chunkText.Length > 0)
            {
                chunks.Add((chunkText, position, end));
            }

            int step = end - position - overlapChars;
            if (step <= 0) step = end - position;
            position += step;

            if (position >= text.Length) break;
        }

        return chunks;
    }
}
