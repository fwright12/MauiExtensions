using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Layouts;

namespace Microsoft.Maui.Controls
{
    public static class ViewExtensions
    {
        public static readonly BindableProperty GestureRecognizersProperty = BindableProperty.CreateAttached(nameof(GetGestureRecognizers).Substring(3), typeof(DataTemplate), typeof(View), null, propertyChanged: GestureRecognizersChanged);

        public static DataTemplate GetGestureRecognizers(this View view) => (DataTemplate)view.GetValue(GestureRecognizersProperty);
        public static void SetGestureRecognizers(this View view, DataTemplate value) => view.SetValue(GestureRecognizersProperty, value);

        private static void GestureRecognizersChanged(BindableObject bindable, object _oldValue, object _newValue)
        {
            var view = (View)bindable;
            var oldValue = (DataTemplate)_oldValue;
            var newValue = (DataTemplate)_newValue;

            view.GestureRecognizers.Add((IGestureRecognizer)newValue.CreateContent());
        }

        public static void SetLayoutOptions(this View view, LayoutOptions layoutOptions)
        {
            view.HorizontalOptions = layoutOptions;
            view.VerticalOptions = layoutOptions;
        }

        public static void MoveToBounded(this View view, Point position) => view.MoveToBounded(position, (view.Parent as View)?.Bounds ?? RectangleExtensions.Unbounded);

        public static void MoveToBounded(this View view, Point position, Rect bounds) => view.MoveTo(position.Bound(new Rect(Point.Zero, bounds.Size).Shrink(view.Bounds.Size)));

        public static void MoveTo(this View view, double x, double y) => view.MoveTo(new Point(x, y));

        public static void MoveTo(this View view, Point position)
        {
            if (view.Parent is AbsoluteLayout)
            {
                MoveOnAbsoluteLayout(view, position, AbsoluteLayoutFlags.None);
            }
            else if (view.Parent is RelativeLayout)
            {
                MoveOnRelativeLayout(view, position);
            }
            else
            {
                MoveOnLayout(view, position);
            }
        }

        private static void MoveOnAbsoluteLayout(View view, Point point, AbsoluteLayoutFlags pointFlags)
        {
            AbsoluteLayoutFlags currentFlags = AbsoluteLayout.GetLayoutFlags(view);
            Rect bounds = new Rect(point, AbsoluteLayout.GetLayoutBounds(view).Size);
            Rect feasibleBounds = (view.Parent as View).Bounds.Shrink(view.Bounds.Size);

            // x is interpreted absolute but given proportionally
            if (pointFlags.HasFlag(AbsoluteLayoutFlags.XProportional) && !currentFlags.HasFlag(AbsoluteLayoutFlags.XProportional))
            {
                bounds.X *= feasibleBounds.Width;
            }
            // x is interpreted proportionally but given absolute
            else if (!pointFlags.HasFlag(AbsoluteLayoutFlags.XProportional) && currentFlags.HasFlag(AbsoluteLayoutFlags.XProportional))
            {
                bounds.X /= feasibleBounds.Width;
            }

            // y is interpreted absolute but given proportionally
            if (pointFlags.HasFlag(AbsoluteLayoutFlags.YProportional) && !currentFlags.HasFlag(AbsoluteLayoutFlags.YProportional))
            {
                bounds.Y *= feasibleBounds.Height;
            }
            // y is interpreted proportionally but given absolute
            else if (!pointFlags.HasFlag(AbsoluteLayoutFlags.YProportional) && currentFlags.HasFlag(AbsoluteLayoutFlags.YProportional))
            {
                bounds.Y /= feasibleBounds.Height;
            }

            AbsoluteLayout.SetLayoutBounds(view, bounds);
        }

        private static void MoveOnRelativeLayout(View view, Point point)
        {
            RelativeLayout.SetXConstraint(view, Constraint.Constant(point.X));
            RelativeLayout.SetYConstraint(view, Constraint.Constant(point.Y));
        }

        private static void MoveOnLayout(View view, Point point)
        {
            view.TranslationX = point.X;
            view.TranslationY = point.Y;
        }

        /*public static void MoveView(this View view, Rectangle bounds, Point position)
        {
            view.MoveToBounded(position, bounds);
            return;

            //Action<View, Point> move = v.Parent is AbsoluteLayout ? MoveOnAbsoluteLayout : MoveOnLayout;
            Point point = new Point(
                Math.Max(0, Math.Min(bounds.Width - view.Width, position.X)),
                Math.Max(0, Math.Min(bounds.Height - view.Height, position.Y))
                );
            //move(v, p);

            view.MoveTo(point);
        }*/

        //public static void MoveTo(this View view, Point position) => MoveTo(view, position.X, position.Y);

        /*public static void MoveTo(this View view, double x, double y)
        {
            if (view.Parent is AbsoluteLayout)
            {
                AbsoluteLayout.SetLayoutBounds(view, new Rectangle(x, y, -1, -1));
            }
            else
            {
                throw new Exception("View is not a child of an absolute layout");
            }
        }*/

        public static Point RenderPosition(this View view) => new Point(view.X + view.TranslationX, view.Y + view.TranslationY);

        public static int Index(this View view) => (view.Parent as Layout<View>).Children.IndexOf(view);

        /*public static bool Remove(this View view)
        {
            if (view.Parent is Layout<View>)
            {
                (view.Parent as Layout<View>).Children.Remove(view);
                return true;
            }

            System.Diagnostics.Debug.WriteLine("View did not have a parent that could be cast to Layout<View>");
            return false;
        }*/
    }
}
