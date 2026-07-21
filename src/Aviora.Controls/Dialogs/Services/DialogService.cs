using Avalonia.Threading;
using Aviora.Presentation.Dialogs;

namespace Aviora.Controls;

/// <summary>Default queued implementation of <see cref="IDialogHostService"/>.</summary>
public sealed class DialogService : IDialogHostService
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, HostState> _hosts = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task<DialogResult> ShowAsync(DialogRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<DialogResult>(cancellationToken);
        }

        var operation = new DialogOperation(this, request, cancellationToken);
        try
        {
            operation.Content = request.ContentFactory?.Invoke(operation) ?? request.Content;
        }
        catch (Exception exception)
        {
            return Task.FromException<DialogResult>(exception);
        }

        lock (_syncRoot)
        {
            GetOrCreateState(request.HostId).Queue.Enqueue(operation);
        }

        operation.RegisterCancellation();
        ScheduleNext(request.HostId);
        return operation.Completion.Task;
    }

    /// <inheritdoc />
    public bool Close(string hostId = DialogHost.DefaultId, object? result = null)
    {
        DialogOperation? operation;
        lock (_syncRoot)
        {
            if (!_hosts.TryGetValue(hostId, out var state) || state.Active is null || state.Host is null)
            {
                return false;
            }

            operation = state.Active;
        }

        return Close(operation, DialogCloseReason.Programmatic, result);
    }

    /// <inheritdoc />
    public void Attach(IDialogHost host, string hostId)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostId);

        lock (_syncRoot)
        {
            var state = GetOrCreateState(hostId);
            if (state.Host is not null && !ReferenceEquals(state.Host, host))
            {
                throw new InvalidOperationException($"A Dialog with HostId '{hostId}' is already attached.");
            }

            state.Host = host;
        }

        ScheduleNext(hostId);
    }

    /// <inheritdoc />
    public void Detach(IDialogHost host, string hostId)
    {
        DialogOperation? active = null;
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

        active?.Complete(new DialogResult(null, DialogCloseReason.HostDetached));
    }

    /// <inheritdoc />
    public void Complete(IDialogHost host, string hostId, object? result, DialogCloseReason reason)
    {
        DialogOperation? operation;
        lock (_syncRoot)
        {
            if (!_hosts.TryGetValue(hostId, out var state) || !ReferenceEquals(state.Host, host))
            {
                return;
            }

            operation = state.Active;
            state.Active = null;
        }

        operation?.Complete(new DialogResult(result, reason));
        ScheduleNext(hostId);
    }

    private void Cancel(DialogOperation operation)
    {
        IDialogHost? host = null;
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
            operation.Complete(new DialogResult(null, DialogCloseReason.Canceled));
        }
        else if (host is not null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!host.TryClose(DialogCloseReason.Canceled))
                {
                    Complete(host, operation.Request.HostId, null, DialogCloseReason.Canceled);
                }
            });
        }
    }

    private void ScheduleNext(string hostId) => Dispatcher.UIThread.Post(() => PresentNext(hostId));

    private void PresentNext(string hostId)
    {
        IDialogHost? host;
        DialogOperation? operation;
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

        host.Present(operation.Request, operation.Content);
    }

    private bool Close(DialogOperation operation, DialogCloseReason reason, object? result)
    {
        IDialogHost? host = null;
        var removed = false;
        lock (_syncRoot)
        {
            if (!_hosts.TryGetValue(operation.Request.HostId, out var state) || operation.IsCompleted)
            {
                return false;
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
            operation.Complete(new DialogResult(result, reason));
            return true;
        }

        if (host is null)
        {
            return false;
        }

        Dispatcher.UIThread.Post(() => CloseActive(operation, host, reason, result));
        return true;
    }

    private void CloseActive(
        DialogOperation operation,
        IDialogHost host,
        DialogCloseReason reason,
        object? result)
    {
        lock (_syncRoot)
        {
            if (!_hosts.TryGetValue(operation.Request.HostId, out var state) ||
                !ReferenceEquals(state.Active, operation) ||
                !ReferenceEquals(state.Host, host))
            {
                return;
            }
        }

        host.TryClose(reason, result);
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

    private static bool RemoveFromQueue(Queue<DialogOperation> queue, DialogOperation target)
    {
        var removed = false;
        var count = queue.Count;
        for (var index = 0; index < count; index++)
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
        public IDialogHost? Host { get; set; }

        public DialogOperation? Active { get; set; }

        public Queue<DialogOperation> Queue { get; } = new();
    }

    private sealed class DialogOperation : IDialogSession
    {
        private readonly DialogService _owner;
        private CancellationTokenRegistration _registration;
        private int _isCompleted;

        public DialogOperation(
            DialogService owner,
            DialogRequest request,
            CancellationToken cancellationToken)
        {
            _owner = owner;
            Request = request;
            CancellationToken = cancellationToken;
        }

        public DialogRequest Request { get; }

        public CancellationToken CancellationToken { get; }

        public object? Content { get; set; }

        public TaskCompletionSource<DialogResult> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool IsCompleted => Volatile.Read(ref _isCompleted) != 0;

        public bool IsClosed => IsCompleted;

        public bool Close(object? result = null) =>
            _owner.Close(this, DialogCloseReason.Programmatic, result);

        public bool Cancel() =>
            _owner.Close(this, DialogCloseReason.Canceled, null);

        public void RegisterCancellation()
        {
            if (CancellationToken.CanBeCanceled)
            {
                _registration = CancellationToken.Register(() => _owner.Cancel(this));
            }
        }

        public void Complete(DialogResult result)
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
