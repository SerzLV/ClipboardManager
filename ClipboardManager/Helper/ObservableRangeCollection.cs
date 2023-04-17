using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ClipboardManager.Helper;

public sealed class ObservableRangeCollection<T> : ObservableCollection<T>
{
    public void AddRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var itemList = items as IReadOnlyCollection<T> ?? items.ToArray();
        if (itemList.Count == 0)
        {
            return;
        }

        CheckReentrancy();

        foreach (var item in itemList)
        {
            Items.Add(item);
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public void ReplaceRange(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        CheckReentrancy();
        Items.Clear();

        foreach (var item in items)
        {
            Items.Add(item);
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
