using System;

namespace MarketInsight.Application.Indicators
{
    /// <summary>
    /// Classic RSI(14) helper using Wilder's smoothing.
    /// Feed it consecutive price deltas: (close_t - close_{t-1}).
    /// </summary>
    public sealed class Rsi14Helper
    {
        private const int Period = 14;
        private double _avgGain;
        private double _avgLoss;
        private bool _initialized;

        public void Reset()
        {
            _avgGain = 0;
            _avgLoss = 0;
            _initialized = false;
        }

        public void AddDelta(double delta)
        {
            var gain = Math.Max(0.0, delta);
            var loss = Math.Max(0.0, -delta);

            if (!_initialized)
            {
                _avgGain = gain;
                _avgLoss = loss;
                _initialized = true;
                return;
            }

            _avgGain = (_avgGain * (Period - 1) + gain) / Period;
            _avgLoss = (_avgLoss * (Period - 1) + loss) / Period;
        }

        public double GetValue()
        {
            if (!_initialized)
                return double.NaN;

            if (_avgLoss == 0.0)
                return 100.0;

            var rs = _avgGain / _avgLoss;
            return 100.0 - (100.0 / (1.0 + rs));
        }
    }
}
