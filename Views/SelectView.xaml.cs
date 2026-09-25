namespace Microsoft.Maui.Controls.Extensions;

public partial class SelectView : Controls.ContentView
{
    public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(nameof(IsSelected), typeof(bool), typeof(SelectView), false);
    public static readonly BindableProperty IsEditingProperty = BindableProperty.Create(nameof(IsEditing), typeof(bool), typeof(SelectView), false, propertyChanged: (bindable, oldValue, newValue) => ((SelectView)bindable).EditingChanged());

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool IsEditing
    {
        get => (bool)GetValue(IsEditingProperty);
        set => SetValue(IsEditingProperty, value);
    }

    public SelectView()
    {
        InitializeComponent();
    }

    private void EditingChanged()
    {
        if (Content is SwipeView swipeView)
        {
            swipeView.IsEnabled = !IsEditing;

            if (IsEditing)
            {
                swipeView.Close();
            }
        }
    }
}