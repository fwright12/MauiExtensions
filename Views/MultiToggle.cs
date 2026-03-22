using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.Maui.Controls.Extensions
{
    public delegate void SelectEventHandler(string value);

    public class MultiToggle : WrapLayout
    {
        public static readonly BindableProperty SelectedProperty = BindableProperty.Create("Selected", typeof(List<ToggledEventArgs>), typeof(MultiToggle), defaultBindingMode: BindingMode.TwoWay, propertyChanged: OnSelectedChanged);

        //public event EventHandler<ToggledEventArgs> Selected;
        public event SelectEventHandler Selected;
        public event SelectEventHandler Deselected;

        //public IBoolean ValueView { get; protected set; }

        /*public List<ToggledEventArgs> Selected
        {
            get { return (List<ToggledEventArgs>)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }*/
        public bool CanSelectMultiple = false;
        public Color SelectedColor;// = Color.Green;
        public Color DefaultColor = Colors.Transparent;

        private static readonly int OPTION_MARGIN = 5;

        public MultiToggle()
        {
            Wrap = FlexWrap.Wrap;
            JustifyContent = FlexJustify.Center;
        }

        public MultiToggle(params string[] items) : this()
        {
            foreach (string s in items)
            {
                Button button = new Button
                {
                    Text = s,
                    Margin = new Thickness(OPTION_MARGIN),
                    CornerRadius = 5,
                    BackgroundColor = DefaultColor,
                    BorderColor = Colors.Black,
                    BorderWidth = 2,
                };

                button.Clicked += (sender, e) =>
                {
                    if (button.BackgroundColor == DefaultColor)
                    {
                        //Deselect the previous selection if only one can be selected
                        if (!CanSelectMultiple)
                        {
                            DeselectAll();
                        }

                        button.BackgroundColor = SelectedColor;
                        Selected?.Invoke(button.Text);
                        //SelectionChanged?.Invoke(button.Text, new ToggledEventArgs(true));
                    }
                    else if (CanSelectMultiple)
                    {
                        button.BackgroundColor = DefaultColor;
                        Deselected?.Invoke(button.Text);
                        //SelectionChanged?.Invoke(button.Text, new ToggledEventArgs(false));
                    }
                };

                Children.Add(button);
                //Children.Add(new Label { Text = s, BackgroundColor= Color.Red, Margin = new Thickness(5) });
            }
        }

        public void Enable(bool isEnabled) => IsEnabled = isEnabled;

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == IsEnabledProperty.PropertyName)
            {
                foreach (View v in Children)
                {
                    v.IsEnabled = IsEnabled;
                    if (v is Button)
                    {
                        (v as Button).BorderColor = IsEnabled ? Colors.Black : Colors.LightGray;
                    }
                }
            }
        }

        public IEnumerable<string> GetSelected()
        {
            foreach(Button b in Children.OfType<Button>())
            {
                if (b.BackgroundColor == SelectedColor)
                {
                    yield return b.Text;
                }
            }
        }

        public void Select(params string[] selected)
        {
            foreach (Button b in Children.OfType<Button>())
            {
                if (selected.Length > 0 && (b.Text == selected[0] || (CanSelectMultiple && selected.Contains(b.Text))))
                {
                    b.BackgroundColor = SelectedColor;
                }
                else if (b.BackgroundColor == SelectedColor)
                {
                    b.BackgroundColor = DefaultColor;
                }
            }
        }

        private void DeselectAll()
        {
            foreach (Button b in Children)
            {
                if (b.BackgroundColor == SelectedColor)
                {
                    b.BackgroundColor = DefaultColor;

                    if (!CanSelectMultiple)
                    {
                        break;
                    }
                }
            }
        }

        private static void OnSelectedChanged(object bindable, object oldValue, object newValue)
        {
            MultiToggle toggle = (MultiToggle)bindable;
            List<ToggledEventArgs> selected = (List<ToggledEventArgs>)newValue;

            /*foreach(Button b in toggle.Children)
            {
                if (selected.Contains(b.Text))
                {
                    b.BackgroundColor = toggle.SelectedColor;
                }
                else
                {
                    b.BackgroundColor = toggle.DefaultColor;
                }
            }*/
        }
    }

    public class WrapLayout : Controls.FlexLayout
    {
        private SizeRequest lastMeasurement;
        private Size lastValidMeasurement;// = new Size(double.PositiveInfinity, double.PositiveInfinity);

        protected override SizeRequest OnMeasure(double widthConstraint, double heightConstraint)
        {
            if (Direction == FlexDirection.Row)
            {
                double width = 0;
                double height = 0;
                double max = 0;
                double x = 0;
                foreach (View v in Children)
                {
                    SizeRequest temp = v.Measure(widthConstraint, heightConstraint);

                    if (x + temp.Request.Width + v.Margin.Left + v.Margin.Right > widthConstraint)
                    {
                        x = 0;
                        height += max;
                        max = temp.Request.Height;
                    }
                    else
                    {
                        max = Math.Max(max, temp.Request.Height);
                        x += temp.Request.Width + v.Margin.Left + v.Margin.Right;
                    }

                    width = Math.Max(width, x);
                }

                height += max;

                Size size = new Size(width, height);
                //Print.Log("on measure", widthConstraint, heightConstraint, size);
                if (double.IsInfinity(widthConstraint))
                {
                    return new SizeRequest(lastValidMeasurement, (lastMeasurement = base.OnMeasure(widthConstraint, heightConstraint)).Minimum);
                }
                return lastMeasurement = new SizeRequest(size, size);
            }

            return new SizeRequest(lastValidMeasurement, (lastMeasurement = base.OnMeasure(widthConstraint, heightConstraint)).Minimum);
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            //Print.Log("size allocated", width, height);
            base.OnSizeAllocated(width, height);
            //return;
            lastValidMeasurement = lastMeasurement.Request;
            InvalidateMeasure();
        }
    }
}
