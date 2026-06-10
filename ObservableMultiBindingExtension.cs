using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Microsoft.Maui.Controls.Extensions;

[ContentProperty(nameof(Bindings))]
public class ObservableMultiBindingExtension : IMarkupExtension<MultiBinding>
{
    public BindingMode Mode { get; set; } = BindingMode.Default;
    public object? ConverterParameter { get; set; }
    public string? StringFormat { get; set; }
    public string? UpdateSourceEventName { get; set; }
    public object? TargetNullValue { get; set; }
    public object? FallbackValue { get; set; }

    public IList<BindingBase> Bindings { get; set; } = new List<BindingBase>();

    public MultiBinding ProvideValue(IServiceProvider serviceProvider) => new MultiBinding
    {
        Mode = Mode,
        ConverterParameter = ConverterParameter,
        Bindings = Bindings.ToList<BindingBase>(),
        StringFormat = StringFormat,
        FallbackValue = FallbackValue,
        TargetNullValue = TargetNullValue,
    }.AsObservable();

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}

public static class ObservableMultiBinding
{
    public static MultiBinding Create() => AsObservable(new MultiBinding());

    public static MultiBinding AsObservable(this MultiBinding binding)
    {
        binding.Converter = new MultiConverter();
        binding.Bindings.Add(new Binding(Binding.SelfPath, source: string.Empty, converter: new Converter(binding.Bindings.Count)));

        return binding;
    }

    private class MultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var collection = (IList)values[^1];

            for (int i = 0; i < values.Length - 1; i++)
            {
                if (!Equals(collection[i], values[i]))
                {
                    collection[i] = values[i];
                }
            }

            return collection;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => ((IEnumerable<object>)value).Append(value).ToArray();
    }

    private class Converter : IValueConverter
    {
        public int Count { get; }

        public Converter(int count)
        {
            Count = count;
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => new ObservableCollection<object>(Enumerable.Repeat<object>(null!, Count));

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
