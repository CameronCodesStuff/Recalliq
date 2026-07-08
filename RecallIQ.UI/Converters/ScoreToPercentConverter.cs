using Microsoft.UI.Xaml.Data;

namespace RecallIQ.UI.Converters;

public sealed class ScoreToPercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double score)
            return $"{score * 100:F1}%";
        return "0%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
