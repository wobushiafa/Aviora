using Avalonia;
using Avalonia.Media;

namespace Aviora.Controls;

internal static class RingDrawing
{
    internal static PathGeometry CreateArcGeometry(Point center, double radius, double startAngle, double sweep)
    {
        var figure = new PathFigure
        {
            StartPoint = PointOnCircle(center, radius, startAngle),
            IsClosed = false,
            IsFilled = false,
        };
        figure.Segments!.Add(new ArcSegment
        {
            Point = PointOnCircle(center, radius, startAngle + sweep),
            Size = new Size(radius, radius),
            IsLargeArc = sweep > 180,
            SweepDirection = SweepDirection.Clockwise,
        });
        var geometry = new PathGeometry();
        geometry.Figures!.Add(figure);
        return geometry;
    }

    internal static (double StartAngle, double SweepAngle) CalculateIndeterminateArc(double progress)
    {
        double normalized = progress - Math.Floor(progress);
        double growth = 0.5 - (Math.Cos(normalized * Math.Tau) * 0.5);
        double sweep = 70 + (growth * 210);
        double start = (normalized * 720) - 90 - (growth * 110);
        return (start, sweep);
    }

    private static Point PointOnCircle(Point center, double radius, double angle)
    {
        double radians = angle * Math.PI / 180;
        return new Point(center.X + (Math.Cos(radians) * radius), center.Y + (Math.Sin(radians) * radius));
    }
}
