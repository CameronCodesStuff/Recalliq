using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RecallIQ.Indexing.Parsers;
using Xunit;

namespace RecallIQ.Tests.Indexing;

public class TextParserTests
{
    private readonly TextDocumentParser _parser;

    public TextParserTests()
    {
        _parser = new TextDocumentParser(Mock.Of<ILogger<TextDocumentParser>>());
    }

    [Fact]
    public void CanParse_TxtFile_ReturnsTrue()
    {
        _parser.CanParse("test.txt").Should().BeTrue();
    }

    [Fact]
    public void CanParse_NonTxtFile_ReturnsFalse()
    {
        _parser.CanParse("test.pdf").Should().BeFalse();
    }

    [Fact]
    public async Task ExtractText_ReadsFileContent()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "Hello World\nLine Two");
            var renamedFile = Path.ChangeExtension(tempFile, ".txt");
            File.Move(tempFile, renamedFile);

            var text = await _parser.ExtractTextAsync(renamedFile);
            text.Should().Contain("Hello World");
            text.Should().Contain("Line Two");

            File.Delete(renamedFile);
        }
        catch
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
            throw;
        }
    }

    [Fact]
    public async Task ExtractText_NonExistentFile_ReturnsEmpty()
    {
        var text = await _parser.ExtractTextAsync("/nonexistent/file.txt");
        text.Should().BeEmpty();
    }
}
