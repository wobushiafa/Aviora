namespace Aviora.Presentation.Loadings;

/// <summary>Provides scoped loading overlay presentations.</summary>
public interface ILoadingService
{
    /// <summary>Shows a loading presentation until the returned session is closed or disposed.</summary>
    ILoadingSession Show(LoadingRequest? request = null);
}
