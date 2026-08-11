namespace Aviora.Presentation.Toasts;

/// <summary>Describes the semantic importance of a toast notification.</summary>
public enum ToastSeverity
{
    /// <summary>General information.</summary>
    Information,

    /// <summary>A successful operation.</summary>
    Success,

    /// <summary>A condition that may require attention.</summary>
    Warning,

    /// <summary>A failed operation or error.</summary>
    Error,
}
