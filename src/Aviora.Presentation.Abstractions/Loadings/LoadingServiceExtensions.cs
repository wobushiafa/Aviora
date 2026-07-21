namespace Aviora.Presentation.Loadings;

/// <summary>Provides exception-safe loading scopes for asynchronous operations.</summary>
public static class LoadingServiceExtensions
{
    /// <summary>Runs an asynchronous operation while a loading presentation is active.</summary>
    public static async Task RunAsync(
        this ILoadingService service,
        Func<Task> operation,
        LoadingRequest? request = null)
    {
        if (service is null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        using ILoadingSession session = service.Show(request);
        await operation().ConfigureAwait(false);
    }

    /// <summary>Runs an asynchronous operation while a loading presentation is active.</summary>
    public static async Task<TResult> RunAsync<TResult>(
        this ILoadingService service,
        Func<Task<TResult>> operation,
        LoadingRequest? request = null)
    {
        if (service is null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        using ILoadingSession session = service.Show(request);
        return await operation().ConfigureAwait(false);
    }

    /// <summary>Runs a cancellable asynchronous operation while a loading presentation is active.</summary>
    public static async Task RunAsync(
        this ILoadingService service,
        Func<CancellationToken, Task> operation,
        LoadingRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }
        cancellationToken.ThrowIfCancellationRequested();

        using ILoadingSession session = service.Show(request);
        await operation(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Runs a cancellable asynchronous operation with a result while loading is active.</summary>
    public static async Task<TResult> RunAsync<TResult>(
        this ILoadingService service,
        Func<CancellationToken, Task<TResult>> operation,
        LoadingRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
        {
            throw new ArgumentNullException(nameof(service));
        }

        if (operation is null)
        {
            throw new ArgumentNullException(nameof(operation));
        }
        cancellationToken.ThrowIfCancellationRequested();

        using ILoadingSession session = service.Show(request);
        return await operation(cancellationToken).ConfigureAwait(false);
    }
}
