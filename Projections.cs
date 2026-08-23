namespace Microsoft.Maui;

public abstract class Projection<TObject>
{
    public TObject Object { get; set; }
}

public static class ProjectionHelpers
{
    public static TProjection SetObject<TProjection, TObject>(this TProjection projection, TObject obj) where TProjection : Projection<TObject>
    {
        projection.Object = obj;
        return projection;
    }

    public static T Create<T>(this IProjectionFactory projectionFactory) => (T)projectionFactory.Create(typeof(T));

    public static T Project<T>(this VisualElement view, IProjectionFactory? projectionFactory = null) where T : Projection<VisualElement> => (T)Project(view, typeof(T), projectionFactory);
    public static Projection<T> Project<T>(T obj, IProjectionFactory? projectionFactory = null) => Project(obj, typeof(T), projectionFactory);
    private static IProjectionFactory? _projectionFactoryInstance;
    public static Projection<T> Project<T>(this T obj, Type projectionType, IProjectionFactory? projectionFactory = null) => ((Projection<T>)(projectionFactory ?? (_projectionFactoryInstance ??= new SimpleProjectionFactory())).Create(projectionType)).SetObject(obj);
}

public abstract class ViewProjection : Projection<VisualElement>
{
    public abstract Primitives.LayoutAlignment Alignment { get; }
    public PointProjection FrameLocation => CreatePointProjection().SetObject(Object.Frame.Location);
    public abstract double Length { get; }
    public abstract BindableProperty LengthRequestProperty { get; }

    public abstract Type PointProjectionType { get; }
    public abstract Type SizeProjectionType { get; }

    public IProjectionFactory? ProjectionFactory { get; set; }

    private PointProjection CreatePointProjection() => CreateProjection<PointProjection>(PointProjectionType);
    private SizeProjection CreateSizeProjection() => CreateProjection<SizeProjection>(SizeProjectionType);
    private T CreateProjection<T>(Type projectionType) => (T)(ProjectionFactory?.Create(projectionType) ?? Activator.CreateInstance(projectionType) ?? throw new Exception($"Could not create Projection of type {projectionType}"));

    public class Horizontal : ViewProjection
    {
        public override Primitives.LayoutAlignment Alignment => ((IView)Object).HorizontalLayoutAlignment;
        public override double Length => Object.Width;
        public override BindableProperty LengthRequestProperty => VisualElement.WidthRequestProperty;

        public override Type PointProjectionType => typeof(PointProjection.Horizontal);
        public override Type SizeProjectionType => typeof(SizeProjection.Horizontal);
    }

    public class Vertical : ViewProjection
    {
        public override Primitives.LayoutAlignment Alignment => ((IView)Object).VerticalLayoutAlignment;
        public override double Length => Object.Height;
        public override BindableProperty LengthRequestProperty => VisualElement.HeightRequestProperty;

        public override Type PointProjectionType => typeof(PointProjection.Vertical);
        public override Type SizeProjectionType => typeof(SizeProjection.Vertical);
    }
}

public abstract class SizeProjection : Projection<Size>
{
    public abstract double Length { get; }
    public abstract Size Create(double length, double orthogonalLength);

    public class Horizontal : SizeProjection
    {
        public override double Length => Object.Width;

        public override Size Create(double length, double orthogonalLength) => new Size(length, orthogonalLength);
    }

    public class Vertical : SizeProjection
    {
        public override double Length => Object.Height;

        public override Size Create(double length, double orthogonalLength) => new Size(orthogonalLength, length);
    }
}

public abstract class PointProjection : Projection<Point>
{
    public abstract double Value { get; }

    public abstract Point Create(double value, double otherValue);

    public class Horizontal : PointProjection
    {
        public override double Value => Object.X;

        public override Point Create(double value, double otherValue) => new Point(value, otherValue);
    }

    public class Vertical : PointProjection
    {
        public override double Value => Object.Y;

        public override Point Create(double value, double otherValue) => new Point(otherValue, value);
    }
}

public interface IProjectionFactory
{
    public object Create(Type type);
}

public class SimpleProjectionFactory : IProjectionFactory
{
    public object Create(Type type) => Activator.CreateInstance(type)!;
}

public class ReusableProjectionFactory : IProjectionFactory
{
    private Dictionary<Type, object> Projections = new Dictionary<Type, object>();

    public virtual object Create(Type type)
    {
        if (!Projections.TryGetValue(type, out var projection))
        {
            Projections[type] = projection = Activator.CreateInstance(type)!;
        }

        return projection;
    }
}

public class MauiReusableProjectionFactory : ReusableProjectionFactory
{
    public override object Create(Type type)
    {
        var projection = base.Create(type);

        if (projection is ViewProjection viewProjection)
        {
            viewProjection.ProjectionFactory = this;
        }

        return projection;
    }
}
