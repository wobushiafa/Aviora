namespace Aviora.Core.Charts;

/// <summary>
/// Default framework-independent implementation of <see cref="IChartPoint"/>.
/// </summary>
public class ChartPoint : IChartPoint
{
    /// <inheritdoc />
    public virtual object? Key { get; init; }

    /// <inheritdoc />
    public virtual string? Label { get; init; }

    /// <inheritdoc />
    public virtual double Value { get; init; }

    /// <inheritdoc />
    public virtual string? ToolTip { get; init; }
}
