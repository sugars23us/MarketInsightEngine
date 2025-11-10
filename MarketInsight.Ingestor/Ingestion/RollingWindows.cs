namespace MarketInsight.Ingestor.Ingestion;

/// Fixed-size sliding window keeping sum & sumsq (for mean/std) and sum only (for SMA).
public sealed class RollingWindow
{
    private readonly int _capacity;
    private readonly double[] _buf;
    private int _count;     // number of valid samples (<= capacity)
    private int _head;      // next index to overwrite
    private double _mean;   // current mean
    private double _m2;     // sum of squared diffs for variance

    public RollingWindow(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
        _buf = new double[capacity];
    }

    public int Count => _count;
    public int Capacity => _capacity;

    public double Mean => (_count == 0) ? double.NaN : _mean;

    public double VarianceSample =>
        (_count >= 2) ? (_m2 / (_count - 1)) : double.NaN;

    public double StdSample
        => double.IsNaN(VarianceSample) ? double.NaN : Math.Sqrt(VarianceSample);

    /// <summary>Add a new value, evicting the oldest when the window is full.</summary>
    public void Add(double x)
    {
        if (_count < _capacity)
        {
            // standard Welford update
            _count++;
            var delta = x - _mean;
            _mean += delta / _count;
            var delta2 = x - _mean;
            _m2 += delta * delta2;

            // store and advance
            _buf[_head] = x;
            _head = (_head + 1) % _capacity;
        }
        else
        {
            // remove oldest, then add newest (Welford sliding window)
            var old = _buf[_head];

            // remove 'old'
            // (ref: Bennett et al. “Numerically Stable, Single-Pass, Parallel Statistics …”)
            var oldMean = _mean;
            var newMeanAfterRemoval = (_count * _mean - old) / (_count - 1);
            var m2AfterRemoval = _m2 - (old - oldMean) * (old - newMeanAfterRemoval);

            // now add 'x'
            var newCount = _count; // stays at capacity
            var deltaAdd = x - newMeanAfterRemoval;
            var meanAfterAdd = newMeanAfterRemoval + deltaAdd / newCount;
            var delta2Add = x - meanAfterAdd;
            var m2AfterAdd = m2AfterRemoval + deltaAdd * delta2Add;

            _mean = meanAfterAdd;
            _m2 = m2AfterAdd;

            // overwrite oldest and advance
            _buf[_head] = x;
            _head = (_head + 1) % _capacity;
        }
    }

    /// <summary>Clears all samples.</summary>
    public void Clear()
    {
        Array.Clear(_buf, 0, _buf.Length);
        _count = 0;
        _head = 0;
        _mean = 0;
        _m2 = 0;
    }
}
