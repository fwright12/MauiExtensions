using System.Globalization;
using static Microsoft.Maui.Controls.BindableProperty;

namespace Microsoft.Maui.Controls.Extensions
{
    [BindingValueConverter]
    public class TemplateToViewConverter : IValueConverter<object, View>
    {
        public BindableObject? Container { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value == null && parameter is DataTemplateSelector ? null : Templates.CreateView(Container!, value, parameter as DataTemplate);

        public object? ConvertBack(View? value, Type targetType, object? parameter, CultureInfo culture) => value?.BindingContext ?? value;
    }

    [ContentProperty(nameof(Path))]
    public class ViewBindingExtension : IMarkupExtension<BindingBase>
    {
        public string? Path { get; set; } = Binding.SelfPath;
        public BindingMode BindingMode { get; set; } = BindingMode.Default;
        public DataTemplate? Template { get; set; }
        public object? Source { get; set; }

        public BindingBase ProvideValue(IServiceProvider serviceProvider) => new Binding(Path, BindingMode, IPlatformApplication.Current?.Services.GetService<TemplateToViewConverter>(), Template, source: Source);

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
    }

    public static class Templates
    {
        public static BindingPropertyChangedDelegate CreateViewChangedDelegate<T>(Func<T, object> objectGetter, Func<T, DataTemplate> templateGetter, Action<T, View?> viewUpdated) where T : BindableObject => (bindable, oldValue, newValue) =>
        {
            T t = (T)bindable;

            object obj;
            if (newValue is DataTemplate template)
            {
                obj = objectGetter(t);
            }
            else
            {
                obj = newValue;
                template = templateGetter(t);
            }

            viewUpdated(t, CreateView(bindable, obj, template));
        };

        public static View? CreateView(BindableObject container, object? obj, DataTemplate? template)
        {
            if (template != null)
            {
                if (template is DataTemplateSelector selector)
                {
                    if (obj == null)
                    {
                        throw new ArgumentNullException($"{nameof(obj)} cannot be null when using a DataTemplateSelector");
                    }

                    template = selector.SelectTemplate(obj, container);
                }

                var view = template?.CreateContent() as View;
                if (view != null)
                {
                    view.BindingContext = obj;
                }

                return view;
            }
            else
            {
                return obj as View ?? new Label { Text = obj?.ToString() };
            }
        }
    }
}
