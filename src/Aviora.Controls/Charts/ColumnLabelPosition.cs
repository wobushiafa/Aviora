namespace Aviora.Controls;

/// <summary>
/// Specifies where numeric data labels are positioned relative to each column in a <see cref="ColumnChart"/>.
/// </summary>
public enum ColumnLabelPosition
{
    /// <summary>Places the label just outside the column tip (above positive bars, below negative bars).</summary>
    Outside,

    /// <summary>Places the label inside the column near its tip.</summary>
    InsideTop,

    /// <summary>Centers the label vertically inside the column.</summary>
    Center,
}
