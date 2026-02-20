namespace Microsoft.Maui.Controls.Extensions
{
    public static class LayoutExtensions
    {
        public static BindableProperty AbsoluteWidthProperty => AbsoluteWidthPropertyKey.BindableProperty;

        private static readonly BindablePropertyKey AbsoluteWidthPropertyKey = BindableProperty.CreateAttachedReadOnly(nameof(GetAbsoluteWidth).Substring(3), typeof(double), typeof(Layout), 0d, defaultValueCreator: bindable => DefaultAbsoluteSizeCreator(bindable, AbsoluteHeightProperty));

        public static BindableProperty AbsoluteHeightProperty => AbsoluteHeightPropertyKey.BindableProperty;

        private static readonly BindablePropertyKey AbsoluteHeightPropertyKey = BindableProperty.CreateAttachedReadOnly(nameof(GetAbsoluteHeight).Substring(3), typeof(double), typeof(Layout), 0d, defaultValueCreator: bindable => DefaultAbsoluteSizeCreator(bindable, AbsoluteWidthProperty));

        private static readonly BindableProperty IsRelativeSizeProperty = BindableProperty.CreateAttached(nameof(GetIsRelativeSize).Substring(3), typeof(bool), typeof(Controls.VisualElement), false);

        public static double GetAbsoluteWidth(this Layout layout) => (double)layout.GetValue(AbsoluteWidthProperty);
        public static double GetAbsoluteHeight(this Layout layout) => (double)layout.GetValue(AbsoluteHeightProperty);

        private static bool GetIsRelativeSize(this Controls.VisualElement visualElement) => visualElement.IsSet(IsRelativeSizeProperty) && (bool)visualElement.GetValue(IsRelativeSizeProperty);
        public static void SetIsRelativeSize(Controls.VisualElement visualElement, bool value)
        {
            visualElement.SetValue(IsRelativeSizeProperty, value);
            if (value)
            {
                ChildRemoved(visualElement);
            }
            else if (visualElement.Parent != null && (visualElement.Parent.IsSet(AbsoluteWidthProperty) || visualElement.Parent.IsSet(AbsoluteHeightProperty)))
            {
                ChildAdded(visualElement);
            }
        }

        private static double DefaultAbsoluteSizeCreator(BindableObject bindable, BindableProperty otherProperty)
        {
            if (!bindable.IsSet(otherProperty))
            {
                var layout = (Layout)bindable;

                layout.ChildAdded += ChildAdded;
                layout.ChildRemoved += ChildRemoved;

                foreach (var child in layout.Children.OfType<Controls.VisualElement>())
                {
                    ChildAdded(child);
                }
            }

            return 0d;
        }

        private static async void ChildSizeChanged(Controls.VisualElement child)
        {
            if (child.Parent is not Layout layout)
            {
                return;
            }

            double maxWidth = 0, maxHeight = 0;
            foreach (var sibling in layout.Children.OfType<Controls.VisualElement>())
            {
                if (GetIsRelativeSize(sibling))
                {
                    continue;
                }

                maxWidth = Math.Max(maxWidth, sibling.Width);
                maxHeight = Math.Max(maxHeight, sibling.Height);
            }

            // Make sure we start a new layout cycle
            await Task.Yield();

            layout.SetValue(AbsoluteWidthPropertyKey, maxWidth);
            layout.SetValue(AbsoluteHeightPropertyKey, maxHeight);
        }

        private static void ParentChanging(Controls.VisualElement ve)
        {
            if (ve.Parent == null)
            {
                return;
            }

            ve.Parent.ChildAdded -= ChildAdded;
            ve.Parent.ChildRemoved -= ChildRemoved;

            if (ve.Parent is Layout layout)
            {
                foreach (var child in layout.Children.OfType<Controls.VisualElement>())
                {
                    ChildRemoved(child);
                }
            }
        }

        private static void ChildAdded(object? sender, ElementEventArgs e)
        {
            if (e.Element is Controls.VisualElement ve)
            {
                ChildAdded(ve);
            }
        }

        private static void ChildAdded(Controls.VisualElement child)
        {
            if (!GetIsRelativeSize(child))
            {
                child.SizeChanged += ChildSizeChanged;
            }
        }

        private static void ChildRemoved(object? sender, ElementEventArgs e)
        {
            if (e.Element is Controls.VisualElement ve)
            {
                ChildRemoved(ve);
            }
        }

        private static void ChildRemoved(Controls.VisualElement ve)
        {
            ve.SizeChanged -= ChildSizeChanged;
        }

        private static void ChildSizeChanged(object? sender, EventArgs e)
        {
            if (sender is Controls.VisualElement ve)
            {
                ChildSizeChanged(ve);
            }
        }
    }
}
