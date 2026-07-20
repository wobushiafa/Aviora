using System.Collections.Specialized;
using System.ComponentModel;

namespace Aviora.Controls;

internal sealed class ChartDataObserver : IDisposable
{
    private readonly Action<object?> _collectionChanged;
    private readonly Action _itemChanged;
    private readonly List<INotifyCollectionChanged> _collections = [];
    private readonly HashSet<INotifyPropertyChanged> _items = new(ReferenceEqualityComparer.Instance);

    public ChartDataObserver(Action<object?> collectionChanged, Action itemChanged)
    {
        _collectionChanged = collectionChanged;
        _itemChanged = itemChanged;
    }

    public void ObserveCollections(params object?[] sources)
    {
        ClearCollections();
        foreach (object? source in sources)
        {
            if (source is not INotifyCollectionChanged notify || _collections.Contains(notify))
            {
                continue;
            }

            notify.CollectionChanged += OnCollectionChanged;
            _collections.Add(notify);
        }
    }

    public void ObserveItems(IEnumerable<IChartDataPoint> items)
    {
        ClearItems();
        foreach (INotifyPropertyChanged item in items.OfType<INotifyPropertyChanged>())
        {
            if (_items.Add(item))
            {
                item.PropertyChanged += OnItemPropertyChanged;
            }
        }
    }

    public void Dispose()
    {
        ClearCollections();
        ClearItems();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        _collectionChanged(sender);

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e) => _itemChanged();

    private void ClearCollections()
    {
        foreach (INotifyCollectionChanged notify in _collections)
        {
            notify.CollectionChanged -= OnCollectionChanged;
        }

        _collections.Clear();
    }

    private void ClearItems()
    {
        foreach (INotifyPropertyChanged item in _items)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }

        _items.Clear();
    }
}
