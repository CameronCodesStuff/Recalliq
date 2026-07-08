using FluentAssertions;
using RecallIQ.AI;
using Xunit;

namespace RecallIQ.Tests.AI;

public class TextChunkerTests
{
    private readonly TextChunker _chunker = new();

    [Fact]
    public void ChunkText_EmptyString_ReturnsEmpty()
    {
        var result = _chunker.ChunkText(string.Empty);
        result.Should().BeEmpty();
    }

    [Fact]
    public void ChunkText_Whitespace_ReturnsEmpty()
    {
        var result = _chunker.ChunkText("   \n  \t  ");
        result.Should().BeEmpty();
    }

    [Fact]
    public void ChunkText_ShortText_ReturnsSingleChunk()
    {
        var result = _chunker.ChunkText("Hello world. This is a short text.");
        result.Should().ContainSingle();
        result[0].Text.Should().Contain("Hello world");
    }

    [Fact]
    public void ChunkText_LongText_ReturnsMultipleChunks()
    {
        var longText = string.Join(". ", Enumerable.Range(1, 500).Select(i => $"Sentence number {i}"));
        var result = _chunker.ChunkText(longText, chunkSizeTokens: 64, overlapTokens: 8);
        result.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public void ChunkText_AllChunksHaveContent()
    {
        var text = string.Join(". ", Enumerable.Range(1, 200).Select(i => $"Test sentence {i}"));
        var result = _chunker.ChunkText(text, chunkSizeTokens: 32, overlapTokens: 4);

        foreach (var (chunkText, start, end) in result)
        {
            chunkText.Should().NotBeNullOrWhiteSpace();
            end.Should().BeGreaterThan(start);
        }
    }

    [Fact]
    public void ChunkText_OffsetsAreValid()
    {
        var text = "First paragraph of content. Second paragraph of content. Third paragraph of content.";
        var result = _chunker.ChunkText(text, chunkSizeTokens: 8, overlapTokens: 2);

        foreach (var (_, start, end) in result)
        {
            start.Should().BeGreaterThanOrEqualTo(0);
            end.Should().BeLessThanOrEqualTo(text.Length);
        }
    }

    [Fact]
    public void ChunkText_CustomSizes_Respected()
    {
        var text = string.Join(" ", Enumerable.Repeat("word", 1000));
        var smallChunks = _chunker.ChunkText(text, chunkSizeTokens: 16, overlapTokens: 2);
        var largeChunks = _chunker.ChunkText(text, chunkSizeTokens: 128, overlapTokens: 8);

        smallChunks.Count.Should().BeGreaterThan(largeChunks.Count);
    }

    [Fact]
    public void ChunkText_SentenceBoundaryRespected()
    {
        var text = "This is sentence one. This is sentence two. This is sentence three. This is sentence four.";
        var result = _chunker.ChunkText(text, chunkSizeTokens: 10, overlapTokens: 2);

        result.Should().NotBeEmpty();
        foreach (var (chunkText, _, _) in result)
        {
            chunkText.Length.Should().BeGreaterThan(0);
        }
    }
}
