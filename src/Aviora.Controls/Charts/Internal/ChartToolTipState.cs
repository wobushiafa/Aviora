using Avalonia;

namespace Aviora.Controls;

internal sealed class ChartToolTipState
{
    public int HoveredIndex { get; private set; } = -1;

    public Point AnchorPosition { get; private set; }

    public bool Update(int index, Point pointerPosition)
    {
        if (index == HoveredIndex)
        {
            return false;
        }

        HoveredIndex = index;
        if (index >= 0)
        {
            AnchorPosition = pointerPosition;
        }

        return true;
    }

    public bool Clear() => Update(-1, default);

    public void Normalize(int itemCount)
    {
        if (HoveredIndex >= itemCount)
        {
            Clear();
        }
    }
}
