using System.ComponentModel;
using System.Globalization;

namespace Microsoft.Maui.Controls.Extensions
{
    public class AttachedBindingExtension : IMarkupExtension<BindingBase>
    {
        public string Path { get; set; } = Binding.SelfPath;
        public BindableProperty? AttachedProperty { get; set; }
        public BindingMode Mode { get; set; } = BindingMode.Default;
        public IValueConverter? Converter { get; set; }
        public object? ConverterParameter { get; set; }
        public string? StringFormat { get; set; }
        public object? Source { get; set; }

        public BindingBase ProvideValue(IServiceProvider serviceProvider) => AttachedProperty == null ? null! : AttachedBinding.Create(Path, AttachedProperty, Mode, Converter, ConverterParameter, StringFormat, Source);

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
    }

    public static class AttachedBinding
    {
        public static MultiBinding Create(string path, BindableProperty property, BindingMode mode = BindingMode.Default, IValueConverter? converter = null, object? converterParameter = null, string? stringFormat = null, object? source = null)
        {
            var paths = path.Split(property.PropertyName);
            for (int i = 0; i < paths.Length; i++)
            {
                paths[i] = string.IsNullOrEmpty(paths[i]) ? Binding.SelfPath : paths[i].Trim('.');
            }

            var proxy = new AttachedPropertyProxy(property, new Binding(paths[1], mode, converter, converterParameter, stringFormat));

            return new MultiBinding
            {
                Converter = new BindingJoinConverter(proxy),
                Bindings =
                {
                    new Binding(paths[0], mode, source: source),
                    new Binding(nameof(AttachedPropertyProxy.Value), mode, source: proxy)
                }
            };
        }

        private class BindingJoinConverter : IMultiValueConverter
        {
            public AttachedPropertyProxy Proxy { get; }

            public BindingJoinConverter(AttachedPropertyProxy proxy)
            {
                Proxy = proxy;
            }

            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                Proxy.UpdateTarget(values[0] as BindableObject);
                return values[1];
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                return
                [
                    Proxy.Target!,
                    value
                ];
            }
        }

        private class AttachedPropertyProxy : BindableObject
        {
            public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(object), typeof(AttachedPropertyProxy));

            public BindableObject? Target { get; private set; }
            public BindableProperty Property { get; }
            public Binding? Binding { get; }

            public object? Value
            {
                get => GetValue(ValueProperty);
                set => SetValue(ValueProperty, value);
            }

            public AttachedPropertyProxy(BindableProperty property, Binding binding = null!)
            {
                Property = property;
                Binding = binding;

                // Initialize Value so MultiBinding doesn't get confused about mismatching types
                Value = Property.DefaultValue;
            }

            public void UpdateTarget(BindableObject? target)
            {
                if (target == Target)
                {
                    return;
                }

                if (Target != null)
                {
                    Target.PropertyChanged -= TargetPropertyChanged;
                    RemoveBinding(ValueProperty);
                }

                Target = target;

                if (Target != null)
                {
                    Target.PropertyChanged += TargetPropertyChanged;
                    TargetPropertyChanged();
                }
            }

            private void TargetPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == Property.PropertyName)
                {
                    TargetPropertyChanged();
                }
            }

            private void TargetPropertyChanged()
            {
                var value = Target.GetValue(Property);

                if (Binding == null)
                {
                    Value = value;
                }
                else
                {
                    RemoveBinding(ValueProperty);
                    Binding.Source = value;
                    SetBinding(ValueProperty, Binding);
                }
            }
        }
    }
}
