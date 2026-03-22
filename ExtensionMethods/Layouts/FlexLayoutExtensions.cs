namespace Microsoft.Maui.Controls.Extensions;

public static class FlexLayout
{
    public static readonly BindableProperty RowSpacingProperty = BindableProperty.Create(nameof(GetRowSpacing).Substring(3), typeof(double), typeof(Controls.FlexLayout), 0d, propertyChanged: (bindable, oldValue, newValue) => SpacingChanged((Controls.FlexLayout)bindable, (double)newValue, null));

    public static readonly BindableProperty ColumnSpacingProperty = BindableProperty.Create(nameof(GetColumnSpacing).Substring(3), typeof(double), typeof(Controls.FlexLayout), 0d, propertyChanged: (bindable, oldValue, newValue) => SpacingChanged((Controls.FlexLayout)bindable, null, (double)newValue));

    public static double GetRowSpacing(this Controls.FlexLayout flexLayout) => (double)flexLayout.GetValue(RowSpacingProperty);
    public static void SetRowSpacing(this Controls.FlexLayout flexLayout, double value) => flexLayout.SetValue(RowSpacingProperty, value);

    public static double GetColumnSpacing(this Controls.FlexLayout flexLayout) => (double)flexLayout.GetValue(ColumnSpacingProperty);
    public static void SetColumnSpacing(this Controls.FlexLayout flexLayout, double value) => flexLayout.SetValue(ColumnSpacingProperty, value);

    private static void SpacingChanged(Controls.FlexLayout flexLayout, double? rowSpacing, double? columnSpacing)
    {
        rowSpacing ??= GetRowSpacing(flexLayout);
        columnSpacing ??= GetColumnSpacing(flexLayout);

        // Would like to distribute this evenly on all sides, but FlexLayout does not properly handle right and bottom margin values
        flexLayout.Padding = new Thickness(-columnSpacing.Value, -rowSpacing.Value, 0, 0);

        flexLayout.ChildAdded -= FlexLayoutChildAdded;
        flexLayout.ChildAdded += FlexLayoutChildAdded;

        foreach (var child in flexLayout.Children.OfType<View>())
        {
            FlexLayoutChildAdded(flexLayout, child);
        }
    }

    private static void FlexLayoutChildAdded(object? sender, ElementEventArgs e)
    {
        if (sender is Controls.FlexLayout flexLayout && e.Element is View view)
        {
            FlexLayoutChildAdded(flexLayout, view);
        }
    }

    private static void FlexLayoutChildAdded(Controls.FlexLayout flexLayout, View child)
    {
        // Need to divide both values by 2 because FlexLayout has issues with child Margins
        child.Margin = new Thickness(GetColumnSpacing(flexLayout) / 2, GetRowSpacing(flexLayout) / 2, 0, 0);
    }
}
