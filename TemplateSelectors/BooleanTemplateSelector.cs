﻿namespace Microsoft.Maui.Controls.Extensions
{
    public class BooleanTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? TrueTemplate { get; set; }
        public DataTemplate? FalseTemplate { get; set; }

        protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
        {
            var value = (bool)item;
            return value ? TrueTemplate : FalseTemplate;
        }
    }
}
