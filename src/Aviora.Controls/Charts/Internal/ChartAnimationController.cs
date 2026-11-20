using System.Collections;
using Avalonia.Animation.Easings;

namespace Aviora.Controls;

internal sealed class ChartAnimationController
{
    private readonly BufferView _valuesView;
    private double[] _currentValues = [];
    private double[] _startValues = [];
    private double[] _targetValues = [];
    private int _count;

    public ChartAnimationController()
    {
        _valuesView = new BufferView(this);
    }

    public IReadOnlyList<double> Values => _valuesView;

    public bool IsAnimating { get; private set; }

    public bool SetTargets(IEnumerable<double> targets, bool animate)
    {
        int newCount = 0;
        if (targets is ICollection<double> collection)
        {
            EnsureCapacity(collection.Count);
            foreach (double value in collection)
            {
                _targetValues[newCount++] = value;
            }
        }
        else
        {
            foreach (double value in targets)
            {
                EnsureCapacity(newCount + 1);
                _targetValues[newCount++] = value;
            }
        }

        if (newCount > _count)
        {
            Array.Clear(_currentValues, _count, newCount - _count);
        }

        _count = newCount;
        Array.Copy(_currentValues, _startValues, _count);
        IsAnimating = animate && _count > 0;
        if (!IsAnimating)
        {
            Complete();
        }

        return IsAnimating;
    }

    public bool Advance(double progress, Easing? easing = null)
    {
        if (!IsAnimating)
        {
            return false;
        }

        progress = Math.Clamp(progress, 0, 1);
        double easedProgress = easing != null
            ? easing.Ease(progress)
            : 1 - Math.Pow(1 - progress, 3);
        for (int index = 0; index < _count; index++)
        {
            _currentValues[index] = _startValues[index] +
                                    ((_targetValues[index] - _startValues[index]) * easedProgress);
        }

        if (progress >= 1)
        {
            Complete();
        }

        return IsAnimating;
    }

    public void Complete()
    {
        if (_count > 0)
        {
            Array.Copy(_targetValues, _currentValues, _count);
            Array.Copy(_targetValues, _startValues, _count);
        }

        IsAnimating = false;
    }

    public void Stop() => IsAnimating = false;

    private void EnsureCapacity(int required)
    {
        if (required <= _targetValues.Length)
        {
            return;
        }

        int newCapacity = Math.Max(required, _targetValues.Length == 0 ? 16 : _targetValues.Length * 2);
        Array.Resize(ref _currentValues, newCapacity);
        Array.Resize(ref _startValues, newCapacity);
        Array.Resize(ref _targetValues, newCapacity);
    }

    private sealed class BufferView(ChartAnimationController owner) : IReadOnlyList<double>
    {
        public int Count => owner._count;

        public double this[int index]
        {
            get
            {
                if ((uint)index >= (uint)owner._count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return owner._currentValues[index];
            }
        }

        public IEnumerator<double> GetEnumerator()
        {
            for (int index = 0; index < owner._count; index++)
            {
                yield return owner._currentValues[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
