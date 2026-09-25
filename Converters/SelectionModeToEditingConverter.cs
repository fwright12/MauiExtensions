using System.Globalization;

namespace Microsoft.Maui.Controls.Extensions;

[BindingValueConverter]
public class SelectionModeToEditingConverter : IValueConverter<SelectionMode, bool>
{
    public object? Convert(SelectionMode value, Type targetType, object? parameter, CultureInfo culture) => value != SelectionMode.None;

    public object? ConvertBack(bool value, Type targetType, object? parameter, CultureInfo culture) => value ? null : SelectionMode.None;
}
