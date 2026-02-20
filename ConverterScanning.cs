using System.Reflection;

namespace Microsoft.Maui.Controls
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BindingValueConverterAttribute : Attribute { }

    public class ConverterExtension : ServicesInstanceExtension { }

    [ContentProperty(nameof(Type))]
    public class ServicesInstanceExtension : IMarkupExtension
    {
        public Type? Type { get; set; }

        public object? ProvideValue(IServiceProvider serviceProvider) => Type == null ? null : IPlatformApplication.Current?.Services.GetService(Type);
    }

    public static class ConverterFactory
    {
        public static IValueConverter Create<T>() => Create(typeof(T));

        public static IValueConverter Create(Type type) => IPlatformApplication.Current?.Services.GetService(type) as IValueConverter ?? Activator.CreateInstance(type) as IValueConverter ?? throw new ArgumentException($"{type} must implement {typeof(IValueConverter)}");
    }

    public static class MultiConverterFactory
    {
        public static IMultiValueConverter Create<T>() => Create(typeof(T));

        public static IMultiValueConverter Create(Type type) => IPlatformApplication.Current?.Services.GetService(type) as IMultiValueConverter ?? Activator.CreateInstance(type) as IMultiValueConverter ?? throw new ArgumentException($"{type} must implement {typeof(IMultiValueConverter)}");
    }


    public static class ConverterScanning
    {
        public static MauiAppBuilder RegisterConverters(this MauiAppBuilder builder)
        {
            foreach (var assembly in new Assembly[] { Assembly.GetExecutingAssembly(), Assembly.GetCallingAssembly() })
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.GetCustomAttributes<BindingValueConverterAttribute>().Any())
                    {
                        builder.Services.AddSingleton(type);
                    }
                }
            }

            return builder;
        }
    }
}
