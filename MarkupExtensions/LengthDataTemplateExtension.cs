namespace Microsoft.Maui.Controls.Extensions
{
    public class LengthDataTemplateExtension : IMarkupExtension<DataTemplate>
    {
        public DataTemplate? Template { get; set; }
        public object? Length { get; set; }

        public DataTemplate ProvideValue(IServiceProvider serviceProvider)
        {
            if (Template == null)
            {
                throw new ArgumentNullException(nameof(Template));
            }

            if (Length is BindingBase binding)
            {
                Template.Bindings[ItemsLayout.LengthProperty] = binding;
            }
            else
            {
                Template.Values[ItemsLayout.LengthProperty] = Length?.ToString();
            }

            return Template;
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
    }
}
