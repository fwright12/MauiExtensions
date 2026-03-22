using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;

namespace Microsoft.Maui.Controls.Extensions
{
    public static class ScrollView
    {
        public static readonly BindableProperty ContentProperty = BindableProperty.CreateAttached("Content", typeof(View), typeof(Controls.ScrollView), null, propertyChanged: (bindable, oldValue, newValue) =>
        {
            var scrollView = (Controls.ScrollView)bindable;
            var content = (View)newValue;

            scrollView.Content = content;
        });

        public static View GetContent(this Controls.ScrollView bindable) => (View)bindable.GetValue(ContentProperty);
        public static void SetContent(this Controls.ScrollView bindable, object value) => bindable.SetValue(ContentProperty, value);
    }
}

namespace Microsoft.Maui.Controls.Compatibility
{
    public static class ScrollViewExtensions
    {
        public static BindableProperty IsScrollEnabledProperty = BindableProperty.CreateAttached("IsScrollEnabled", typeof(bool), typeof(View), true);

        public static bool GetIsScrollEnabled(this View scrollView) => (bool)scrollView.GetValue(IsScrollEnabledProperty);

        public static void SetIsScrollEnabled(this View scrollView, bool enabled) => scrollView.SetValue(IsScrollEnabledProperty, enabled);

        public class PagingBehavior
        {
            public double Interval;
            public ScrollView ScrollView;

            public PagingBehavior(ScrollView scrollView)
            {
                ScrollView = scrollView;
            }

            public void Snap()
            {
                double interval = ScrollView.GetPagingInterval();
                double scrollX = interval * Math.Round(ScrollView.ScrollX.Bound(0, ScrollView.Content.Width - ScrollView.Width) / interval);
                double scrollY = interval * Math.Round(ScrollView.ScrollY.Bound(0, ScrollView.Content.Height - ScrollView.Height) / interval);

                ScrollView.ScrollToAsync(scrollX, scrollY, true);
            }
        }

        private static BindableProperty PagingBehaviorProperty = BindableProperty.CreateAttached("PagingInterval", typeof(PagingBehavior), typeof(ScrollView), null, defaultValueCreator: value => new PagingBehavior((ScrollView)value) { Interval = double.PositiveInfinity });

        public static bool IsPagingEnabled(this ScrollView scrollView) => scrollView.IsSet(PagingBehaviorProperty);

        public static PagingBehavior GetPagingBehavior(ScrollView scrollView) => (PagingBehavior)scrollView.GetValue(PagingBehaviorProperty);

        public static double GetPagingInterval(this ScrollView scrollView) => GetPagingBehavior(scrollView).Interval;

        public static void SetPagingInterval(this ScrollView scrollView, double value) => GetPagingBehavior(scrollView).Interval = value;

        public static BindableProperty BouncesProperty = BindableProperty.CreateAttached("Bounces", typeof(bool), typeof(ScrollView), true);

        public static bool GetBounces(this ScrollView scrollView) => (bool)scrollView.GetValue(BouncesProperty);

        public static void SetBounces(this ScrollView scrollView, bool value) => scrollView.SetValue(BouncesProperty, value);

        public static Point ScrollPos(this ScrollView scrollView) => new Point(scrollView.ScrollX, scrollView.ScrollY);

        public static Task MakeVisible(this ScrollView scrollView, View view)
        {
            Point point = view.PositionOn(scrollView.Content);
            Rect visibleBounds = new Rect(new Point(scrollView.ScrollX, scrollView.ScrollY), scrollView.Bounds.Size);
            Point scrollTo = scrollView.ScrollPos();

            if (point.X < visibleBounds.Left)
            {
                scrollTo.X = point.X;
            }
            else if (point.X > visibleBounds.Right)
            {
                scrollTo.X = point.X + view.Width - scrollView.Width;
            }

            if (point.Y < visibleBounds.Top)
            {
                scrollTo.Y = point.Y;
            }
            else if (point.Y > visibleBounds.Bottom)
            {
                scrollTo.Y = point.Y + view.Height - scrollView.Height;
            }

            return scrollTo.Equals(scrollView.ScrollPos()) ? Task.CompletedTask : scrollView.ScrollToAsync(scrollTo.X, scrollTo.Y, true);
        }
    }
}
