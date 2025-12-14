using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Maui.Controls
{
    public class ControlTemplateSelector<T> : CompoundTemplateSelector<T> where T : TemplatedView
    {
        public ControlTemplate ControlTemplate { get; set; }

        protected override void Apply(T content)
        {
            content.ControlTemplate = ControlTemplate;
        }
    }

    public abstract class CompoundTemplateSelector : CompoundTemplateSelector<object> { }

    [ContentProperty(nameof(Inner))]
    public abstract class CompoundTemplateSelector<T> : DataTemplateSelector
    {
        public DataTemplateSelector Inner { get; set; }

        private Dictionary<DataTemplate, DataTemplate> Templates { get; } = new Dictionary<DataTemplate, DataTemplate>();

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            var template = Inner.SelectTemplate(item, container);

            if (!Templates.TryGetValue(template, out var compoundTemplate))
            {
                Templates.Add(template, compoundTemplate = new DataTemplate(() =>
                {
                    var content = template.CreateContent();
                    if (false == content is T t)
                    {
                        throw new NotSupportedException($"Inner template must return a value of type {typeof(T)}");
                    }

                    Apply(t);

                    return content;
                }));
            }

            return compoundTemplate;
        }

        protected abstract void Apply(T content);
    }
}
