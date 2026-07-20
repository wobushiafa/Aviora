namespace Aviora.Controls;

internal sealed class ChartAnimationController
{
    private List<double> _currentValues = [];
    private List<double> _startValues = [];
    private List<double> _targetValues = [];

    public IReadOnlyList<double> Values => _currentValues;

    public bool IsAnimating { get; private set; }

    public bool SetTargets(IEnumerable<double> targets, bool animate)
    {
        _targetValues = targets.ToList();
        AlignCurrentValues();
        _startValues = _currentValues.ToList();
        IsAnimating = animate && _targetValues.Count > 0;
        if (!IsAnimating)
        {
            Complete();
        }

        return IsAnimating;
    }

    public bool Advance(double progress)
    {
        if (!IsAnimating)
        {
            return false;
        }

        progress = Math.Clamp(progress, 0, 1);
        double easedProgress = 1 - Math.Pow(1 - progress, 3);
        for (int index = 0; index < _targetValues.Count; index++)
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
        _currentValues = _targetValues.ToList();
        _startValues = _targetValues.ToList();
        IsAnimating = false;
    }

    public void Stop() => IsAnimating = false;

    private void AlignCurrentValues()
    {
        while (_currentValues.Count < _targetValues.Count)
        {
            _currentValues.Add(0);
        }

        if (_currentValues.Count > _targetValues.Count)
        {
            _currentValues.RemoveRange(_targetValues.Count, _currentValues.Count - _targetValues.Count);
        }
    }
}
