using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Microsoft.Maui.Controls;

public class CollectionConverterContext : BindableObject
{
    public static readonly BindableProperty Collection1Property = BindableProperty.Create(nameof(CollectionConverterContext<,>.Collection1), typeof(IEnumerable), typeof(BindableObject));

    public static readonly BindableProperty Collection2Property = BindableProperty.Create(nameof(CollectionConverterContext<,>.Collection2), typeof(IEnumerable), typeof(BindableObject));
}

public abstract class CollectionConverterContext<T1, T2> : CollectionConverterContext
{
    public IEnumerable<T1> Collection1
    {
        get => (IEnumerable<T1>)GetValue(Collection1Property);
        set => SetValue(Collection1Property, value);
    }

    public IEnumerable<T2> Collection2
    {
        get => (IEnumerable<T2>)GetValue(Collection2Property);
        set => SetValue(Collection2Property, value);
    }

    public CollectionConverterContext()
    {
        PropertyChanging += EntireCollectionChanging;
        PropertyChanged += EntireCollectionChanged;
    }

    protected abstract T2 Convert(T1 item);
    protected abstract T1 Convert(T2 item);

    private void EntireCollectionChanging(object sender, Microsoft.Maui.Controls.PropertyChangingEventArgs e)
    {
        IEnumerable collection;
        if (e.PropertyName == Collection1Property.PropertyName)
        {
            collection = Collection1;
        }
        else if (e.PropertyName == Collection2Property.PropertyName)
        {
            collection = Collection2;
        }
        else
        {
            return;
        }

        if (collection is INotifyCollectionChanged observable)
        {
            observable.CollectionChanged -= PartialCollectionChanged;
        }
    }

    private void EntireCollectionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == Collection1Property.PropertyName)
        {
            EntireCollectionChanged<T1, T2>(Collection1, Collection2, Convert, Convert);
        }
        else if (e.PropertyName == Collection2Property.PropertyName)
        {
            EntireCollectionChanged<T2, T1>(Collection2, Collection1, Convert, Convert);
        }
    }

    private void EntireCollectionChanged<TCollection, TOther>(IEnumerable collection, IEnumerable other, Func<TOther, TCollection> convert, Func<TCollection, TOther> convertBack)
    {
        if (other == null)
        {
            return;
        }

        var otherObservable = other as INotifyCollectionChanged;
        if (otherObservable != null)
        {
            otherObservable.CollectionChanged -= PartialCollectionChanged;
        }

        if (collection == null)
        {
            return;
        }

        PartialCollectionChanged(other, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        PartialCollectionChanged(other, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, collection.OfType<TCollection>().Select(convertBack).ToList(), 0));

        if (false && collection.GetEnumerator().MoveNext())
        {
            var add = collection.OfType<TCollection>().ToHashSet();
            var remove = new List<TOther>();
            foreach (var item in other.OfType<TOther>())
            {
                var converted = convert(item);
                if (!add.Remove(converted))
                {
                    remove.Add(item);
                }
            }

            PartialCollectionChanged(other, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, remove, add.ToList()));
        }

        if (otherObservable != null)
        {
            otherObservable.CollectionChanged += PartialCollectionChanged;
        }
        if (collection is INotifyCollectionChanged observable)
        {
            observable.CollectionChanged += PartialCollectionChanged;
        }
    }

    private bool IsSyncing = false;

    private void PartialCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Collection1.Count() > 0 || Collection2.Count() > 0)
        {
            ;
        }

        if (IsSyncing)
        {
            return;
        }
        IsSyncing = true;

        if (sender == Collection1)
        {
            PartialCollectionChanged<T1, T2>(Collection2, e, Convert);
        }
        else if (sender == Collection2)
        {
            PartialCollectionChanged<T2, T1>(Collection1, e, Convert);
        }

        IsSyncing = false;
    }

    private void PartialCollectionChanged<TCollection, TOther>(IEnumerable other, NotifyCollectionChangedEventArgs e, Func<TCollection, TOther> convert)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            if (other is ICollection<TOther> typedCollection)
            {
                typedCollection.Clear();
            }
            else if (other is IList list)
            {
                list.Clear();
            }
        }
        else
        {
            if (e.OldItems != null)
            {
                for (int i = 0; i < e.OldItems.Count; i++)
                {
                    if (e.OldStartingIndex != -1)
                    {
                        if (other is IList<TOther> typedList)
                        {
                            typedList.RemoveAt(e.OldStartingIndex);
                            continue;
                        }
                        else if (other is IList list)
                        {
                            list.RemoveAt(e.OldStartingIndex);
                            continue;
                        }
                    }

                    var item = convert((TCollection)e.OldItems[i]!);

                    if (other is ICollection<TOther> typedCollection)
                    {
                        typedCollection.Remove(item);
                    }
                    else if (other is IList list)
                    {
                        list.Remove(item);
                    }
                }
            }

            if (e.NewItems != null)
            {
                int index = e.NewStartingIndex;
                foreach (var item in e.NewItems.OfType<TCollection>().Select(convert))
                {
                    if (e.NewStartingIndex != -1)
                    {
                        if (other is IList<TOther> typedList)
                        {
                            typedList.Insert(index++, item);
                            continue;
                        }
                        else if (other is IList list)
                        {
                            list.Insert(index++, item);
                            continue;
                        }
                    }

                    if (other is ICollection<TOther> typedCollection)
                    {
                        typedCollection.Add(item);
                    }
                    else if (other is IList list)
                    {
                        list.Add(item);
                    }
                }
            }
        }
    }
}