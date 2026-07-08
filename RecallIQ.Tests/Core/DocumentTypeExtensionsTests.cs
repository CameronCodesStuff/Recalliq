using FluentAssertions;
using RecallIQ.Core.Enums;
using RecallIQ.Core.Extensions;
using Xunit;

namespace RecallIQ.Tests.Core;

public class DocumentTypeExtensionsTests
{
    [Theory]
    [InlineData(".pdf", DocumentType.Pdf)]
    [InlineData(".docx", DocumentType.Docx)]
    [InlineData(".txt", DocumentType.Txt)]
    [InlineData(".md", DocumentType.Markdown)]
    [InlineData(".markdown", DocumentType.Markdown)]
    [InlineData(".png", DocumentType.Png)]
    [InlineData(".jpg", DocumentType.Jpg)]
    [InlineData(".jpeg", DocumentType.Jpg)]
    [InlineData(".tiff", DocumentType.Tiff)]
    [InlineData(".tif", DocumentType.Tiff)]
    [InlineData(".xyz", DocumentType.Unknown)]
    public void FromExtension_ReturnsCorrectType(string extension, DocumentType expected)
    {
        DocumentTypeExtensions.FromExtension(extension).Should().Be(expected);
    }

    [Theory]
    [InlineData(".pdf", true)]
    [InlineData(".docx", true)]
    [InlineData(".txt", true)]
    [InlineData(".md", true)]
    [InlineData(".png", true)]
    [InlineData(".xyz", false)]
    [InlineData(".exe", false)]
    [InlineData("", false)]
    public void IsSupportedExtension_ReturnsExpected(string extension, bool expected)
    {
        DocumentTypeExtensions.IsSupportedExtension(extension).Should().Be(expected);
    }

    [Theory]
    [InlineData(DocumentType.Png, true)]
    [InlineData(DocumentType.Jpg, true)]
    [InlineData(DocumentType.Tiff, true)]
    [InlineData(DocumentType.Pdf, false)]
    [InlineData(DocumentType.Docx, false)]
    [InlineData(DocumentType.Txt, false)]
    public void IsImageType_ReturnsExpected(DocumentType type, bool expected)
    {
        type.IsImageType().Should().Be(expected);
    }

    [Fact]
    public void GetDisplayName_ReturnsNonEmptyForAllTypes()
    {
        foreach (DocumentType type in Enum.GetValues<DocumentType>())
        {
            type.GetDisplayName().Should().NotBeNullOrEmpty();
        }
    }

    [Theory]
    [InlineData(".PDF", DocumentType.Pdf)]
    [InlineData(".Docx", DocumentType.Docx)]
    [InlineData(".TXT", DocumentType.Txt)]
    public void FromExtension_IsCaseInsensitive(string extension, DocumentType expected)
    {
        DocumentTypeExtensions.FromExtension(extension).Should().Be(expected);
    }
}
