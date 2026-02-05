namespace Microsoft.Maui.Controls.Extensions
{
    public static class ItemsLayout
    {
        public static readonly BindableProperty LengthProperty = BindableProperty.CreateAttached(nameof(GetLength).Substring(3), typeof(string), typeof(Controls.VisualElement), GridLength.Auto.ToString());

        public static string GetLength(this Controls.VisualElement bindable) => (string)bindable.GetValue(LengthProperty);
        public static void SetLength(this Controls.VisualElement bindable, string value) => bindable.SetValue(LengthProperty, value);



        private static readonly GridLengthTypeConverter GridLengthTypeConverter = new GridLengthTypeConverter();
        public static GridLength GetGridLength(this Controls.VisualElement bindable) => (GridLength)GridLengthTypeConverter.ConvertFrom(GetLength(bindable))!;
        public static void SetGridLength(this Controls.VisualElement bindable, GridLength value) => bindable.SetValue(LengthProperty, value.ToString());

        private static readonly string ITEMS_LAYOUT_EXTENSIONS_KEY = "ItemsLayoutExtensions";

        public static readonly BindableProperty ItemLengthProperty = BindableProperty.CreateAttached(nameof(GetItemLength).Substring(3), typeof(double), typeof(Controls.ItemsLayout), -1d, defaultValueCreator: bindable =>
        {
            var mapper = Handlers.Items.CollectionViewHandler.Mapper;
            if (mapper.GetProperty(ITEMS_LAYOUT_EXTENSIONS_KEY) == null)
            {
                mapper.AppendToMapping(ITEMS_LAYOUT_EXTENSIONS_KEY, (handler, view) =>
                {
                    view.ChildAdded += ItemsChildAdded;
                });
            }

            return -1d;
        });

        public static double GetItemLength(this Controls.ItemsLayout bindable) => (double)bindable.GetValue(ItemLengthProperty);
        public static void SetItemLength(this Controls.ItemsLayout bindable, double value) => bindable.SetValue(ItemLengthProperty, value);

        private static void ItemsChildAdded(object? sender, ElementEventArgs e)
        {
            if (sender == null)
            {
                return;
            }

            if (e.Element is not Controls.VisualElement ve)
            {
                return;
            }

            var itemsView = (StructuredItemsView)sender;
            if (itemsView.ItemsLayout is not Controls.ItemsLayout layout || !layout.IsSet(ItemLengthProperty))
            {
                return;
            }

            var length = ve.IsSet(LengthProperty) ? ve.GetGridLength() : new GridLength(1, GridUnitType.Star);
            if (length == GridLength.Auto)
            {
                return;
            }

            double value = length.Value;
            if (length.IsStar)
            {
                value *= layout.GetItemLength();
            }

            if (layout.Orientation == ItemsLayoutOrientation.Horizontal)
            {
                ve.WidthRequest = value;
            }
            else
            {
                ve.HeightRequest = value;
            }
        }
    }
}
