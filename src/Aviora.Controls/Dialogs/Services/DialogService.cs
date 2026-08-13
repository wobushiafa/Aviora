using Avalonia.Threading;
using Aviora.Presentation.Dialogs;

namespace Aviora.Controls;

/// <summary>Default queued implementation of <see cref="IDialogHostService"/>.</summary>
public sealed class DialogService : IDialogHostService
{
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, HostState> _hosts = new(StringComparer.Ordinal);
    private readonly DialogServiceOptions? _options;

    /// <summary>Initializes a dialog service that uses each host's configured defaults.</summary>
    public DialogService()
    {
    }

    /// <summary>Initializes a dialog service with application-wide presentation defaults.</summary>
    public DialogService(DialogServiceOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>Initializes a dialog service with application-wide presentation defaults.</summary>
    [Obsolete("Use the DialogServiceOptions constructor overload instead.")]
    public DialogService(DialogOptions options)
        : this((DialogServiceOptions)options)
    {
    }

    /// <inheritdoc />
    public Task<DialogResult> ShowAsync(DialogRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<DialogResult>(cancellationToken);
        }

        request = ApplyOptions(request);
        var operation = new DialogOperation(this, request, cancellationToken);
        try
        {
            operation.Content = request.ContentFactory?.Invoke(operation) ?? request.Content;
        }
        catch (Exception exception)
        {
            return Task.FromException<DialogResult>(exception);
        }

        IDialogHost? nestedHost = null;
        lock (_syncRoot)
        {
            HostState state = GetOrCreateState(request.HostId);
            if (request.PresentationMode != DialogPresentationMode.Queue &&
                state.Active is not null &&
                state.Host is not null)
            {
                state.Suspended.Add(state.Active);
                state.Active = operation;
                nestedHost = state.Host;
            }
            else
            {
                state.Queue.Enqueue(operation);
            }
        }

        operation.RegisterCancellation();
        if (nestedHost is not null)
        {
            Dispatcher.UIThread.Post(() => nestedHost.Present(request, operation.Content));
        }
        else
        {
            ScheduleNext(request.HostId);
        }
        return operation.Completion.Task;
    }

    private DialogRequest ApplyOptions(DialogRequest request)
    {
        if (_options is null)
        {
            return request;
        }

        return new DialogRequest(request.Content)
        {
            ContentFactory = request.ContentFactory,
            HostId = request.HostId,
            PresentationMode = request.PresentationMode,
            Title = request.Title,
            Description = request.Description,
            Width = request.Width,
            Height = request.Height,
            IsLightDismissEnabled = request.IsLightDismissEnabled ?? _options.IsLightDismissEnabled,
            IsEscapeKeyEnabled = request.IsEscapeKeyEnabled ?? _options.IsEscapeKeyEnabled,
            IsOverlayVisible = request.IsOverlayVisible ?? _options.IsOverlayVisible,
            IsAnimationEnabled = request.IsAnimationEnabled ?? _options.IsAnimationEnabled,
            AnimationDuration = request.AnimationDuration ?? _options.AnimationDuration,
            Tag = request.Tag,
        };
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
        List<DialogOperation>? active = null;
        lock (_syncRoot)
        {
            if (!_hosts.TryGetValue(hostId, out var state) || !ReferenceEquals(state.Host, host))
            {
                return;
            }

            state.Host = null;
            active = [];
            if (state.Active is not null)
            {
                active.Add(state.Active);
            }
            active.AddRange(state.Suspended);
            state.Active = null;
            state.Suspended.Clear();
        }

        active?.ForEach(operation =>
            operation.Complete(new DialogResult(null, DialogCloseReason.HostDetached)));
    }

    /// <inheritdoc />
    public void Complete(IDialogHost host, string hostId, object? result, DialogCloseReason reason)
    {
        DialogOperation? operation;
        DialogOperation? restored = null;
        lock (_syncRoot)
        {
            if (!_hosts.TryGetValue(hostId, out var state) || !ReferenceEquals(state.Host, host))
            {
                return;
            }

            operation = state.Active;
            if (state.Suspended.Count > 0)
            {
                int index = state.Suspended.Count - 1;
                restored = state.Suspended[index];
                state.Suspended.RemoveAt(index);
            }
            state.Active = restored;
        }

        operation?.Complete(new DialogResult(result, reason));
        if (restored is not null)
        {
            Dispatcher.UIThread.Post(() => host.Present(restored.Request, restored.Content));
        }
        else
        {
            ScheduleNext(hostId);
        }
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
                removed = state.Suspended.Remove(operation) || RemoveFromQueue(state.Queue, operation);
            }
        }

        if (removed)
        {
            operation.Complete(new DialogResult(null, DialogCloseReason.Canceled));
        }
        else if (host is not null)
        {
            Dispatcher.UIThread.Post(() => host.TryClose(DialogCloseReason.Canceled));
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
                removed = state.Suspended.Remove(operation) || RemoveFromQueue(state.Queue, operation);
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

        public List<DialogOperation> Suspended { get; } = [];

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
