namespace Microsoft.Maui.Controls.Extensions;

public static class ResourceDictionaryExtensions
{
    public static MauiAppBuilder UseExtensionsResources(this MauiAppBuilder app)
    {
        if (Application.Current != null)
        {
            ResourceDataTemplateSelector.TryProcess(Application.Current.Resources);
            Merge(Application.Current.Resources, new Styles());
        }

        return app;
    }

    public static IEnumerable<ResourceDictionary> AllDictionaries(this ResourceDictionary resourceDictionary)
    {
        foreach (var merged in resourceDictionary.MergedDictionaries)
        {
            yield return merged;

            foreach (var descendant in AllDictionaries(merged))
            {
                yield return descendant;
            }
        }
    }

    public static void Merge(this ResourceDictionary rd, ResourceDictionary other)
    {
        foreach (var key in EnumerateKeys(rd))
        {
            if (!other.TryGetValue(key, out var value) || value is not Style otherStyle || rd[key] is not Style style)
            {
                continue;
            }

            other.Remove(key);
            Merge(style, otherStyle);
        }

        rd.MergedDictionaries.Add(other);
    }

    public static void Merge(this Style style, Style other)
    {
        foreach (var behavior in other.Behaviors.ToArray())
        {
            other.Behaviors.Remove(behavior);
            style.Behaviors.Add(behavior);
        }

        foreach (var setter in other.Setters)
        {
            if (!style.Setters.Any(s => s.Property == setter.Property))
            {
                style.Setters.Add(setter);
            }
        }

        foreach (var trigger in other.Triggers.ToArray())
        {
            other.Triggers.Remove(trigger);
            style.Triggers.Add(trigger);
        }
    }

    public static IEnumerable<string> EnumerateKeys(this ResourceDictionary rd) => EnumerateAll(rd).Select(kvp => kvp.Key);
    public static IEnumerable<object> EnumerateValues(this ResourceDictionary rd) => EnumerateAll(rd).Select(kvp => kvp.Value);
    public static IEnumerable<KeyValuePair<string, object>> EnumerateAll(this ResourceDictionary rd)
    {
        foreach (var kvp in rd)
        {
            yield return kvp;
        }

        foreach (var merged in rd.MergedDictionaries)
        {
            foreach (var kvp in EnumerateAll(merged))
            {
                yield return kvp;
            }
        }
    }
}
