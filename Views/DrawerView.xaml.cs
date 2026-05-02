using Microsoft.Maui.Controls.Extensions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace Microsoft.Maui.Controls
{
    public class RelativePositionViewModel : BindableViewModel
    {
        public VisualElement Ancestor { get; }
        public VisualElement Child
        {
            get => _Child;
            set => UpdateValue(ref _Child, value);
        }

        public Point Position
        {
            get => _Position;
            set => UpdateValue(ref _Position, value);
        }

        private VisualElement _Child;
        private Point _Position;

        public RelativePositionViewModel(VisualElement ancestor)
        {
            Ancestor = ancestor;

            PropertyChange += ChildChange;
        }

        private void ChildChange(object sender, PropertyChangeEventArgs e)
        {
            if (e.OldValue is VisualElement oldChild)
            {
                RemoveHandlers(oldChild);
            }
            if (e.NewValue is VisualElement newChild)
            {
                AddHandlers(newChild);
            }
        }

        private void AddHandlers(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Element.Parent))
            {
                var element = (Element)sender;
                AddHandlers(element.Parent);
            }
        }

        private void RemoveHandlers(object sender, PropertyChangingEventArgs e)
        {
            if (e.PropertyName == nameof(Element.Parent))
            {
                var element = (Element)sender;
                RemoveHandlers(element.Parent);
            }
        }

        private void AddHandlers(Element element)
        {
            if (element == null)
            {
                return;
            }

            element.PropertyChanged += PositionChanged;

            if (element is VisualElement visualElement)
            {
                visualElement.SizeChanged += SizeChanged;
            }
            if (element is Compatibility.Layout layout)
            {
                layout.LayoutChanged += LayoutChanged;
            }

            if (element != Ancestor)
            {
                element.PropertyChanging += RemoveHandlers;
                element.PropertyChanged += AddHandlers;

                AddHandlers(element.Parent);
            }
            else
            {
                BoundsChanged();
            }
        }

        private void RemoveHandlers(Element element)
        {
            if (element == null)
            {
                return;
            }

            element.PropertyChanged -= PositionChanged;

            if (element is VisualElement visualElement)
            {
                visualElement.SizeChanged -= SizeChanged;
            }
            if (element is Compatibility.Layout layout)
            {
                layout.LayoutChanged -= LayoutChanged;
            }

            if (element != Ancestor)
            {
                element.PropertyChanging -= RemoveHandlers;
                element.PropertyChanged -= AddHandlers;

                RemoveHandlers(element.Parent);
            }
        }

        private void LayoutChanged(object sender, EventArgs e) => BoundsChanged();

        private void SizeChanged(object sender, EventArgs e) => BoundsChanged();

        private void PositionChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == VisualElement.XProperty.PropertyName || e.PropertyName == VisualElement.YProperty.PropertyName)
            {
                BoundsChanged();
            }
        }

        private void BoundsChanged() => Position = Child.PositionOn(Ancestor);
    }

    public interface ISnapPoint
    {
        double Value { get; }
    }

    public class SnapPoint : Element, ISnapPoint
    {
        public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(double), typeof(ISnapPoint));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        //public static implicit operator SnapPoint(double value) => new SnapPoint { Value = value };
    }

    public class ElementSnapPoint : Element, ISnapPoint
    {
        public static readonly BindableProperty ElementProperty = BindableProperty.Create(nameof(Element), typeof(VisualElement), typeof(ElementSnapPoint), propertyChanged: (bindable, oldValue, newValue) =>
        {
            var snapPoint = (ElementSnapPoint)bindable;

            if (oldValue is VisualElement oldElement)
            {
                oldElement.SizeChanged -= snapPoint.PositionChanged;
            }
            if (newValue is VisualElement newElement)
            {
                newElement.SizeChanged += snapPoint.PositionChanged;
            }
            snapPoint.PositionChanged();

            if (snapPoint.Position != null)
            {
                snapPoint.Position.Child = (VisualElement)newValue;
            }
        });

        public VisualElement Element
        {
            get => (VisualElement)GetValue(ElementProperty);
            set => SetValue(ElementProperty, value);
        }

        public double Value { get; private set; }
        public SnapPointsAlignment Alignment
        {
            get => _Alignment;
            set
            {
                if (value != _Alignment)
                {
                    _Alignment = value;
                    PositionChanged();
                }
            }
        }

        private RelativePositionViewModel Position;
        private SnapPointsAlignment _Alignment;

        public ElementSnapPoint()
        {
            PropertyChanging += ParentWillChange;
            PropertyChanged += ParentDidChange;
        }

        public Point GetPoint()
        {
            if (Position == null || Element == null || !(Parent is VisualElement))
            {
                return Point.Zero;
            }

            var value = Position.Position;

            if (Alignment == SnapPointsAlignment.Center)
            {
                value += (Size)Element.Bounds.Center;
            }
            else if (Alignment == SnapPointsAlignment.End)
            {
                value += Element.Bounds.Size;
            }

            return value;
        }

        private void ParentWillChange(object? sender, PropertyChangingEventArgs e)
        {
            if (e.PropertyName == nameof(Parent) && Parent != null)
            {
                Parent.PropertyChanged -= DrawerContentChanged;
            }
        }

        private void ParentDidChange(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Parent) && Parent != null)
            {
                Parent.PropertyChanged += DrawerContentChanged;
                DrawerContentChanged();
            }
        }

        private void DrawerContentChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DrawerView.DrawerContentView))
            {
                DrawerContentChanged();
            }
        }

        private void DrawerContentChanged()
        {
            if (Position != null)
            {
                Position.Child = null;
                Position.PropertyChanged -= PositionChanged;
            }

            var content = (Parent as DrawerView)?.DrawerContentView;

            if (content != null)
            {
                Position = new RelativePositionViewModel(content)
                {
                    Child = Element
                };
                Position.PropertyChanged += PositionChanged;
                PositionChanged();
            }
        }

        private void PositionChanged(object? sender, EventArgs e) => PositionChanged();
        private void PositionChanged(object? sender, PropertyChangedEventArgs e) => PositionChanged();
        private void PositionChanged()
        {
            Value = GetPoint().Y;
            if (Element != null && (!Element.IsSet(VisualElement.WidthProperty) || !Element.IsSet(VisualElement.HeightProperty)))
            {
                Value += 1;
            }

            OnPropertyChanged(nameof(Value));
        }
    }

    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DrawerView : ContentView
    {
        public static class VisualStates
        {
            public static readonly string Open = "Open";
            public static readonly string Closed = "Closed";
        }

        private static readonly BindablePropertyKey SnapPointsPropertyKey = BindableProperty.CreateReadOnly(nameof(SnapPoints), typeof(bool), typeof(DrawerView), null, defaultValueCreator: bindable =>
        {
            var drawer = (DrawerView)bindable;

            var snapPoints = new ObservableCollection<ISnapPoint>();
            snapPoints.CollectionChanged += (sender, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems.OfType<Element>())
                    {
                        //item.Parent = drawer.DrawerContentView;
                        item.Parent = drawer;
                    }
                }

                //drawer.ChangeSnapPoint((();
            };

            return snapPoints;
        });

        public static readonly BindableProperty SnapPointsProperty = SnapPointsPropertyKey.BindableProperty;

        public static readonly BindableProperty IsOpenProperty = BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(DrawerView), true, propertyChanged: (bindable, oldValue, newValue) =>
        {
            var drawer = (DrawerView)bindable;
            //drawer.ChangeState();
        });

        /*public static readonly BindableProperty DrawerContentProperty = BindableProperty.Create(nameof(DrawerContent), typeof(object), typeof(DrawerView), propertyChanged: (bindable, oldValue, newValue) =>
        {
            DrawerView drawer = (DrawerView)bindable;
            drawer.LazyView = new Lazy<View>(() => newValue as View ?? (newValue as ElementTemplate)?.CreateContent() as View);
            drawer.OnPropertyChanged(nameof(DrawerContentView));
        });*/

        public IList<ISnapPoint> SnapPoints => (IList<ISnapPoint>)GetValue(SnapPointsProperty);

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        private static void SetDrawerContent(DrawerView drawerView, View view)
        {
            if (view == null)
            {
                drawerView.AbortAnimation(SNAP_ANIMATION_NAME);
            }

            drawerView.DrawerContentView = view;
            drawerView.OnPropertyChanged(nameof(DrawerContentView));
        }

        //public static readonly BindableProperty DrawerProperty = Items.ItemSourceProperty;
        //public static readonly BindableProperty DrawerTemplateProperty = Items.ItemTemplateProperty;

        public static readonly BindableProperty DrawerProperty = BindableProperty.Create(nameof(Drawer), typeof(object), typeof(DrawerView));
        public static readonly BindableProperty DrawerTemplateProperty = BindableProperty.Create(nameof(DrawerTemplate), typeof(ElementTemplate), typeof(DrawerView));

        public object Drawer
        {
            get => GetValue(DrawerProperty);
            set => SetValue(DrawerProperty, value);
        }

        public ElementTemplate DrawerTemplate
        {
            get => (ElementTemplate)GetValue(DrawerTemplateProperty);
            set => SetValue(DrawerTemplateProperty, value);
        }

        /*public object DrawerContent
        {
            get => GetValue(DrawerContentProperty);
            set => SetValue(DrawerContentProperty, value);
        }*/

        public View DrawerContentView { get; private set; }

        public ICommand Toggle { get; private set; }
        public ICommand NextSnapPointCommand { get; }

        private SwipeGestureRecognizer Swipe { get; }

        public DrawerView()
        {
            Toggle = new Command(() => IsOpen = !IsOpen);
            NextSnapPointCommand = new Command(() => ToggleSnapPoint(1, true));

            Swipe = new SwipeGestureRecognizer
            {
                Direction = SwipeDirection.Down | SwipeDirection.Up,
            };
            Swipe.Swiped += Swiped;

            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == DrawerProperty.PropertyName || e.PropertyName == DrawerTemplateProperty.PropertyName)
                {
                    var content = TemplatedContent.CreateContent(DrawerTemplate, Drawer, this) as View;
                    if (content != null)
                    {
                        SetDrawerContent(this, content);
                    }
                }
                if (e.PropertyName == IsOpenProperty.PropertyName || e.PropertyName == nameof(DrawerContentView))
                {
                    ChangeState();
                    foreach (var snapPoint in SnapPoints.OfType<Element>())
                    {
                        snapPoint.Parent = this;
                    }

                    if (SnapPoints.Count > 0)
                    {
                        SnapTo(SnapPoints.First(), false);
                    }
                }
            };

            InitializeComponent();
            ChangeState();
            //IsOpenPropertyChanged(this, true, false);
        }

        private void Swiped(object sender, SwipedEventArgs e)
        {
            int direction = e.Direction == SwipeDirection.Down ? -1 : 1;
            ToggleSnapPoint(direction, true);
        }

        private void ToggleSnapPoint(int step, bool animated) => SnapTo(SnapPoints[(NearestSnapPointIndex(DrawerContentView.Height) + step) % SnapPoints.Count], animated);

        private const string SNAP_ANIMATION_NAME = "Snap";

        public void SnapTo(ISnapPoint snapPoint, bool animated)
        {
            if (DrawerContentView == null)
            {
                return;
            }

            if (snapPoint != SnapPoints.LastOrDefault())
            {
                ChangeState(snapPoint);
            }

            var animation = new Animation(value => DrawerContentView.HeightRequest = value, DrawerContentView.Height, snapPoint.Value, Easing.SpringOut);
            animation.Commit(this, SNAP_ANIMATION_NAME, length: animated ? 250u : 0, finished: (value, cancelled) => Snapped(snapPoint));
            //HeightRequest = SnapPoints[index % SnapPoints.Count].Value;
        }

        private void Snapped(ISnapPoint snapPoint)
        {
            DrawerContentView.SetBinding(HeightRequestProperty, new Binding(nameof(ISnapPoint.Value), source: snapPoint));
            ChangeState(snapPoint);
        }

        private void ChangeState(ISnapPoint snapPoint)
        {
            var state = snapPoint == SnapPoints.LastOrDefault() ? VisualStates.Open : VisualStates.Closed;

            if (VisualStateManager.GoToState(DrawerContentView, state))
            {
                return;
                if (state == VisualStates.Open)
                {
                    DrawerContentView.GestureRecognizers.Remove(Swipe);
                }
                else if (!DrawerContentView.GestureRecognizers.Contains(Swipe))
                {
                    DrawerContentView.GestureRecognizers.Add(Swipe);
                }
            }
        }

        public ISnapPoint NearestSnapPoint(double value) => SnapPoints[NearestSnapPointIndex(value) % SnapPoints.Count];
        public int NearestSnapPointIndex(double value)
        {
            var snapPoints = new List<double>(SnapPoints.Select(snapPoint => snapPoint.Value));

            //snapPoints.Add(Height);
            snapPoints.Sort();

            var index = snapPoints.BinarySearch(value);

            if (index < 0)
            {
                index = ~index;

                if (index != 0 && (index == snapPoints.Count || value - snapPoints[index - 1] < snapPoints[index] - value))
                {
                    index--;
                }
            }

            return index;
        }

        private void ChangeState()
        {
            if (DrawerContentView != null)
            {
                VisualStateManager.GoToState(DrawerContentView, IsOpen ? VisualStates.Open : VisualStates.Closed);
            }
        }

        //private static void IsOpenPropertyChanged(BindableObject bindable, object oldValue, object newValue) => VisualStateManager.GoToState((VisualElement)bindable, (bool)newValue ? VisualStates.Open : VisualStates.Closed);
    }
}
