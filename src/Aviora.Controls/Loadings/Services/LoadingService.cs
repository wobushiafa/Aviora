using Avalonia.Threading;
using Aviora.Presentation.Loadings;

namespace Aviora.Controls;

/// <summary>Default thread-safe implementation of scoped loading presentations.</summary>
public sealed class LoadingService : ILoadingHostService
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, HostState> _hosts = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public ILoadingSession Show(LoadingRequest? request = null)
    {
        request ??= new LoadingRequest();
        ArgumentException.ThrowIfNullOrWhiteSpace(request.HostId);

        var operation = new LoadingOperation(this, request);
        lock (_syncRoot)
        {
            GetOrCreateState(request.HostId).Operations.Add(operation);
        }

        ScheduleRefresh(request.HostId);
        return operation;
    }

    /// <inheritdoc />
    public void Attach(ILoadingHost host, string hostId)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostId);

        lock (_syncRoot)
        {
            HostState state = GetOrCreateState(hostId);
            if (state.Host is not null && !ReferenceEquals(state.Host, host))
            {
                throw new InvalidOperationException($"A LoadingOverlay with HostId '{hostId}' is already attached.");
            }

            state.Host = host;
        }

        ScheduleRefresh(hostId);
    }

    /// <inheritdoc />
    public void Detach(ILoadingHost host, string hostId)
    {
        lock (_syncRoot)
        {
            if (_hosts.TryGetValue(hostId, out HostState? state) && ReferenceEquals(state.Host, host))
            {
                state.Host = null;
            }
        }
    }

    private bool Close(LoadingOperation operation)
    {
        bool removed;
        lock (_syncRoot)
        {
            if (operation.IsClosed || !_hosts.TryGetValue(operation.Request.HostId, out HostState? state))
            {
                return false;
            }

            removed = state.Operations.Remove(operation);
            if (removed)
            {
                operation.MarkClosed();
            }
        }

        if (removed)
        {
            ScheduleRefresh(operation.Request.HostId);
        }

        return removed;
    }

    private void ScheduleRefresh(string hostId) => Dispatcher.UIThread.Post(() => Refresh(hostId));

    private void Refresh(string hostId)
    {
        ILoadingHost? host;
        LoadingPresentation[] presentations;
        lock (_syncRoot)
        {
            if (!_hosts.TryGetValue(hostId, out HostState? state) || state.Host is null)
            {
                return;
            }

            host = state.Host;
            presentations = state.Operations
                .Where(operation => !operation.IsClosed)
                .Select(operation => new LoadingPresentation(operation.Id, operation.Request))
                .ToArray();
        }

        host.Synchronize(presentations);
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
        public ILoadingHost? Host { get; set; }

        public List<LoadingOperation> Operations { get; } = new();
    }

    private sealed class LoadingOperation : ILoadingSession
    {
        private readonly LoadingService _owner;
        private int _isClosed;

        public LoadingOperation(LoadingService owner, LoadingRequest request)
        {
            _owner = owner;
            Request = request;
        }

        public Guid Id { get; } = Guid.NewGuid();

        public LoadingRequest Request { get; }

        public bool IsClosed => Volatile.Read(ref _isClosed) != 0;

        public bool Close() => _owner.Close(this);

        public void Dispose() => Close();

        public void MarkClosed() => Interlocked.Exchange(ref _isClosed, 1);
    }
}
