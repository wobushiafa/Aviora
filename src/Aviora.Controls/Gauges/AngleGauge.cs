using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Aviora.Controls;

#pragma warning disable CS1591

/// <summary>
/// Displays an angle-like value on a semicircular gauge.
/// </summary>
public class AngleGauge : GaugeBase
{
    private const double SweepAngle = 180;

    static AngleGauge()
    {
        MaximumProperty.OverrideDefaultValue<AngleGauge>(180.0);
        TickCountProperty.OverrideDefaultValue<AngleGauge>(6);
        NeedleBrushProperty.OverrideDefaultValue<AngleGauge>(AvioraControlPalette.Danger);
    }

    internal new static double NormalizeValue(double value, double minimum, double maximum) => GaugeBase.NormalizeValue(value, minimum, maximum);

    protected override Size MeasureOverride(Size availableSize)
    {
        const double defaultWidth = 240;
        const double defaultHeight = 150;
        return new Size(
            double.IsInfinity(availableSize.Width) ? defaultWidth : Math.Min(defaultWidth, Math.Max(0, availableSize.Width)),
            double.IsInfinity(availableSize.Height) ? defaultHeight : Math.Min(defaultHeight, Math.Max(0, availableSize.Height)));
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double width = Bounds.Width;
        double height = Bounds.Height;
        if (width < 48 || height < 48)
        {
            return;
        }

        double labelSpace = ShowTickLabels ? Math.Max(14, TickLabelFontSize + 6) : 6;
        double centerX = width / 2;
        double centerY = height - labelSpace - 8;
        double radius = Math.Min((width / 2) - labelSpace, centerY - 6);
        if (radius <= 12)
        {
            return;
        }

        var thinPen = new Pen(TickBrush, 1.2);
        DrawArc(context, centerX, centerY, radius, thinPen);
        DrawArc(context, centerX, centerY, radius - 12, new Pen(TickBrush, 4, lineCap: PenLineCap.Round));

        int tickCount = Math.Clamp(TickCount, 0, 100);
        if (ShowTicks && tickCount > 0)
        {
            for (int index = 0; index <= tickCount; index++)
            {
                double angle = SweepAngle * index / tickCount;
                context.DrawLine(thinPen,
                    PointOnCircle(centerX, centerY, radius, angle),
                    PointOnCircle(centerX, centerY, radius + 6, angle));
            }
        }

        if (ShowTickLabels)
        {
            DrawEndpointLabel(context, Minimum, centerX - radius, centerY + 3, false);
            DrawEndpointLabel(context, Maximum, centerX + radius, centerY + 3, true);
        }

        double normalizedValue = NormalizeValue(Value, Minimum, Maximum);
        Point needleEnd = PointOnCircle(centerX, centerY, radius - 12, normalizedValue * SweepAngle);
        context.DrawLine(new Pen(NeedleBrush, 6, lineCap: PenLineCap.Round), new Point(centerX, centerY), needleEnd);
        context.DrawEllipse(PivotBrush, null, new Point(centerX, centerY), 7.5, 7.5);
    }

    private static void DrawArc(DrawingContext context, double centerX, double centerY, double radius, Pen pen)
    {
        if (radius <= 0)
        {
            return;
        }

        var figure = new PathFigure { StartPoint = PointOnCircle(centerX, centerY, radius, 0) };
        figure.Segments!.Add(new ArcSegment
        {
            Point = PointOnCircle(centerX, centerY, radius, SweepAngle),
            Size = new Size(radius, radius),
            SweepDirection = SweepDirection.Clockwise,
        });
        var geometry = new PathGeometry();
        geometry.Figures!.Add(figure);
        context.DrawGeometry(null, pen, geometry);
    }

    private void DrawEndpointLabel(DrawingContext context, double value, double x, double y, bool alignRight)
    {
        var text = new FormattedText(
            $"{FormatTickLabel(value)}°",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Medium),
            Math.Max(1, TickLabelFontSize),
            TickLabelBrush);
        context.DrawText(text, new Point(alignRight ? x - text.Width : x, y));
    }

}
