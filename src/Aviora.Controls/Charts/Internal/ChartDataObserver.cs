using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;

namespace Aviora.Controls;

internal sealed class ChartDataObserver : IDisposable
{
    private readonly Action<object?, bool> _collectionChanged;
    private readonly Action _itemChanged;
    private readonly Action<AvaloniaObject, AvaloniaProperty>? _seriesPropertyChanged;
    private readonly List<INotifyCollectionChanged> _collections = [];
    private readonly Dictionary<INotifyPropertyChanged, int> _itemRefCounts = new(ReferenceEqualityComparer.Instance);
    private readonly List<AvaloniaObject> _avaloniaObjects = [];

    public ChartDataObserver(Action<object?> collectionChanged, Action itemChanged)
        : this((sender, _) => collectionChanged(sender), itemChanged, null)
    {
    }

    public ChartDataObserver(
        Action<object?, bool> collectionChanged,
        Action itemChanged,
        Action<AvaloniaObject, AvaloniaProperty>? seriesPropertyChanged)
    {
        _collectionChanged = collectionChanged;
        _itemChanged = itemChanged;
        _seriesPropertyChanged = seriesPropertyChanged;
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

    public void ObserveAvaloniaObjects(IEnumerable<AvaloniaObject?> objects)
    {
        ClearAvaloniaObjects();
        foreach (AvaloniaObject? item in objects)
        {
            if (item == null || _avaloniaObjects.Contains(item))
            {
                continue;
            }

            item.PropertyChanged += OnAvaloniaObjectPropertyChanged;
            _avaloniaObjects.Add(item);
        }
    }

    public void ObserveItems(IEnumerable<IChartDataPoint> items)
    {
        ClearItems();
        foreach (IChartDataPoint entry in items)
        {
            if (UnwrapSource(entry) is INotifyPropertyChanged npc)
            {
                AddObservedItem(npc);
            }
        }
    }

    public void Dispose()
    {
        ClearCollections();
        ClearItems();
        ClearAvaloniaObjects();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        bool incremental = false;
        if (e.Action is NotifyCollectionChangedAction.Add or
            NotifyCollectionChangedAction.Remove or
            NotifyCollectionChangedAction.Replace)
        {
            incremental = true;
            UpdateIncrementalItems(e.OldItems, add: false);
            UpdateIncrementalItems(e.NewItems, add: true);
        }

        _collectionChanged(sender, incremental);
    }

    private void UpdateIncrementalItems(IList? list, bool add)
    {
        if (list == null)
        {
            return;
        }

        foreach (object? raw in list)
        {
            if (UnwrapSource(raw) is not INotifyPropertyChanged npc)
            {
                continue;
            }

            if (add)
            {
                AddObservedItem(npc);
            }
            else
            {
                RemoveObservedItem(npc);
            }
        }
    }

    private static object? UnwrapSource(object? raw)
    {
        object? unwrapped = raw is ChartDataPipeline.SeriesDataPoint series ? series.Source : raw;
        return unwrapped is ChartDataPipeline.AdaptedDataPoint adapted ? adapted.Source : unwrapped;
    }

    private void AddObservedItem(INotifyPropertyChanged item)
    {
        if (_itemRefCounts.TryGetValue(item, out int count))
        {
            _itemRefCounts[item] = count + 1;
            return;
        }

        _itemRefCounts.Add(item, 1);
        item.PropertyChanged += OnItemPropertyChanged;
    }

    private void RemoveObservedItem(INotifyPropertyChanged item)
    {
        if (!_itemRefCounts.TryGetValue(item, out int count))
        {
            return;
        }

        if (count > 1)
        {
            _itemRefCounts[item] = count - 1;
            return;
        }

        _itemRefCounts.Remove(item);
        item.PropertyChanged -= OnItemPropertyChanged;
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e) => _itemChanged();

    private void OnAvaloniaObjectPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is AvaloniaObject target)
        {
            _seriesPropertyChanged?.Invoke(target, e.Property);
        }
    }

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
        foreach (INotifyPropertyChanged item in _itemRefCounts.Keys)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }

        _itemRefCounts.Clear();
    }

    private void ClearAvaloniaObjects()
    {
        foreach (AvaloniaObject item in _avaloniaObjects)
        {
            item.PropertyChanged -= OnAvaloniaObjectPropertyChanged;
        }

        _avaloniaObjects.Clear();
    }
}
