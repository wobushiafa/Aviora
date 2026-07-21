namespace Aviora.Presentation.Loadings;

/// <summary>Controls one loading presentation without exposing its host.</summary>
public interface ILoadingSession : IDisposable
{
    /// <summary>Gets whether this presentation has been closed.</summary>
    bool IsClosed { get; }

    /// <summary>Closes only this presentation.</summary>
    bool Close();
}
