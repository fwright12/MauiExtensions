using System.Globalization;

namespace Microsoft.Maui.Controls.Extensions
{
    [ContentProperty(nameof(Value))]
    public class PercentExtension : IMarkupExtension<BindingBase>
    {
        public double Value { get; set; }

        public BindingBase ProvideValue(IServiceProvider serviceProvider)
        {
            var valueProvider = serviceProvider?.GetService<IProvideValueTarget>() ?? throw new ArgumentException();

            var targetProperty = (valueProvider.TargetObject as Setter)?.Property ?? valueProvider.TargetProperty as BindableProperty;
            if (targetProperty == null)
            {
                throw new InvalidOperationException("Cannot determine property to provide the value for.");
            }

            BindableProperty absoluteProperty;
            if (targetProperty.PropertyName.Contains("Width"))
            {
                absoluteProperty = LayoutExtensions.AbsoluteWidthProperty;
            }
            else if (targetProperty.PropertyName.Contains("Height"))
            {
                absoluteProperty = LayoutExtensions.AbsoluteHeightProperty;
            }
            else
            {
                throw new InvalidOperationException("Setting a relative size value is only supported on Width or Height properties.");
            }
            
            if (valueProvider.TargetObject is Controls.VisualElement ve)
            {
                LayoutExtensions.SetIsRelativeSize(ve, true);
                return CreateBinding(absoluteProperty);
            }
            else if (valueProvider.TargetObject is Setter setter)
            {
                return new Binding(Binding.SelfPath, BindingMode.OneTime, converter: new PeekConverter(this, targetProperty, absoluteProperty), source: RelativeBindingSource.Self);
            }
            else
            {
                throw new InvalidOperationException($"Setting a relative size value is not supported on {valueProvider.TargetObject}.");
            }
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);

        private BindingBase CreateBinding(BindableProperty targetProperty) => AttachedBinding.Create(string.Join(".", nameof(Element.Parent), targetProperty.PropertyName), targetProperty, converter: SizeFactorConverter.Instance, converterParameter: Value / 100, source: RelativeBindingSource.Self);

        private class PeekConverter : IValueConverter
        {
            private static readonly double DUMMY_VALUE = -12d;

            public PercentExtension Extension { get; }
            public BindableProperty TargetProperty { get; }
            public BindableProperty AbsoluteProperty { get; }

            public PeekConverter(PercentExtension extension, BindableProperty targetProperty, BindableProperty absoluteProperty)
            {
                Extension = extension;
                TargetProperty = targetProperty;
                AbsoluteProperty = absoluteProperty;
            }

            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                if (value is not BindableObject bindable)
                {
                    return value;
                }

                bindable.PropertyChanged += DummyValueSet;

                return DUMMY_VALUE;
            }

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }

            private void DummyValueSet(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (sender is not Controls.VisualElement ve || e.PropertyName != TargetProperty.PropertyName || !Equals(ve.GetValue(TargetProperty), DUMMY_VALUE))
                {
                    return;
                }

                ve.PropertyChanged -= DummyValueSet;

                LayoutExtensions.SetIsRelativeSize(ve, true);
                ve.SetBinding(TargetProperty, Extension.CreateBinding(AbsoluteProperty));
            }
        }

        private class SizeFactorConverter : IValueConverter
        {
            public static readonly SizeFactorConverter Instance = new SizeFactorConverter();

            private SizeFactorConverter() { }

            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                if (value is not double d)
                {
                    return value;
                }

                if (d != -1 && parameter is double factor)
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

                if (d != -1 && parameter is double factor)
                {
                    d /= factor;
                }

                return d;
            }
        }
    }
}
