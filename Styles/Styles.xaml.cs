using System.Collections.Specialized;

namespace Microsoft.Maui.Controls.Extensions;

public partial class Styles : ResourceDictionary
{
    public Styles()
    {
        InitializeComponent();
        return;
        if (Application.Current != null)
        {
            Application.Current.PropertyChanged += ApplicationResourcesChanged;
        }
    }

    private void ApplicationResourcesChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Application.Resources))
        {
            return;
        }

        if (Application.Current?.Resources.MergedDictionaries is INotifyCollectionChanged observable)
        {
            observable.CollectionChanged += MergedDictionariesChanged;
        }
    }

    private void MergedDictionariesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems?.Contains(this) == true)
        {
            ((INotifyCollectionChanged)Application.Current?.Resources.MergedDictionaries!).CollectionChanged -= MergedDictionariesChanged;
            Application.Current.Resources.Merge(this);
        }
    }
}