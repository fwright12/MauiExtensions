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

    public ResourceDictionary? Resources { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container) => OnSelectTemplate(item.GetType(), container);
    protected DataTemplate OnSelectTemplate(Type type, BindableObject container)
    {
        IEnumerable<ResourceDictionary> dicts;
        if (Resources != null)
        {
            dicts = [Resources];
        }
        else if (container is Controls.VisualElement ve)
        {
            dicts = GetAncestors(ve).OfType<Controls.VisualElement>().Select(ancestor => ancestor.Resources);
            if (Application.Current != null)
            {
                dicts = dicts.Append(Application.Current.Resources);
            }
        }
        else
        {
            throw new InvalidOperationException($"{nameof(Resources)} must be set if {nameof(ResourceDataTemplateSelector)} is not used on a VisualElement");
        }

        var typeHierarchy = TypeHierarchy(type).Distinct().ToArray();

        foreach (var dict in dicts)
        {
            TryProcess(dict);

            foreach (var baseType in typeHierarchy)
            {
                if (dict.TryGetValue(GetKey(baseType), out var value) && value is DataTypeTemplate dataTypeTemplate)
                {
                    return dataTypeTemplate.Template;
                }
            }
        }

        return DEFAULT;
    }

    private static IEnumerable<Type> TypeHierarchy(Type type)
    {
        for (var baseType = type; baseType != null;)
        {
            yield return baseType;

            IEnumerable<Type> interfaces = baseType.GetInterfaces();
            baseType = baseType.BaseType;
            if (baseType != null)
            {
                interfaces = interfaces.Except(baseType.GetInterfaces());
            }

            foreach (var iType in interfaces)
            {
                yield return iType;
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
            // Inefficient, but we have no choice - ResourceDictionary provides no key/value enumeration over a dictionary merged via the Source property
            foreach (var key in resourceDictionary.Keys.ToArray())
            {
                var template = resourceDictionary[key] as DataTypeTemplate;
                if (template == null)
                {
                    continue;
                }
                else if (template.Type == null)
                {
                    System.Diagnostics.Debug.WriteLine($"{nameof(DataTypeTemplate.Type)} must be set on {nameof(DataTypeTemplate)} object to use as a resource");
                    continue;
                }

                resourceDictionary.Remove(key);
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
