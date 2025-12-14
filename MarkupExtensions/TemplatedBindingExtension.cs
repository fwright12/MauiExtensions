namespace Microsoft.Maui.Controls
{
    [ContentProperty(nameof(Path))]
    public class TemplatedBindingExtension : IMarkupExtension<BindingBase>
    {
        public string? Path { get; set; }
        public DataTemplate? Template { get; set; }

        public BindingBase ProvideValue(IServiceProvider serviceProvider)
        {
            var be = new BindingExtension
            {
                Converter = IPlatformApplication.Current?.Services.GetService<ObjectToViewConverter>(),
                ConverterParameter = Template
            };
            if (Path != null)
            {
                be.Path = Path;
            }

            return ((IMarkupExtension<BindingBase>)be).ProvideValue(serviceProvider);
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
    }

    [ContentProperty(nameof(Path))]
    public class TemplatedContentBindingExtension : IMarkupExtension<BindingBase>
    {
        public string? Path { get; set; }
        public DataTemplate? Template { get; set; }

        public BindingBase ProvideValue(IServiceProvider serviceProvider)
        {
            var be = new BindingExtension
            {
                Converter = IPlatformApplication.Current?.Services.GetService<ObjectToViewConverter>(),
                ConverterParameter = Template
            };
            if (Path != null)
            {
                be.Path = Path;
            }

            return ((IMarkupExtension<BindingBase>)be).ProvideValue(serviceProvider);
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
    }
}
