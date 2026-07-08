using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RecallIQ.Core.Interfaces;
using RecallIQ.Indexing;
using RecallIQ.Indexing.Parsers;
using Xunit;

namespace RecallIQ.Tests.Indexing;

public class DocumentParserFactoryTests
{
    private readonly DocumentParserFactory _factory;

    public DocumentParserFactoryTests()
    {
        var ocrService = new Mock<IOcrService>();
        var parsers = new IDocumentParser[]
        {
            new PdfDocumentParser(Mock.Of<ILogger<PdfDocumentParser>>(), ocrService.Object),
            new DocxDocumentParser(Mock.Of<ILogger<DocxDocumentParser>>()),
            new TextDocumentParser(Mock.Of<ILogger<TextDocumentParser>>()),
            new MarkdownDocumentParser(Mock.Of<ILogger<MarkdownDocumentParser>>()),
            new ImageDocumentParser(Mock.Of<ILogger<ImageDocumentParser>>(), ocrService.Object)
        };
        _factory = new DocumentParserFactory(parsers);
    }

    [Theory]
    [InlineData("document.pdf", true)]
    [InlineData("document.docx", true)]
    [InlineData("readme.txt", true)]
    [InlineData("notes.md", true)]
    [InlineData("image.png", true)]
    [InlineData("photo.jpg", true)]
    [InlineData("scan.tiff", true)]
    [InlineData("program.exe", false)]
    [InlineData("data.csv", false)]
    public void IsSupportedFile_ReturnsExpected(string fileName, bool expected)
    {
        _factory.IsSupportedFile(fileName).Should().Be(expected);
    }

    [Theory]
    [InlineData("test.pdf")]
    [InlineData("test.docx")]
    [InlineData("test.txt")]
    [InlineData("test.md")]
    [InlineData("test.png")]
    [InlineData("test.jpg")]
    [InlineData("test.tiff")]
    public void GetParser_SupportedFiles_ReturnsParser(string fileName)
    {
        var parser = _factory.GetParser(fileName);
        parser.Should().NotBeNull();
    }

    [Fact]
    public void GetParser_UnsupportedFile_ReturnsNull()
    {
        var parser = _factory.GetParser("file.xyz");
        parser.Should().BeNull();
    }
}
