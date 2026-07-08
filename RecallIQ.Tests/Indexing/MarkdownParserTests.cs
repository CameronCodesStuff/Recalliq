using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RecallIQ.Indexing.Parsers;
using Xunit;

namespace RecallIQ.Tests.Indexing;

public class MarkdownParserTests
{
    private readonly MarkdownDocumentParser _parser;

    public MarkdownParserTests()
    {
        _parser = new MarkdownDocumentParser(Mock.Of<ILogger<MarkdownDocumentParser>>());
    }

    [Theory]
    [InlineData("readme.md", true)]
    [InlineData("readme.markdown", true)]
    [InlineData("readme.txt", false)]
    public void CanParse_ReturnsExpected(string file, bool expected)
    {
        _parser.CanParse(file).Should().Be(expected);
    }

    [Fact]
    public async Task ExtractText_StripsMarkdownFormatting()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.md");
        try
        {
            await File.WriteAllTextAsync(tempFile, "# Heading\n\n**Bold text** and *italic text*.\n\n- List item");
            var text = await _parser.ExtractTextAsync(tempFile);

            text.Should().Contain("Heading");
            text.Should().Contain("Bold text");
            text.Should().Contain("italic text");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
