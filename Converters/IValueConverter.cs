using System.Globalization;

namespace Microsoft.Maui.Controls;

public interface IValueConverter<TIn, TOut> : IValueConverter<TIn, TOut, object> { }
public interface IValueConverter<TIn, TOut, TParameter> : IValueConverter
{
    object? Convert(TIn? value, Type targetType, TParameter? parameter, CultureInfo culture);
    object? ConvertBack(TOut? value, Type targetType, TParameter? parameter, CultureInfo culture);

    object? IValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => TryCast(value, out TIn? t) && TryCast(parameter, out TParameter? tp) ? Convert(t, targetType, tp, culture) : null;
    object? IValueConverter.ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => TryCast(value, out TOut? t) && TryCast(parameter, out TParameter? tp) ? ConvertBack(t, targetType, tp, culture) : null;

    private static bool TryCast<T>(object? value, out T? result)
    {
        if (value is T t)
        {
            result = t;
            return true;
        }
        else
        {
            result = default;
            return value == null && !typeof(T).IsValueType;
        }
    }
}
