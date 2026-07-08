using RecallIQ.Core.Enums;

namespace RecallIQ.Core.Extensions;

public static class DocumentTypeExtensions
{
    private static readonly Dictionary<string, DocumentType> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = DocumentType.Pdf,
        [".docx"] = DocumentType.Docx,
        [".txt"] = DocumentType.Txt,
        [".md"] = DocumentType.Markdown,
        [".markdown"] = DocumentType.Markdown,
        [".png"] = DocumentType.Png,
        [".jpg"] = DocumentType.Jpg,
        [".jpeg"] = DocumentType.Jpg,
        [".tif"] = DocumentType.Tiff,
        [".tiff"] = DocumentType.Tiff,
    };

    private static readonly HashSet<string> SupportedExtensions = new(ExtensionMap.Keys, StringComparer.OrdinalIgnoreCase);

    public static DocumentType FromExtension(string extension)
    {
        return ExtensionMap.GetValueOrDefault(extension, DocumentType.Unknown);
    }

    public static bool IsSupportedExtension(string extension)
    {
        return SupportedExtensions.Contains(extension);
    }

    public static bool IsImageType(this DocumentType type)
    {
        return type is DocumentType.Png or DocumentType.Jpg or DocumentType.Tiff;
    }

    public static string GetDisplayName(this DocumentType type)
    {
        return type switch
        {
            DocumentType.Pdf => "PDF",
            DocumentType.Docx => "Word",
            DocumentType.Txt => "Text",
            DocumentType.Markdown => "Markdown",
            DocumentType.Png => "PNG Image",
            DocumentType.Jpg => "JPEG Image",
            DocumentType.Tiff => "TIFF Image",
            _ => "Unknown"
        };
    }
}
