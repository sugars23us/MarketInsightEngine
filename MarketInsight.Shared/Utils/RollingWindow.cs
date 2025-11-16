
using System;

namespace MarketInsight.Shared.Utils
{
    /// <summary>
    /// Numerically stable fixed-size rolling window keeping
    /// mean and sample variance using Welford's algorithm.
    /// </summary>
    public sealed class RollingWindow
    {
        private readonly int _capacity;
        private readonly double[] _buf;
        private int _count;
        private int _head;
        private double _mean;
        private double _m2; // sum of squared diffs

        public RollingWindow(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
            _buf = new double[capacity];
        }

        public int Capacity => _capacity;
        public int Count => _count;

        public double Mean => _count == 0 ? double.NaN : _mean;
        public double VarianceSample => _count >= 2 ? _m2 / (_count - 1) : double.NaN;
        public double StdSample => double.IsNaN(VarianceSample) ? double.NaN : Math.Sqrt(VarianceSample);

        /// <summary>Add a new value, evicting the oldest if full.</summary>
        public void Add(double x)
        {
            if (_count < _capacity)
            {
                _count++;
                var delta = x - _mean;
                _mean += delta / _count;
                var delta2 = x - _mean;
                _m2 += delta * delta2;

                _buf[_head] = x;
                _head = (_head + 1) % _capacity;
                return;
            }

            // remove oldest then add new (sliding Welford)
            var old = _buf[_head];
            var oldMean = _mean;

            var meanAfterRemove = (_count * _mean - old) / (_count - 1);
            var m2AfterRemove = _m2 - (old - oldMean) * (old - meanAfterRemove);

            var deltaAdd = x - meanAfterRemove;
            var meanAfterAdd = meanAfterRemove + deltaAdd / _count;
            var delta2Add = x - meanAfterAdd;
            var m2AfterAdd = m2AfterRemove + deltaAdd * delta2Add;

            _mean = meanAfterAdd;
            _m2 = m2AfterAdd;

            _buf[_head] = x;
            _head = (_head + 1) % _capacity;
        }

        public void Clear()
        {
            Array.Clear(_buf, 0, _buf.Length);
            _count = 0;
            _head = 0;
            _mean = 0;
            _m2 = 0;
        }
    }
}
