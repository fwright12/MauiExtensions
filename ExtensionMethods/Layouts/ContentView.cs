using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Target = Microsoft.Maui.Controls.ContentView;

namespace Microsoft.Maui.Controls.Extensions
{
    public class ViewFromTemplate<TDeclaring> where TDeclaring : BindableObject
    {
        public readonly BindableProperty ItemTemplateProperty;
        public readonly BindableProperty ItemSourceProperty;
        public readonly BindableProperty EmptyViewProperty;

        private Action<TDeclaring, View> SetContent;

        public ViewFromTemplate(Action<TDeclaring, View> setContent, string templatePropertyName = "ContentTemplate", string itemPropertyName = "ItemSource", string emptyPropertyName = "EmptyView")
        {
            ItemTemplateProperty = BindableProperty.CreateAttached(templatePropertyName, typeof(ElementTemplate), typeof(TDeclaring), null, propertyChanged: (b, o, n) => UpdateContent((TDeclaring)b, (ElementTemplate)n));
            ItemSourceProperty = BindableProperty.CreateAttached(itemPropertyName, typeof(object), typeof(TDeclaring), null, propertyChanged: (b, o, n) => UpdateContent((TDeclaring)b, item: n));
            EmptyViewProperty = BindableProperty.CreateAttached(emptyPropertyName, typeof(object), typeof(TDeclaring), null, propertyChanged: (b, o, n) => UpdateContent((TDeclaring)b, emptyView: n));

            SetContent = setContent;
        }

        private void UpdateContent(TDeclaring bindable, ElementTemplate template = null, object item = null, object emptyView = null)
        {
            //contentView.Content = (template is DataTemplateSelector selector && item != null ? selector.SelectTemplate(item, contentView) : template)?.CreateContent() as View;

            if (template == null)
            {
                template = GetItemTemplate(bindable);
            }
            if (item == null)
            {
                item = GetItemSource(bindable);
            }
            if (emptyView == null)
            {
                emptyView = GetEmptyView(bindable);
            }

            if (item != null)
            {
                if (template is DataTemplateSelector selector)
                {
                    template = selector.SelectTemplate(item, bindable);
                }

                View view = item as View;

                if (view == null && template?.CreateContent() is View content)
                {
                    content.BindingContext = item;
                    view = content;
                }
                else
                {
                    return;
                    throw new Exception("Could not convert " + item + " to a view");
                }

                SetContent(bindable, view);
            }
            else if (emptyView != null)
            {
                SetContent(bindable, emptyView as View ?? (emptyView as ElementTemplate)?.CreateContent() as View ?? new Label
                {
                    Text = emptyView?.ToString(),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                });
            }
            else
            {
                SetContent(bindable, null);
            }
        }

        public ElementTemplate GetItemTemplate(TDeclaring bindable) => (ElementTemplate)bindable.GetValue(ItemTemplateProperty);
        public object GetItemSource(TDeclaring bindable) => bindable.GetValue(ItemSourceProperty);
        public object GetEmptyView(TDeclaring bindable) => bindable.GetValue(EmptyViewProperty);

        public void SetItemTemplate(TDeclaring bindable, ElementTemplate value) => bindable.SetValue(ItemTemplateProperty, value);
        public void SetItemSource(TDeclaring bindable, object value) => bindable.SetValue(ItemSourceProperty, value);
        public void SetEmptyView(TDeclaring bindable, object value) => bindable.SetValue(EmptyViewProperty, value);
    }

    public static class ContentView
    {
        private static readonly ViewFromTemplate<Target> Items = new ViewFromTemplate<Target>((contentView, view) => contentView.Content = view);

        public static readonly BindableProperty ContentTemplateProperty = Items.ItemTemplateProperty;
        public static readonly BindableProperty ItemSourceProperty = Items.ItemSourceProperty;
        public static readonly BindableProperty EmptyViewProperty = Items.EmptyViewProperty;

        public static ElementTemplate GetContentTemplate(this Target contentView) => Items.GetItemTemplate(contentView);
        public static object GetItemSource(this Target contentView) => Items.GetItemSource(contentView);
        public static object GetEmptyView(this Target contentView) => Items.GetEmptyView(contentView);

