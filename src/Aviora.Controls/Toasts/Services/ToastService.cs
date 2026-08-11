using Avalonia.Threading;
using Aviora.Presentation.Toasts;

namespace Aviora.Controls;

/// <summary>Default thread-safe implementation of global toast notifications.</summary>
public sealed class ToastService : IToastHostService
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, HostState> _hosts = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public IToastSession Show(ToastRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.HostId);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateDuration(request.Duration);

        var operation = new ToastOperation(this, request, cancellationToken);
        operation.Content = request.ContentFactory?.Invoke(operation) ?? request.Content;
        lock (_syncRoot)
        {
            GetOrCreateState(request.HostId).Operations.Add(operation);
        }

        operation.RegisterCancellation();
        SchedulePresent(operation);
        return operation;
    }

    /// <inheritdoc />
    public int Clear(string hostId = ToastHosts.DefaultId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostId);
        ToastOperation[] operations;
        lock (_syncRoot)
        {
            operations = _hosts.TryGetValue(hostId, out HostState? state)
                ? state.Operations.Where(operation => !operation.IsDismissed).ToArray()
                : [];
        }

        foreach (ToastOperation operation in operations)
        {
            Dismiss(operation, ToastDismissReason.Cleared);
        }

        return operations.Length;
    }

    /// <inheritdoc />
    public void Attach(IToastHost host, string hostId)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostId);
        ToastOperation[] operations;
        lock (_syncRoot)
        {
            HostState state = GetOrCreateState(hostId);
            if (state.Host is not null && !ReferenceEquals(state.Host, host))
            {
                throw new InvalidOperationException($"A ToastHost with HostId '{hostId}' is already attached.");
            }

            state.Host = host;
            operations = state.Operations.Where(operation => !operation.IsDismissed).ToArray();
        }

        foreach (ToastOperation operation in operations)
        {
            SchedulePresent(operation);
        }
    }

    /// <inheritdoc />
    public void Detach(IToastHost host, string hostId)
    {
        ToastOperation[] operations;
        lock (_syncRoot)
        {
            if (!_hosts.TryGetValue(hostId, out HostState? state) || !ReferenceEquals(state.Host, host))
            {
                return;
            }

            state.Host = null;
            operations = state.Operations.ToArray();
            state.Operations.Clear();
        }

        foreach (ToastOperation operation in operations)
        {
            operation.Complete(ToastDismissReason.HostDetached);
        }
    }

    /// <inheritdoc />
    public bool RequestDismiss(IToastHost host, string hostId, Guid id, ToastDismissReason reason)
    {
        lock (_syncRoot)
        {
            if (!_hosts.TryGetValue(hostId, out HostState? state) ||
                !ReferenceEquals(state.Host, host) ||
                state.Operations.All(operation => operation.Id != id || operation.IsDismissed))
            {
                return false;
            }
        }

        return host.Dismiss(id, reason);
    }

    /// <inheritdoc />
    public void Complete(IToastHost host, string hostId, Guid id, ToastDismissReason reason)
    {
        ToastOperation? operation;
        lock (_syncRoot)
        {
            if (!_hosts.TryGetValue(hostId, out HostState? state) || !ReferenceEquals(state.Host, host))
            {
                return;
            }

            operation = state.Operations.FirstOrDefault(candidate => candidate.Id == id);
            if (operation is not null)
            {
                state.Operations.Remove(operation);
            }
        }

        operation?.Complete(reason);
    }

    private static void ValidateDuration(TimeSpan? duration)
    {
        if (duration is { } value && value != Timeout.InfiniteTimeSpan && value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Toast duration must be positive or infinite.");
        }
    }

    private void SchedulePresent(ToastOperation operation) =>
        Dispatcher.UIThread.Post(() => Present(operation));

    private void Present(ToastOperation operation)
    {
        IToastHost? host;
        lock (_syncRoot)
        {
            if (operation.IsDismissed ||
                !_hosts.TryGetValue(operation.Request.HostId, out HostState? state) ||
                !state.Operations.Contains(operation))
            {
                return;
            }

            host = state.Host;
        }

        host?.Present(new ToastPresentation(operation.Id, operation.Request, operation.Content, operation));
    }

    private bool Dismiss(ToastOperation operation, ToastDismissReason reason)
    {
        IToastHost? host;
        var completeImmediately = false;
        lock (_syncRoot)
        {
            if (operation.IsDismissed ||
                !_hosts.TryGetValue(operation.Request.HostId, out HostState? state) ||
                !state.Operations.Contains(operation))
            {
                return false;
            }

            host = state.Host;
            if (host is null)
            {
                state.Operations.Remove(operation);
                completeImmediately = true;
            }
        }

        if (completeImmediately)
        {
            operation.Complete(reason);
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!host!.Dismiss(operation.Id, reason))
                {
                    Complete(host, operation.Request.HostId, operation.Id, reason);
                }
            });
        }

        return true;
    }

    private HostState GetOrCreateState(string hostId)
    {
        if (!_hosts.TryGetValue(hostId, out HostState? state))
        {
            state = new HostState();
            _hosts.Add(hostId, state);
        }

        return state;
    }

    private sealed class HostState
    {
        public IToastHost? Host { get; set; }

        public List<ToastOperation> Operations { get; } = [];
    }

    private sealed class ToastOperation : IToastSession
    {
        private readonly ToastService _owner;
        private CancellationTokenRegistration _registration;
        private int _isDismissed;

        public ToastOperation(ToastService owner, ToastRequest request, CancellationToken cancellationToken)
        {
            _owner = owner;
            Request = request;
            CancellationToken = cancellationToken;
        }

        public Guid Id { get; } = Guid.NewGuid();

        public ToastRequest Request { get; }

        public CancellationToken CancellationToken { get; }

        public object? Content { get; set; }

        public bool IsDismissed => Volatile.Read(ref _isDismissed) != 0;

        public Task<ToastDismissReason> Completion => CompletionSource.Task;

        private TaskCompletionSource<ToastDismissReason> CompletionSource { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool Dismiss() => _owner.Dismiss(this, ToastDismissReason.Programmatic);

        public void RegisterCancellation()
        {
            if (CancellationToken.CanBeCanceled)
            {
                _registration = CancellationToken.Register(() => _owner.Dismiss(this, ToastDismissReason.Canceled));
            }
        }

        public void Complete(ToastDismissReason reason)
        {
            if (Interlocked.Exchange(ref _isDismissed, 1) != 0)
            {
                return;
            }

            _registration.Dispose();
            CompletionSource.TrySetResult(reason);
        }
    }
}
