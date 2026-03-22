using System.Globalization;

namespace Microsoft.Maui.Controls.Extensions;

public static class AggregatorBinding
{
    public static MultiBinding Create(object seed) => new MultiBinding
    {
        Converter = Converter.Instance,
        ConverterParameter = (ArraySegment<object> values) => seed
    };

    public static MultiBinding Add<TAccumulate, TSource>(this MultiBinding multiBinding, object? value, Func<TAccumulate, TSource, TAccumulate> accumulator)
    {
        if (multiBinding.ConverterParameter == null)
        {
            Func<ArraySegment<object>, object> fold = (ArraySegment<object> values) => default(TAccumulate)!;
            multiBinding.ConverterParameter = fold;
        }

        Add(multiBinding, value, (aggregate, next) => accumulator((TAccumulate)aggregate, (TSource)next)!);
        return multiBinding;
    }

    private static void Add(this MultiBinding multiBinding, object? value, Func<object, object, object> accumulator)
    {
        if (value == null)
        {
            return;
        }
        if (multiBinding.ConverterParameter is not Func<ArraySegment<object>, object> fold)
        {
            System.Diagnostics.Debug.WriteLine($"{multiBinding} cannot be used as an aggregator because it has a converter parameter");
            return;
        }

        Func<ArraySegment<object>, object> outerFold;
        if (value is BindingBase binding)
        {
            multiBinding.Bindings.Add(binding);
            outerFold = values => accumulator(fold(values.Slice(1)), values[0]);
        }
        else
        {
            outerFold = values => accumulator(fold(values), value);
        }

        multiBinding.ConverterParameter = outerFold;
    }

    private class Converter : IMultiValueConverter
    {
        public static readonly Converter Instance = new Converter();

        private Converter() { }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is Func<ArraySegment<object>, object> func)
            {
                var result = func(values);
                return result;
            }
            else
            {
                return null!;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null!;
    }
}