        public static void SetContentTemplate(this Target contentView, ElementTemplate value) => Items.SetItemTemplate(contentView, value);
        public static void SetItemSource(this Target contentView, object value) => Items.SetItemSource(contentView, value);
        public static void SetEmptyView(this Target contentView, object value) => Items.SetEmptyView(contentView, value);
    }

    public class TemplatedContent
    {
        public static object? CreateContent(ElementTemplate? template, object? item, BindableObject container)
        {
            if (template != null)
            {
                if (template is DataTemplateSelector selector)
                {
                    if (item == null)
                    {
                        return null;
                    }

                    template = selector.SelectTemplate(item, container);
                }

                var result = template.CreateContent();
                if (result is BindableObject bindable)
                {
                    bindable.BindingContext = item;
                }

                return result;
            }
            else if (item is not View && item != null)
            {
                //content = emptyView as View ?? (emptyView as ElementTemplate)?.CreateContent() as View ??
                return new Label
                {
                    Text = item.ToString(),
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                };
            }
            else
            {
                return item;
            }
        }
    }

    public static class IContentView
    {
        public static readonly BindableProperty ContentProperty = BindableProperty.CreateAttached(nameof(GetContent).Substring(3), typeof(object), typeof(Maui.IContentView), null, propertyChanged: (b, o, n) => UpdateContent((Maui.IContentView)b, item: n));
        public static readonly BindableProperty ContentTemplateProperty = BindableProperty.CreateAttached(nameof(GetContentTemplate).Substring(3), typeof(ElementTemplate), typeof(Maui.IContentView), null, propertyChanged: (b, o, n) => UpdateContent((Maui.IContentView)b, (ElementTemplate)n));

        public static readonly BindableProperty EmptyViewProperty = BindableProperty.CreateAttached(nameof(GetEmptyView).Substring(3), typeof(object), typeof(Maui.IContentView), null, propertyChanged: (b, o, n) => UpdateContent((Maui.IContentView)b, emptyView: n));
        public static readonly BindableProperty EmptyViewTemplateProperty = BindableProperty.CreateAttached(nameof(GetEmptyViewTemplate).Substring(3), typeof(ElementTemplate), typeof(Maui.IContentView), null, propertyChanged: (b, o, n) => UpdateContent((Maui.IContentView)b, emptyView: n));

        private static void UpdateContent(Maui.IContentView contentView, ElementTemplate? template = null, object? item = null, ElementTemplate? emptyViewTemplate = null, object? emptyView = null)
        {
            var content = TemplatedContent.CreateContent(template ?? GetContentTemplate(contentView), item ?? GetContent(contentView), (BindableObject)contentView)
                ?? TemplatedContent.CreateContent(emptyViewTemplate ?? GetEmptyViewTemplate(contentView), emptyView ?? GetEmptyView(contentView), (BindableObject)contentView);

            try
            {
                // Hope Content property has a setter and content is a View
                contentView.GetType().GetField(nameof(Maui.IContentView.Content))?.SetValue(contentView, content);
            }
            catch { }
        }

        public static object GetContent(Maui.IContentView bindable) => ((BindableObject)bindable).GetValue(ContentProperty);
        public static ElementTemplate GetContentTemplate(Maui.IContentView bindable) => (ElementTemplate)((BindableObject)bindable).GetValue(ContentTemplateProperty);
        public static object GetEmptyView(Maui.IContentView bindable) => ((BindableObject)bindable).GetValue(EmptyViewProperty);
        public static ElementTemplate GetEmptyViewTemplate(Maui.IContentView bindable) => (ElementTemplate)((BindableObject)bindable).GetValue(EmptyViewTemplateProperty);

        public static void SetContent(Maui.IContentView bindable, object value) => ((BindableObject)bindable).SetValue(ContentProperty, value);
        public static void SetContentTemplate(Maui.IContentView bindable, ElementTemplate value) => ((BindableObject)bindable).SetValue(ContentTemplateProperty, value);
        public static void SetEmptyView(Maui.IContentView bindable, object value) => ((BindableObject)bindable).SetValue(EmptyViewProperty, value);
        public static void SetEmptyViewTemplate(Maui.IContentView bindable, ElementTemplate value) => ((BindableObject)bindable).SetValue(EmptyViewTemplateProperty, value);
    }
}
