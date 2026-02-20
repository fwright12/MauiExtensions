using System.Globalization;

namespace Microsoft.Maui.Controls.Extensions
{
    [BindingValueConverter]
    public class FactorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not double d)
            {
                return value;
            }

            if (parameter is double factor)
            {
                d *= factor;
            }

            return d;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not double d)
            {
                return value;
            }

            if (parameter is double factor)
            {
                d /= factor;
            }

            return d;
        }
    }
}
