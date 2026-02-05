using Android.Content;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Controls.Extensions;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.Platform;
using System.ComponentModel;

namespace MauiExtensions.Platforms.Android
{
    public class MauiRecyclerView<TItemsView, TAdapter, TItemsViewSource> : Microsoft.Maui.Controls.Handlers.Items.MauiRecyclerView<TItemsView, TAdapter, TItemsViewSource>
        where TItemsView : ItemsView
        where TAdapter : ItemsViewAdapter<TItemsView, TItemsViewSource>
        where TItemsViewSource : IItemsViewSource
    {
        public MauiRecyclerView(Context context, Func<IItemsLayout> getItemsLayout, Func<TAdapter> getAdapter) : base(context, getItemsLayout, getAdapter) { }

        protected override LayoutManager SelectLayoutManager(IItemsLayout layoutSpecification)
        {
            switch (layoutSpecification)
            {
                case Microsoft.Maui.Controls.LinearItemsLayout listItemsLayout:
                    var orientation = listItemsLayout.Orientation == ItemsLayoutOrientation.Horizontal
                        ? AndroidX.RecyclerView.Widget.LinearLayoutManager.Horizontal
                        : AndroidX.RecyclerView.Widget.LinearLayoutManager.Vertical;

                    return new LinearLayoutManager(Context, orientation, GridLength.Auto, false);
            }

            return base.SelectLayoutManager(layoutSpecification);
        }

        protected override void LayoutPropertyChanged(object sender, PropertyChangedEventArgs propertyChanged)
        {
            base.LayoutPropertyChanged(sender, propertyChanged);

            if (propertyChanged.PropertyName == "ItemSize")
            {
                GetLayoutManager()?.RequestLayout();
            }
        }
    }

    public class LinearLayoutManager : AndroidX.RecyclerView.Widget.LinearLayoutManager
    {
        private Context? Context { get; }
        private GridLength Size { get; }

        public LinearLayoutManager(Context? context, int orientation, GridLength size, bool reverseLayout) : base(context, orientation, reverseLayout)
        {
            Context = context;
            Size = size;
        }

        public override void MeasureChild(global::Android.Views.View child, int widthUsed, int heightUsed)
        {
            if (Size.IsAuto || !IsRegularChild(child))
            {
                base.MeasureChildWithMargins(child, widthUsed, heightUsed);
                return;
            }

            MeasureChild(child, widthUsed, heightUsed, 0, 0, 0, 0);
        }

        public override void MeasureChildWithMargins(global::Android.Views.View child, int widthUsed, int heightUsed)
        {
            if (Size.IsAuto || !IsRegularChild(child))
            {
                base.MeasureChildWithMargins(child, widthUsed, heightUsed);
                return;
            }

            var lp = (RecyclerView.LayoutParams)child.LayoutParameters;
            MeasureChild(child, widthUsed, heightUsed, lp.LeftMargin, lp.RightMargin, lp.TopMargin, lp.BottomMargin);
        }

        private void MeasureChild(global::Android.Views.View child, int widthUsed, int heightUsed, int rightMargin, int leftMargin, int topMargin, int bottomMargin)
        {
            var lp = (RecyclerView.LayoutParams)child.LayoutParameters;

            var insets = new global::Android.Graphics.Rect();
            CalculateItemDecorationsForChild(child, insets);
            widthUsed += insets.Left + insets.Right;
            heightUsed += insets.Top + insets.Bottom;

            var widthSpec = GetChildMeasureSpecInDirection(Width, WidthMode, PaddingLeft + PaddingRight + leftMargin + rightMargin + widthUsed, lp.Width, CanScrollHorizontally(), Orientation == 0);
            var heightSpec = GetChildMeasureSpecInDirection(Height, HeightMode, PaddingTop + PaddingBottom + topMargin + bottomMargin + heightUsed, lp.Height, CanScrollVertically(), Orientation == 1);

            if (Size.IsStar)
            {
                if (widthSpec == null)
                {
                    widthSpec = MakeRelativeMeasureSpec(heightSpec.Value, Size.Value);
                }
                else
                {
                    heightSpec = MakeRelativeMeasureSpec(widthSpec.Value, 1 / Size.Value);
                }
            }

            if (ShouldMeasureChild(child, widthSpec.Value, heightSpec.Value, lp))
            {
                child.Measure(widthSpec.Value, heightSpec.Value);
            }
        }

        private static int MakeRelativeMeasureSpec(int otherSpec, double scale) => MeasureSpecExtensions.MakeMeasureSpec(MeasureSpecMode.Exactly, (int)(MeasureSpecExtensions.GetSize(otherSpec) * scale));

        private int? GetChildMeasureSpecInDirection(int parentSize, int parentMode, int padding, int childDimension, bool canScroll, bool mainAxis)
        {
            if (mainAxis && !Size.IsAuto)
            {
                if (Size.IsAbsolute)
                {
                    return MeasureSpecExtensions.MakeMeasureSpec(MeasureSpecMode.Exactly, GetPixels(Size.Value));
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return GetChildMeasureSpec(parentSize, parentMode, padding, childDimension, canScroll);
            }
        }

        private bool ShouldMeasureChild(global::Android.Views.View child, int widthSpec, int heightSpec, RecyclerView.LayoutParams lp) => true;

        private bool IsRegularChild(global::Android.Views.View child) => GetItemViewType(child) != 43;

        private int GetPixels(double value) => (int)Context.ToPixels(value);
    }
}
