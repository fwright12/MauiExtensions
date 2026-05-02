namespace Microsoft.Maui.Controls.Extensions;

public class AutoSelectTemplateBehavior : Behavior<Controls.VisualElement>
{
    public ResourceDataTemplateSelector TemplateSelector { get; set; } = ResourceDataTemplateSelector.Instance;
    public BindableProperty? TemplateSelectorProperty { get; set; }

    protected override void OnAttachedTo(Controls.VisualElement bindable)
    {
        base.OnAttachedTo(bindable);
        bindable.Behaviors.Remove(this);

        SetTemplateSelector(bindable);
    }

    public void SetTemplateSelector(Controls.VisualElement visualElement)
    {
        if (ShouldSetTemplateSelector(visualElement))
        {
            if (visualElement.IsLoaded)
            {
                ElementLoaded(visualElement, EventArgs.Empty);
            }
            else
            {
                visualElement.Loaded += ElementLoaded;
            }
        }
    }

    private void ElementLoaded(object? sender, EventArgs e)
    {
        var ve = (Controls.VisualElement)sender!;

        ve.Loaded -= ElementLoaded;

        if (ShouldSetTemplateSelector(ve))
        {
            ve.SetValue(TemplateSelectorProperty, TemplateSelector);
        }
    }

    protected virtual bool ShouldSetTemplateSelector(Controls.VisualElement ve) => TemplateSelectorProperty != null && !ve.IsSet(TemplateSelectorProperty);
}

public class AutoSelectBindableLayoutTemplateBehavior : AutoSelectTemplateBehavior
{
    public AutoSelectBindableLayoutTemplateBehavior()
    {
        TemplateSelectorProperty = BindableLayout.ItemTemplateSelectorProperty;
    }

    protected override bool ShouldSetTemplateSelector(Controls.VisualElement ve) => base.ShouldSetTemplateSelector(ve) && !ve.IsSet(BindableLayout.ItemTemplateProperty) && ve.IsSet(BindableLayout.ItemsSourceProperty);
}

public class AutoSelectContentViewTemplateBehavior : AutoSelectTemplateBehavior
{
    public AutoSelectContentViewTemplateBehavior()
    {
        TemplateSelectorProperty = ContentView.ContentTemplateProperty;
    }

    protected override bool ShouldSetTemplateSelector(Controls.VisualElement ve) => base.ShouldSetTemplateSelector(ve) && ve.IsSet(ContentView.ItemSourceProperty);
}

[ContentProperty(nameof(Template))]
public class DataTypeTemplate// : DataTemplate
{
    public required DataTemplate Template { get; set; }
    public Type? Type { get; set; }

    //public DataTypeTemplate() : base()
    //{
    //    LoadTemplate = () => Template?.CreateContent();
    //}

    static DataTypeTemplate()
    {
        TryAppendMapping(Handlers.Items.CollectionViewHandler.Mapper, new AutoSelectTemplateBehavior { TemplateSelectorProperty = ItemsView.ItemTemplateProperty });
        TryAppendMapping(Handlers.Items.CarouselViewHandler.Mapper, new AutoSelectTemplateBehavior { TemplateSelectorProperty = ItemsView.ItemTemplateProperty });
        TryAppendMapping(Maui.Handlers.LayoutHandler.Mapper, new AutoSelectBindableLayoutTemplateBehavior());
        TryAppendMapping(Maui.Handlers.ContentViewHandler.Mapper, new AutoSelectContentViewTemplateBehavior());
    }

    private static readonly string MAPPER_PROPERTY_KEY = "UseResourceDataTemplateSelector";

    private static void TryAppendMapping<TVirtualView, TViewHandler>(IPropertyMapper<TVirtualView, TViewHandler> mapper, AutoSelectTemplateBehavior behavior)
        where TVirtualView : IElement
        where TViewHandler : IElementHandler
    {
        if (mapper.GetProperty(MAPPER_PROPERTY_KEY) != null)
        {
            return;
        }

        mapper.AppendToMapping(MAPPER_PROPERTY_KEY, (handler, view) =>
        {
            if (view is not Controls.VisualElement ve)
            {
                return;
            }

            behavior.SetTemplateSelector(ve);
        });
    }
}

