using Avalonia.Input;

namespace Aviora.Controls;

internal static class ChartSelectionState
{
    public static int FindIndex(IReadOnlyList<IChartDataPoint> items, IChartDataPoint? selectedItem)
    {
        if (selectedItem == null)
        {
            return -1;
        }

        for (int index = 0; index < items.Count; index++)
        {
            if (ReferenceEquals(items[index], selectedItem))
            {
                return index;
            }
        }

        if (selectedItem.Key == null)
        {
            return -1;
        }

        for (int index = 0; index < items.Count; index++)
        {
            if (Equals(items[index].Key, selectedItem.Key))
            {
                return index;
            }
        }

        return -1;
    }

    public static int NormalizeIndex(int index, int itemCount) =>
        index >= 0 && index < itemCount ? index : -1;

    public static int Move(int currentIndex, int itemCount, Key key)
    {
        if (itemCount == 0)
        {
            return -1;
        }

        return key switch
        {
            Key.Left or Key.Down => currentIndex <= 0 ? 0 : currentIndex - 1,
            Key.Right or Key.Up => currentIndex < 0 ? 0 : Math.Min(itemCount - 1, currentIndex + 1),
            Key.Home => 0,
            Key.End => itemCount - 1,
            _ => currentIndex,
        };
    }
}
