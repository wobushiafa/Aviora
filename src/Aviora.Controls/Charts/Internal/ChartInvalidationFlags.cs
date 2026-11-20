namespace Aviora.Controls;

[Flags]
internal enum ChartInvalidationFlags
{
    None = 0,
    VisualOnly = 1,
    SeriesGeometry = 2,
    LayoutAndScale = 4,
    TextCache = 8,
    All = LayoutAndScale | SeriesGeometry | TextCache,
}
