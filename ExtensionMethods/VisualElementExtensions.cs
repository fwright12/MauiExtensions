using Microsoft.Maui.Controls.Compatibility;
using System.ComponentModel;

namespace Microsoft.Maui.Controls.Extensions
{
    public static class VisualElement
    {
        public static readonly BindableProperty AspectRequestProperty = BindableProperty.CreateAttached(nameof(GetAspectRequest).Substring(3), typeof(double), typeof(VisualElement), -1d, defaultValueCreator: bindable =>
        {
            AspectRequestSetup((Controls.VisualElement)bindable);
            return -1d;
        });

        [TypeConverter(typeof(AspectRatioConverter))]
        public static double GetAspectRequest(this Controls.VisualElement bindable) => (double)bindable.GetValue(AspectRequestProperty);
        public static void SetAspectRequest(this Controls.VisualElement bindable, double value) => bindable.SetValue(AspectRequestProperty, value);

        private static readonly BindableProperty ArrangedLengthProperty = BindableProperty.Create(nameof(GetArrangedLength).Substring(3), typeof(double), typeof(Controls.VisualElement));

        private static double GetArrangedLength(this Controls.VisualElement visualElement) => (double)visualElement.GetValue(ArrangedLengthProperty);
        private static void SetArrangedLength(this Controls.VisualElement visualElement, double value) => visualElement.SetValue(ArrangedLengthProperty, value);

        private static readonly MauiReusableProjectionFactory ProjectionFactory = new MauiReusableProjectionFactory();

        private static ViewProjection Rotate(this ViewProjection projection) => projection is ViewProjection.Horizontal ? projection.Object.Project<ViewProjection.Vertical>(ProjectionFactory) : projection.Object.Project<ViewProjection.Horizontal>(ProjectionFactory);

        private static void AspectRequestSetup(Controls.VisualElement visualElement)
        {
            bool canControlWidth = true, canControlHeight = true;

            widthRequestChanged();
            heightRequestChanged();
            measureInvalidated(visualElement, EventArgs.Empty);

            void sizeRequestChanged(object? sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == Controls.VisualElement.WidthRequestProperty.PropertyName)
                {
                    widthRequestChanged();
                }
                else if (e.PropertyName == Controls.VisualElement.HeightRequestProperty.PropertyName)
                {
                    heightRequestChanged();
                }
            }

            void widthRequestChanged()
            {
                canControlWidth = !visualElement.IsSet(Controls.VisualElement.WidthRequestProperty);
            }

            void heightRequestChanged()
            {
                canControlHeight = !visualElement.IsSet(Controls.VisualElement.HeightRequestProperty);
            }

            async void sizeChanged(object? sender, EventArgs e)
            {
                var ve = sender as Controls.VisualElement;
                if (ve == null)
                {
                    return;
                }
                // Initial layout hasn't happened yet
                if (ve.Frame.Size == Size.Zero || ve.Width < 0 || ve.Height < 0)
                {
                    return;
                }

                var aspectRequest = ve.GetAspectRequest();

                Type projectionAxis;
                bool didOverflowBounds = true;
                // If a width/height request that we set overflows bounds, we will have to use to other direction
                if (canControlHeight && DidOverflowBounds<ViewProjection.Horizontal>(ve, canControlWidth))
                {
                    projectionAxis = typeof(ViewProjection.Horizontal);
                }
                else if (canControlWidth && DidOverflowBounds<ViewProjection.Vertical>(ve, canControlHeight))
                {
                    projectionAxis = typeof(ViewProjection.Vertical);
                }
                else
                {
                    // Allow tolerance of 1 to account for minor differences between requested and actual size
                    if (Math.Abs(Math.Truncate(ve.Width) - Math.Truncate(ve.Height * aspectRequest)) <= 1)
                    {
                        return;
                    }

                    didOverflowBounds = false;

                    var aspect = ve.Width / ve.Height;
                    // We will try to make the view bigger, provided we can control in that direction
                    if (aspect < aspectRequest || (canControlWidth && !canControlHeight))
                    {
                        projectionAxis = typeof(ViewProjection.Vertical);
                    }
                    else
                    {
                        projectionAxis = typeof(ViewProjection.Horizontal);
                    }
                }

                var projection = (ViewProjection)ve.Project(projectionAxis, ProjectionFactory);

                double length;
                // If we overflowed bounds, use the size in that direction stored on the previous layout cycle
                if (didOverflowBounds)
                {
                    length = GetArrangedLength(ve);
                }
                else
                {
                    length = projection.Length;
                    SetArrangedLength(ve, projection.Rotate().Length);
                }

                double orthogonalLength = length * Math.Pow(aspectRequest, projection is ViewProjection.Horizontal ? -1 : 1);
                // We will need to control both width and height, unless alignment is fill and parent is already controlling
                if (projection.Alignment == Primitives.LayoutAlignment.Fill)
                {
                    length = -1;
                }

                Size sizeRequest = ((SizeProjection)ProjectionFactory.Create(projection.SizeProjectionType)).Create(length, orthogonalLength);

                // Make sure we start a new layout cycle
                await Task.Yield();

                ve.MeasureInvalidated -= measureInvalidated;
                ve.PropertyChanged -= sizeRequestChanged;

                if (canControlWidth)
                {
                    if (sizeRequest.Width == -1)
                    {
                        ve.ClearValue(Controls.VisualElement.WidthRequestProperty);
                    }
                    else
                    {
                        ve.WidthRequest = sizeRequest.Width;
                    }
                }
                if (canControlHeight)
                {
                    if (sizeRequest.Height == -1)
                    {
                        ve.ClearValue(Controls.VisualElement.HeightRequestProperty);
                    }
                    else
                    {
                        ve.HeightRequest = sizeRequest.Height;
                    }
                }

                ve.MeasureInvalidated += measureInvalidated;
                ve.PropertyChanged += sizeRequestChanged;
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

                ve.PropertyChanged -= sizeRequestChanged;

                if (canControlWidth)
                {
                    ve.ClearValue(Controls.VisualElement.WidthRequestProperty);
                }
                if (canControlHeight)
                {
                    ve.ClearValue(Controls.VisualElement.HeightRequestProperty);
                }

                ve.PropertyChanged += sizeRequestChanged;
            }
        }

        // .NET MAUI will arrange the view outside of the allocated bounds if it can't fit. If an explicit size request was set, and it was set by us and not the user, indicate that we caused an overflow
        private static bool DidOverflowBounds<T>(Controls.VisualElement visualElement, bool canControl) where T : ViewProjection
        {
            var projection = visualElement.Project<T>(ProjectionFactory);
            return projection.FrameLocation.Value < 0 && visualElement.IsSet(projection.LengthRequestProperty) && canControl;
        }

        public static Point PositionOn(this Controls.VisualElement child, Controls.VisualElement parent = null)
        {
            //return child.PositionOn(parent);

            Point point = Point.Zero;

            if (child?.Parent is Controls.ScrollView scroll)
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
