using MauiExtensions;
using Microsoft.Maui.Layouts;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.Maui.Controls.Extensions
{
    public class AspectRatioConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string) || sourceType.IsAssignableFrom(typeof(double));

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is double d)
            {
                return d;
            }
            else if (value is string str)
            {
                var colon = str.IndexOf(":");

                if (colon == -1)
                {
                    if (double.TryParse(str, out var result))
                    {
                        return result;
                    }
                }
                else
                {
                    if (double.TryParse(str.Substring(0, colon), out var width) && double.TryParse(str.Substring(colon + 1), out var height))
                    {
                        return width / height;
                    }
                }
            }

            throw new InvalidOperationException($"Cannot convert {value} into {typeof(double)}");
        }
    }

    //[ContentProperty(nameof(Image))]
    //[XamlCompilation(XamlCompilationOptions.Compile)]
    public class ImageView : Layout, IImageElement
    {
        public static readonly BindableProperty SourceProperty = Image.SourceProperty;

        public static readonly BindableProperty AspectProperty = Image.AspectProperty;

        public static readonly BindableProperty IsOpaqueProperty = Image.IsOpaqueProperty;

        public static readonly BindableProperty IsLoadingProperty = Image.IsLoadingProperty;

        public static readonly BindableProperty IsAnimationPlayingProperty = Image.IsAnimationPlayingProperty;



        private static readonly BindableProperty.BindingPropertyChangedDelegate ComputedAltViewChangedDelegate = Templates.CreateViewChangedDelegate<ImageView>(imageView => imageView.AltView, imageView => imageView.AltViewTemplate, (imageView, view) => imageView.ComputedAltView = view);

        public static readonly BindableProperty AltViewProperty = BindableProperty.Create(nameof(AltView), typeof(object), typeof(ImageView), propertyChanged: ComputedAltViewChangedDelegate);

        public static readonly BindableProperty AltViewTemplateProperty = BindableProperty.Create(nameof(AltViewTemplate), typeof(DataTemplate), typeof(ImageView), propertyChanged: ComputedAltViewChangedDelegate);

        public static readonly BindableProperty ImageProperty = BindableProperty.Create(nameof(Image), typeof(Image), typeof(ImageView));


        public Aspect Aspect
        {
            get => Image.Aspect;
            set => Image.Aspect = value;
        }

        public bool IsLoading
        {
            get => Image.IsLoading;
        }

        public bool IsOpaque
        {
            get => Image.IsOpaque;
            set => Image.IsOpaque = value;
        }

        public bool IsAnimationPlaying
        {
            get => Image.IsAnimationPlaying;
            set => Image.IsAnimationPlaying = value;
        }

        [TypeConverter(typeof(ImageSourceConverter))]
        public ImageSource Source
        {
            get => Image.Source;
            set => Image.Source = value;
        }

        public object AltView
        {
            get => GetValue(AltViewProperty);
            set => SetValue(AltViewProperty, value);
        }

        public DataTemplate AltViewTemplate
        {
            get => (DataTemplate)GetValue(AltViewTemplateProperty);
            set => SetValue(AltViewTemplateProperty, value);
        }

        public Image Image
        {
            get => (Image)GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }

        private IView? ComputedAltView
        {
            get => _ComputedAltView;
            set
            {
                Children.Remove(_ComputedAltView);

                _ComputedAltView = value;
                if (_ComputedAltView != null)
                {
                    Children.Insert(0, _ComputedAltView);
                }
            }
        }

        private IView? _ComputedAltView;

        public ImageView()
        {
            Children.Add(Image = new Image());
            
            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == SourceProperty.PropertyName)
                {
                    Image.SetValue(SourceProperty, GetValue(SourceProperty));
                }
            };
        }

        public void RaiseImageSourcePropertyChanged()
        {
            ((IImageElement)Image).RaiseImageSourcePropertyChanged();
        }

        public void OnImageSourceSourceChanged(object sender, EventArgs e)
        {
            ((IImageElement)Image).OnImageSourceSourceChanged(sender, e);
        }

        protected override ILayoutManager CreateLayoutManager() => new ImageViewLayoutManager(this);

        private class ImageViewLayoutManager : LayoutManager
        {
            public ImageView ImageView { get; }

            public ImageViewLayoutManager(ImageView imageView) : base(imageView)
            {
                ImageView = imageView;
            }

            public override Size Measure(double widthConstraint, double heightConstraint)
            {
                return ImageView.Image?.Measure(widthConstraint, heightConstraint) ?? Size.Zero;
            }

            public override Size ArrangeChildren(Rect bounds)
            {
                ImageView.Image.Arrange(bounds);
                ImageView._ComputedAltView?.Arrange(bounds);
                return bounds.Size;
            }
        }
    }
}