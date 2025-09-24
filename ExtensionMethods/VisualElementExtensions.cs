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
            BindableProperty? dimensionProperty = null;

            visualElement.SizeChanged += sizeChanged;
            sizeChanged(visualElement, EventArgs.Empty);

            async void sizeChanged(object? sender, EventArgs e)
            {
                var ve = sender as Controls.VisualElement;
                if (ve == null)
                {
                    return;
                }
                else if (ve.Frame.Size == Size.Zero || ve.Width < 0 || ve.Height < 0)
                {
                    return;
                }
                // We tried to set the width, and it didn't work
                else if (dimensionProperty == Controls.VisualElement.WidthRequestProperty && !AreSameSize(ve.Width, ve.WidthRequest))
                {
                    return;
                }
                // We tried to set the height, and it didn't work
                else if (dimensionProperty == Controls.VisualElement.HeightRequestProperty && !AreSameSize(ve.Height, ve.HeightRequest))
                {
                    return;
                }

                //Print.Log(ve.Height);
                var aspect = ve.GetAspectRequest();
                if (AreSameSize(ve.Height * aspect, ve.Width))
                {
                    //return;
                }
                
                bool adjustWidth;
                // WidthRequest was set, and not by us - leave it alone
                if (ve.IsSet(Controls.VisualElement.WidthRequestProperty) && dimensionProperty != Controls.VisualElement.WidthRequestProperty)
                {
                    adjustWidth = false;
                }
                // HeightRequest was set, and not by us - leave it alone
                else if (ve.IsSet(Controls.VisualElement.HeightRequestProperty) && dimensionProperty != Controls.VisualElement.HeightRequestProperty)
                {
                    adjustWidth = true;
                }
                else
                {
                    var widthFlexible = ve.DesiredSize.Width == ve.Width;
                    var heightFlexible = ve.DesiredSize.Height == ve.Height;

                    if (widthFlexible == heightFlexible)
                    {
                        adjustWidth = widthFlexible == ve.Width / ve.Height < aspect;
                    }
                    else
                    {
                        adjustWidth = widthFlexible;
                    }
                }
                
                // Make sure we start a new layout cycle
                await Task.Delay(1);

                //ve.SizeChanged -= sizeChanged;
                ve.MeasureInvalidated -= measureInvalidated;

                //measureInvalidated(ve, e);

                if (adjustWidth)
                {
                    dimensionProperty = Controls.VisualElement.WidthRequestProperty;
                    ve.WidthRequest = ve.Height * aspect;
                }
                else
                {
                    dimensionProperty = Controls.VisualElement.HeightRequestProperty;
                    ve.HeightRequest = ve.Width / aspect;
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

                ve.SizeChanged += sizeChanged;

                if (dimensionProperty != null && ve.IsSet(dimensionProperty))
                {
                    //await Task.Delay(1);

                    if (dimensionProperty == Controls.VisualElement.WidthRequestProperty)
                    {
                        ve.WidthRequest = -1;
                    }
                    else if (dimensionProperty == Controls.VisualElement.HeightRequestProperty)
                    {
                        ve.HeightRequest = -1;
                    }
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
