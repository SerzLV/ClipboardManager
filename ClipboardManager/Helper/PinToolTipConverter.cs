using System.Globalization;
using System.Windows.Data;

namespace ClipboardManager.Helper;

public sealed class PinToolTipConverter : IMultiValueConverter
{
    public object Convert(
        object[] values,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        var isPinned = values.Length > 0 && values[0] is bool value && value;
        var addText = values.Length > 1 ? values[1] as string : null;
        var removeText = values.Length > 2 ? values[2] as string : null;

        return isPinned
            ? removeText ?? "Remove from favorites"
            : addText ?? "Add to favorites";
    }

    public object[] ConvertBack(
        object value,
        Type[] targetTypes,
        object parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