public class ResourceDataTemplateSelector : DataTemplateSelector
{
    public static readonly ResourceDataTemplateSelector Instance = new ResourceDataTemplateSelector();

    private static readonly string BASE_TYPE_LOOKUP_KEY = "__hasProcessedDataTypeTemplates__";
    private static readonly DataTemplate DEFAULT = new DataTemplate(() =>
    {
        var label = new Label();
        label.SetBinding(Label.TextProperty, new Binding());
        return label;
    });

    protected ResourceDataTemplateSelector() { }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container) => OnSelectTemplate(item.GetType(), container);
    protected DataTemplate OnSelectTemplate(Type type, BindableObject container)
    {
        if (container is not Controls.VisualElement ve)
        {
            throw new InvalidOperationException($"{nameof(ResourceDataTemplateSelector)} can only be used on a VisualElement");
        }

        var dicts = GetAncestors(ve).OfType<Controls.VisualElement>().Select(ancestor => ancestor.Resources);
        if (Application.Current != null)
        {
            dicts = dicts.Append(Application.Current.Resources);
        }

        foreach (var dict in dicts)
        {
            TryProcess(dict);

            for (var baseType = type; baseType != null; baseType = baseType.BaseType)
            {
                foreach (var iType in AllInterfaces(baseType).Distinct().Prepend(baseType))
                {
                    if (dict.TryGetValue(GetKey(iType), out var value) && value is DataTypeTemplate dataTypeTemplate)
                    {
                        return dataTypeTemplate.Template;
                    }
                }
            }
        }

        return DEFAULT;
    }

    private static IEnumerable<Type> AllInterfaces(Type type)
    {
        foreach (var iType in type.GetInterfaces())
        {
            yield return iType;

            foreach (var superInterface in AllInterfaces(iType))
            {
                yield return superInterface;
            }
        }
    }

    private static bool TryGetTemplate(ResourceDictionary rd, Type type, out DataTemplate template)
    {
        var typeKey = GetKey(type);

        // Look for cached base type match
        var typeLookup = (IDictionary<string, DataTemplate>)rd[BASE_TYPE_LOOKUP_KEY];
        if (!typeLookup.TryGetValue(typeKey, out template!))
        {
            typeLookup[typeKey] = template = GetTemplate(rd, type)!;
        }

        if (template != null)
        {
            return true;
        }

        foreach (var merged in rd.MergedDictionaries)
        {
            if (TryGetTemplate(merged, type, out template))
            {
                return true;
            }
        }

        return false;
    }

    private static DataTemplate? GetTemplate(ResourceDictionary rd, Type type)
    {
        foreach (var dataTypeTemplate in rd.Values.OfType<DataTypeTemplate>())
        {
            if (dataTypeTemplate.Type == typeof(object) || type.IsAssignableTo(dataTypeTemplate.Type))
            {
                return dataTypeTemplate.Template;
            }
        }

        return null;
    }

    public static void TryProcess(ResourceDictionary resourceDictionary)
    {
        if (resourceDictionary.ContainsKey(BASE_TYPE_LOOKUP_KEY))
        {
            return;
        }

        try
        {
            var dataTypes = resourceDictionary.Where(kvp => kvp.Value is DataTypeTemplate).ToArray();
            foreach (var kvp in dataTypes)
            {
                var template = (DataTypeTemplate)kvp.Value;
                if (template.Type == null)
                {
                    System.Diagnostics.Debug.WriteLine($"{nameof(DataTypeTemplate.Type)} must be set on {nameof(DataTypeTemplate)} object to use as a resource");
                    continue;
                }

                resourceDictionary.Remove(kvp.Key);
                resourceDictionary.Add(GetKey(template.Type), template);
            }

            foreach (var merged in resourceDictionary.MergedDictionaries)
            {
                TryProcess(merged);
            }
        }
        finally
        {
            resourceDictionary[BASE_TYPE_LOOKUP_KEY] = new Dictionary<string, DataTypeTemplate>();
        }
    }

    private static string GetKey(Type type) => type.FullName ?? type.ToString();

    private static IEnumerable<Element> GetAncestors(Element element)
    {
        for (; element != null; element = element.Parent)
        {
            yield return element;
        }
    }
}
