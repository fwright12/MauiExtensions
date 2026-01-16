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
