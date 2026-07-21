using Aviora.Presentation.Loadings;

namespace Aviora.Controls;

/// <summary>Receives synchronized loading presentations from a host service.</summary>
public interface ILoadingHost
{
    /// <summary>Replaces the host's active presentation snapshot.</summary>
    void Synchronize(IReadOnlyList<LoadingPresentation> presentations);
}

/// <summary>Identifies one active loading presentation.</summary>
public sealed record LoadingPresentation(Guid Id, LoadingRequest Request);
