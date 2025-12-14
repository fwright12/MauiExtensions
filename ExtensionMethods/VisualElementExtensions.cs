using Microsoft.Maui.Controls.Compatibility;
using System.ComponentModel;

namespace Microsoft.Maui.Controls
{
    public class RemoveBindingTriggerAction : TriggerAction<BindableObject>
    {
        public BindableProperty? Property { get; set; }

        protected override void Invoke(BindableObject sender)
        {
            sender.RemoveBinding(Property);
        }
    }
}

namespace Microsoft.Maui.Controls.Extensions
{
    public static class VisualElement
    {
        public static readonly BindableProperty AspectRequestProperty = BindableProperty.CreateAttached(nameof(GetAspectRequest).Substring(3), typeof(double), typeof(VisualElement), -1.0, defaultValueCreator: bindable =>
        {
            //((Controls.VisualElement)bindable).WidthRequest = 150;
            //return -1d;
            AspectRequestSetup((Controls.VisualElement)bindable);
            return -1d;
            ((Controls.VisualElement)bindable).SizeChanged += async (sender, e) =>
            {
                await Task.Delay(500);

                var ve = (Controls.VisualElement)sender;
                ve.WidthRequest = 150;
            };
            return -1d;
        });

        public static double GetAspectRequest(this Controls.VisualElement bindable) => (double)bindable.GetValue(AspectRequestProperty);
        public static void SetAspectRequest(this Controls.VisualElement bindable, double value) => bindable.SetValue(AspectRequestProperty, value);

        private static void AspectRequestSetup(Controls.VisualElement visualElement)
        {
            bool controlWidth = false, controlHeight = false;

            visualElement.SizeChanged += sizeChanged;
            sizeChanged(visualElement, EventArgs.Empty);

            async void sizeChanged(object? sender, EventArgs e)
            {
                var ve = sender as Controls.VisualElement;
                if (ve == null)
                {
                    return;
                }
                // Initial layout hasn't happened yet
                else if (ve.Frame.Size == Size.Zero || ve.Width < 0 || ve.Height < 0)
                {
                    return;
                }
                // We tried to set the width, and it didn't work
                else if (controlWidth && !AreSameSize(ve.Width, ve.WidthRequest))
                {
                    //return;
                }
                // We tried to set the height, and it didn't work
                else if (controlHeight && !AreSameSize(ve.Height, ve.HeightRequest))
                {
                    //return;
                }

                //Print.Log(ve.Height);
                var aspect = ve.GetAspectRequest();
                //Print.Log(ve.Height, ve.Width * aspect);
                if (AreSameSize(ve.Height * aspect, ve.Width))
                {
                    return;
                }

                var adjustWidth = ve.Width / ve.Height < aspect;

                // WidthRequest was set, and not by us - leave it alone
                if (ve.IsSet(Controls.VisualElement.WidthRequestProperty) && !controlWidth)
                {
                    controlWidth = false;
                    controlHeight = true;
                }
                // HeightRequest was set, and not by us - leave it alone
                else if (ve.IsSet(Controls.VisualElement.HeightRequestProperty) && !controlHeight)
                {
                    controlWidth = true;
                    controlHeight = false;
                }
                else
                {
                    controlWidth = ve.DesiredSize.Width == ve.Width;
                    controlHeight = ve.DesiredSize.Height == ve.Height;

                    // View is likely constrained in both directions by its parent - make it smaller so we don't overflow bounds
                    if (!controlWidth && !controlHeight)
                    {
                        controlWidth = !(controlHeight = adjustWidth);
                    }
                }

                // Make sure we start a new layout cycle
                await Task.Delay(1);

                //ve.SizeChanged -= sizeChanged;
                ve.MeasureInvalidated -= measureInvalidated;

                //measureInvalidated(ve, e);                

                // If view is completely unconstrained, we should control both dimensions to avoid the view trying to remeasure itself if one is changed
                if (controlWidth)
                {
                    ve.WidthRequest = controlHeight && !adjustWidth ? ve.Width : ve.Height * aspect;
                }
                if (controlHeight)
                {
                    ve.HeightRequest = controlWidth && adjustWidth ? ve.Height : ve.Width / aspect;
                }

                ve.MeasureInvalidated += measureInvalidated;
                //ve.SizeChanged += sizeChanged;
            }

            void measureInvalidated(object? sender, EventArgs e)
            {
                var ve = sender as Controls.VisualElement;
                if (ve == null)
                {
                    return;
                }

                ve.SizeChanged -= sizeChanged;
                ve.SizeChanged += sizeChanged;

                //await Task.Delay(1);

                if (controlWidth)
                {
                    ve.WidthRequest = -1;
                }
                if (controlHeight)
                {
                    ve.HeightRequest = -1;
                }
            }
        }

        private static bool AreSameSize(double size1, double size2) => Math.Abs(size1 - size2) < 1;

        public static Point PositionOn(this Controls.VisualElement child, Controls.VisualElement parent = null)
        {
            //return child.PositionOn(parent);

            Point point = Point.Zero;

            if (child?.Parent is ScrollView scroll)
            {
                point = point.Subtract(scroll.ScrollPos());
            }

            if (child == parent)
            {
                return point;
            }
            else if (child is null)
            {
                throw new Exception("child is not a descendant of parent");
            }

            return PositionOn(child.Parent<Controls.VisualElement>(), parent).Add(point.Add(new Point(child.X + child.TranslationX, child.Y + child.TranslationY)));
        }
    }
}

namespace Microsoft.Maui.Controls.Compatibility
{
    public static class VisualElementAdditions
    {
        public static BindableProperty VisibilityProperty = BindableProperty.CreateAttached("Visibility", typeof(double), typeof(VisualElement), 1.0, propertyChanged: (bindable, oldValue, newValue) =>
        {
            VisualElement visualElement = (VisualElement)bindable;
            visualElement.IsVisible = (visualElement.Opacity = (double)newValue) > 0;
        });

        public static double GetVisibility(this VisualElement visualElement) => (double)visualElement.GetValue(VisibilityProperty);

        public static void SetVisibility(this VisualElement visualElement, double value) => visualElement.SetValue(VisibilityProperty, value);
    }

    public static class VisualElementExtensions
    {
        public static void SizeRequest(this VisualElement element, Size size) => SizeRequest(element, size.Width, size.Height);

        public static void SizeRequest(this VisualElement element, double size) => SizeRequest(element, size, size);

        public static void SizeRequest(this VisualElement element, double width, double height)
        {
            element.WidthRequest = width;
            element.HeightRequest = height;
        }

        public static Size Measure(this VisualElement ve) => ve.Measure(double.PositiveInfinity, double.PositiveInfinity);

        public static Point PositionOn(this VisualElement child, VisualElement parent = null)
        {
            //return child.ositionOn(parent);

            Point point = Point.Zero;

            if (child?.Parent is ScrollView scroll)
            {
                point = point.Subtract(scroll.ScrollPos());
            }

            if (child == parent)
            {
                return point;
            }
            else if (child is null)
            {
                throw new Exception("child is not a descendant of parent");
            }

            return PositionOn(child.Parent<VisualElement>(), parent).Add(point.Add(new Point(child.X + child.TranslationX, child.Y + child.TranslationY)));
        }

        /*public static Point ositionOn(this View child, View parent)
        {
            if (child == parent || child is null)
            {
                return Point.Zero;
            }

            return child.ParentView().PositionOn(parent).Add(new Point(child.X, child.Y + child.TranslationY));
        }*/
    }
}
