using Avalonia.Threading;

namespace Aviora.Controls;

internal sealed class ChartUpdateScheduler
{
    private readonly Action<List<IChartDataPoint>> _apply;
    private DispatcherTimer? _timer;
    private List<IChartDataPoint>? _pendingItems;
    private DateTime _lastAppliedTime = DateTime.MinValue;

    public ChartUpdateScheduler(Action<List<IChartDataPoint>> apply)
    {
        _apply = apply;
    }

    public void Schedule(List<IChartDataPoint> items, TimeSpan interval)
    {
        interval = interval > TimeSpan.Zero ? interval : TimeSpan.Zero;
        TimeSpan elapsed = DateTime.UtcNow - _lastAppliedTime;
        if (elapsed >= interval || items.Count == 0)
        {
            Apply(items);
            return;
        }

        _pendingItems = items;
        TimeSpan remaining = interval - elapsed;
        if (_timer == null)
        {
            _timer = new DispatcherTimer(remaining, DispatcherPriority.Normal, OnTimerTick);
        }
        else
        {
            _timer.Interval = remaining;
            _timer.Stop();
        }

        _timer.Start();
    }

    public void Flush()
    {
        _timer?.Stop();
        if (_pendingItems == null)
        {
            return;
        }

        List<IChartDataPoint> items = _pendingItems;
        _pendingItems = null;
        Apply(items);
    }

    public void Stop()
    {
        _timer?.Stop();
        _pendingItems = null;
    }

    private void OnTimerTick(object? sender, EventArgs e) => Flush();

    private void Apply(List<IChartDataPoint> items)
    {
        _pendingItems = null;
        _lastAppliedTime = DateTime.UtcNow;
        _apply(items);
    }
}
