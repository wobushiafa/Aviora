using Avalonia.Threading;
using Aviora.Presentation.Drawers;

namespace Aviora.Controls;

/// <summary>
/// Default queued implementation of <see cref="IDrawerService"/>.
/// </summary>
public sealed class DrawerService : IDrawerService
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, HostState> _hosts = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task<DrawerResult> ShowAsync(DrawerRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<DrawerResult>(cancellationToken);
        }

        var operation = new DrawerOperation(request, cancellationToken);
        lock (_syncRoot)
        {
            var state = GetOrCreateState(request.HostId);
            state.Queue.Enqueue(operation);
        }

        operation.RegisterCancellation(this);
        ScheduleNext(request.HostId);
        return operation.Completion.Task;
    }

    /// <inheritdoc />
    public bool Close(string hostId = DrawerHost.DefaultId, object? result = null)
    {
        Drawer? host;
        lock (_syncRoot)
        {
            if (!_hosts.TryGetValue(hostId, out var state) || state.Active is null || state.Host is null)
            {
                return false;
            }

            host = state.Host;
        }

        Dispatcher.UIThread.Post(() => host.TryClose(DrawerCloseReason.Programmatic, result));
        return true;
    }

    internal void Attach(Drawer host, string hostId)
    {
        ArgumentNullException.ThrowIfNull(host);

        lock (_syncRoot)
        {
            var state = GetOrCreateState(hostId);
            if (state.Host is not null && !ReferenceEquals(state.Host, host))
            {
                throw new InvalidOperationException($"A Drawer with HostId '{hostId}' is already attached.");
            }

            state.Host = host;
        }

        ScheduleNext(hostId);
    }

    internal void Detach(Drawer host, string hostId)
    {
        DrawerOperation? active = null;
        lock (_syncRoot)
        {
            if (!_hosts.TryGetValue(hostId, out var state) || !ReferenceEquals(state.Host, host))
            {
                return;
            }

            state.Host = null;
            active = state.Active;
            state.Active = null;
        }

        active?.Complete(new DrawerResult(null, DrawerCloseReason.HostDetached));
    }

    internal void Complete(Drawer host, string hostId, object? result, DrawerCloseReason reason)
    {
        DrawerOperation? operation;
        lock (_syncRoot)
        {
            if (!_hosts.TryGetValue(hostId, out var state) || !ReferenceEquals(state.Host, host))
            {
                return;
            }

            operation = state.Active;
            state.Active = null;
        }

        operation?.Complete(new DrawerResult(result, reason));
        ScheduleNext(hostId);
    }

    private void Cancel(DrawerOperation operation)
    {
        Drawer? host = null;
        var removed = false;
        lock (_syncRoot)
        {
            if (!_hosts.TryGetValue(operation.Request.HostId, out var state))
            {
                return;
            }

            if (ReferenceEquals(state.Active, operation))
            {
                host = state.Host;
            }
            else
            {
                removed = RemoveFromQueue(state.Queue, operation);
            }
        }

        if (removed)
        {
            operation.Complete(new DrawerResult(null, DrawerCloseReason.Canceled));
        }
        else if (host is not null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!host.TryClose(DrawerCloseReason.Canceled))
                {
                    Complete(host, operation.Request.HostId, null, DrawerCloseReason.Canceled);
                }
            });
        }
    }

    private void ScheduleNext(string hostId) => Dispatcher.UIThread.Post(() => PresentNext(hostId));

    private void PresentNext(string hostId)
    {
        Drawer? host;
        DrawerOperation? operation;
        lock (_syncRoot)
        {
            if (!_hosts.TryGetValue(hostId, out var state) || state.Host is null || state.Active is not null)
            {
                return;
            }

            do
            {
                operation = state.Queue.Count > 0 ? state.Queue.Dequeue() : null;
            }
            while (operation is not null && operation.IsCompleted);

            if (operation is null)
            {
                return;
            }

            state.Active = operation;
            host = state.Host;
        }

        host.Present(operation.Request);
    }

    private HostState GetOrCreateState(string hostId)
    {
        if (!_hosts.TryGetValue(hostId, out var state))
        {
            state = new HostState();
            _hosts.Add(hostId, state);
        }

        return state;
    }

    private static bool RemoveFromQueue(Queue<DrawerOperation> queue, DrawerOperation target)
    {
        var removed = false;
        var count = queue.Count;
        for (var i = 0; i < count; i++)
        {
            var item = queue.Dequeue();
            if (ReferenceEquals(item, target))
            {
                removed = true;
            }
            else
            {
                queue.Enqueue(item);
            }
        }

        return removed;
    }

    private sealed class HostState
    {
        public Drawer? Host { get; set; }

        public DrawerOperation? Active { get; set; }

        public Queue<DrawerOperation> Queue { get; } = new();
    }

    private sealed class DrawerOperation
    {
        private CancellationTokenRegistration _registration;
        private int _isCompleted;

        public DrawerOperation(DrawerRequest request, CancellationToken cancellationToken)
        {
            Request = request;
            CancellationToken = cancellationToken;
        }

        public DrawerRequest Request { get; }

        public CancellationToken CancellationToken { get; }

        public TaskCompletionSource<DrawerResult> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool IsCompleted => Volatile.Read(ref _isCompleted) != 0;

        public void RegisterCancellation(DrawerService owner)
        {
            if (CancellationToken.CanBeCanceled)
            {
                _registration = CancellationToken.Register(() => owner.Cancel(this));
            }
        }

        public void Complete(DrawerResult result)
        {
            if (Interlocked.Exchange(ref _isCompleted, 1) != 0)
            {
                return;
            }

            _registration.Dispose();
            Completion.TrySetResult(result);
        }
    }
}
