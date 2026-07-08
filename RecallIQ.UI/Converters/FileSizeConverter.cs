using Microsoft.UI.Xaml.Data;
using RecallIQ.Core.Extensions;

namespace RecallIQ.UI.Converters;

public sealed class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is long bytes)
            return StringExtensions.FormatFileSize(bytes);
        return "0 B";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
