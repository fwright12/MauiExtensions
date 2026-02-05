using System.Globalization;

namespace Microsoft.Maui.Controls.Extensions
{
    public class RowItemsLayout : Controls.LinearItemsLayout
    {
        public static readonly BindableProperty UnitLengthProperty = BindableProperty.Create(nameof(UnitLength), typeof(double), typeof(Controls.LinearItemsLayout));

        public double UnitLength
        {
            get => (double)GetValue(UnitLengthProperty);
            set => SetValue(UnitLengthProperty, value);
        }

        public RowItemsLayout() : base(ItemsLayoutOrientation.Horizontal) { }
    }

    public class SizeDataTemplateExtension : IMarkupExtension<DataTemplate>
    {
        public DataTemplate? Template { get; set; }

        public object? Width { get; set; }
        public object? Height { get; set; }

        public DataTemplate ProvideValue(IServiceProvider serviceProvider) => Template == null ? Template! : new SizeDataTemplate(Template.CreateContent)
        {
            Width = Width,
            Height = Height
        };

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
    }

    public class SizeDataTemplate : DataTemplate
    {
        public object? Width
        {
            get => GetSizeValue(Controls.VisualElement.WidthRequestProperty);
            set => SetSizeValue(Controls.VisualElement.WidthRequestProperty, GetAbsoluteSizeValue(value));
        }

        public object? Height
        {
            get => GetSizeValue(Controls.VisualElement.HeightRequestProperty);
            set => SetSizeValue(Controls.VisualElement.HeightRequestProperty, GetAbsoluteSizeValue(value));
        }

#if true
        public SizeDataTemplate() : base() { }

        public SizeDataTemplate(Type type) : base(type) { }

        public SizeDataTemplate(Func<object> loadTemplate) : base(loadTemplate) { }

        private object? GetSizeValue(BindableProperty property)
        {
            if (Bindings.TryGetValue(property, out var binding))
            {
                return binding;
            }
            else if (Values.TryGetValue(property, out var value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        private void SetSizeValue(BindableProperty property, object? value)
        {
            return;
            if (value is BindingBase binding)
            {
                SetBinding(property, binding);
            }
            else if (value is not null)
            {
                SetValue(property, value);
            }
            else
            {
                Bindings.Remove(property);
                Values.Remove(property);
            }
        }
#else
        private Setter WidthSetter = new Setter
        {
            Property = Controls.VisualElement.WidthRequestProperty,
        };

        private Setter HeightSetter = new Setter
        {
            Property = Controls.VisualElement.HeightRequestProperty,
        };

        public SizeDataTemplate() : base()
        {
            InitStyle();
        }

        public SizeDataTemplate(Type type) : base(type)
        {
            InitStyle();
        }

        public SizeDataTemplate(Func<object> loadTemplate) : base(loadTemplate)
        {
            InitStyle();
        }

        private void InitStyle()
        {
            Style = new Style(typeof(Controls.VisualElement))
            {
                Setters =
                {
                    WidthSetter,
                    HeightSetter
                }
            };
        }

        private object GetSizeValue(BindableProperty property) => (property == Controls.VisualElement.WidthRequestProperty ? WidthSetter : HeightSetter).Value;

        private void SetSizeValue(BindableProperty property, object? value) => (property == Controls.VisualElement.WidthRequestProperty ? WidthSetter : HeightSetter).Value = value;
#endif

        private static object? GetAbsoluteSizeValue(object? value)
        {
            if (value is BindingBase binding)
            {
                return new MultiBinding
                {
                    Converter = SizeConverter.Instance,
                    Bindings =
                    {
                        binding,
                        CreateUnitBinding()
                    }
                };
            }
            else if (value is not null && TryGetGridLength(value, out var length))
            {
                if (length.IsAbsolute)
                {
                    return length.Value;
                }
                else if (length.IsStar)
                {
                    return CreateUnitBinding(FactorConverter.Instance, length.Value);
                }
                else if (length.IsAuto)
                {
                    return null!;
                }
            }

            return value;
        }

        private static Binding CreateUnitBinding(IValueConverter? converter = null, object? converterParameter = null) => new Binding(string.Join(".", StructuredItemsView.ItemsLayoutProperty.PropertyName, nameof(RowItemsLayout.UnitLength)), converter: converter, converterParameter: converterParameter, source: new RelativeBindingSource(RelativeBindingSourceMode.FindAncestor, typeof(StructuredItemsView)));

        private static readonly GridLengthTypeConverter GridLengthTypeConverter = new GridLengthTypeConverter();

        private static bool TryGetGridLength(object value, out GridLength gridLength)
        {
            if (GridLengthTypeConverter.CanConvertFrom(value.GetType()) && GridLengthTypeConverter.ConvertFrom(value) is GridLength gl)
            {
                gridLength = gl;
                return true;
            }

            gridLength = default;
            return false;
        }

        private class FactorConverter : IValueConverter
        {
            public static readonly FactorConverter Instance = new FactorConverter();

            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                if (value is null)
                {
                    return value;
                }

                var d = (double)value;
                if (parameter is double factor)
                {
                    d *= factor;
                }

                return d;
            }

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                if (value is null)
                {
                    return value;
                }

                var d = (double)value;
                if (parameter is double factor)
                {
                    d /= factor;
                }

                return d;
            }
        }

        private class SizeConverter : IMultiValueConverter
        {
            public static readonly SizeConverter Instance = new SizeConverter();

            private SizeConverter() { }

            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if (values.Length != 2)
                {
                    return Binding.DoNothing;
                }

                if (!TryGetGridLength(values[0], out var length) || values[1] is not double d)
                {
                    return Binding.DoNothing;
                }

                if (length.IsAbsolute)
                {
                    return length.Value;
                }
                else if (length.IsStar)
                {
                    return length.Value * d;
                }
                else
                {
                    return Binding.DoNothing;
                }
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }

}
