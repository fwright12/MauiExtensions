namespace Microsoft.Maui.Controls.Extensions;

public class ThicknessExtension : IMarkupExtension<MultiBinding>
{
    public object? Left { get; set; }
    public object? Top { get; set; }
    public object? Right { get; set; }
    public object? Bottom { get; set; }

    public object? Horizontal { get; set; }
    public object? Vertical { get; set; }

    public object? Uniform { get; set; }

    public MultiBinding ProvideValue(IServiceProvider serviceProvider)
    {
        var valueProvider = serviceProvider?.GetService<IProvideValueTarget>() ?? throw new ArgumentException();
        var targetProperty = (valueProvider.TargetObject as Setter)?.Property ?? valueProvider.TargetProperty as BindableProperty;

        Thickness seed;
        if (targetProperty != null)
        {
            if (targetProperty.DefaultValue is Thickness thickness)
            {
                seed = thickness;
            }
            else
            {
                throw new InvalidOperationException($"{nameof(ThicknessExtension)} can only be used for properties of type {typeof(Thickness)}");
            }
        }
        else
        {
            seed = Thickness.Zero;
        }

        var binding = AggregatorBinding.Create(seed);

        binding.Add(Uniform, (Thickness aggregate, double value) => new Thickness(value));

        binding.Add(Vertical, (Thickness aggregate, double value) => new Thickness(aggregate.Left,
         value, aggregate.Right, value));
        binding.Add(Horizontal, (Thickness aggregate, double value) => new Thickness(value,
         aggregate.Top, value, aggregate.Bottom));

        binding.Add(Left, (Thickness aggregate, double value) => new Thickness(value,
         aggregate.Top, aggregate.Right, aggregate.Bottom));
        binding.Add(Top, (Thickness aggregate, double value) => new Thickness(aggregate.Left,
         value, aggregate.Right, aggregate.Bottom));
        binding.Add(Right, (Thickness aggregate, double value) => new Thickness(aggregate.Left,
         aggregate.Top, value, aggregate.Bottom));
        binding.Add(Bottom, (Thickness aggregate, double value) => new Thickness(aggregate.Left,
         aggregate.Top, aggregate.Right, value));

        return binding;
    }

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
