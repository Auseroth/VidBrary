using System.Globalization;
using System.Windows.Data;

namespace MediaCatalog.Converters;

[ValueConversion(typeof(bool), typeof(string))]
public class BoolToChevronConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? "▲" : "▼";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}