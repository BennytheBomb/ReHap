using System;
using UnityEngine;

namespace QuestMarkerTracking.Filter
{
    /// <summary>
    ///     Based on implementation from here: https://github.com/casiez/OneEuroFilter/blob/main/java/OneEuroFilter.java
    /// </summary>
    internal class LowPassFilter
    {
        private bool _isInitialized;

        private float _y, _alpha, _s;

        public LowPassFilter(float alpha)
        {
            Initialize(alpha, 0);
        }

        public LowPassFilter(float alpha, float value)
        {
            Initialize(alpha, value);
        }

        private void SetAlpha(float alpha)
        {
            if (alpha <= 0.0 || alpha > 1.0)
            {
                throw new Exception("alpha should be in (0.0., 1.0]");
            }

            _alpha = alpha;
        }

        private void Initialize(float alpha, float value)
        {
            _y = _s = value;
            SetAlpha(alpha);
            _isInitialized = false;
        }

        private float Filter(float value)
        {
            float result;
            if (_isInitialized)
            {
                result = _alpha * value + (1f - _alpha) * _s;
            }
            else
            {
                result = value;
                _isInitialized = true;
            }

            _y = value;
            _s = result;
            return result;
        }

        public float FilterWithAlpha(float value, float alpha)
        {
            SetAlpha(alpha);
            return Filter(value);
        }

        public bool HasLastRawValue()
        {
            return _isInitialized;
        }

        public float LastRawValue()
        {
            return _y;
        }

        public float LastFilteredValue()
        {
            return _s;
        }
    }

    public class OneEuroFilterVector3
    {
        private readonly OneEuroFilterOldNew _xFilterOldNew;
        private readonly OneEuroFilterOldNew _yFilterOldNew;
        private readonly OneEuroFilterOldNew _zFilterOldNew;

        public OneEuroFilterVector3(float frequency = 30f, float minCutoff = 1f, float beta = 0f, float dCutoff = 1f)
        {
            _xFilterOldNew = new OneEuroFilterOldNew(frequency, minCutoff, beta, dCutoff);
            _yFilterOldNew = new OneEuroFilterOldNew(frequency, minCutoff, beta, dCutoff);
            _zFilterOldNew = new OneEuroFilterOldNew(frequency, minCutoff, beta, dCutoff);
        }

        public Vector3 Filter(Vector3 value, float timestamp)
        {
            var fx = _xFilterOldNew.Filter(value.x, timestamp);
            var fy = _yFilterOldNew.Filter(value.y, timestamp);
            var fz = _zFilterOldNew.Filter(value.z, timestamp);
            return new Vector3(fx, fy, fz);
        }

        public void UpdateParameters(float frequency, float minCutoff, float beta, float dCutoff)
        {
            _xFilterOldNew.UpdateParameters(frequency, minCutoff, beta, dCutoff);
            _yFilterOldNew.UpdateParameters(frequency, minCutoff, beta, dCutoff);
            _zFilterOldNew.UpdateParameters(frequency, minCutoff, beta, dCutoff);
        }
    }

    public class OneEuroFilterOldNew
    {
        private const float UNDEFINED_TIME = -1;
        private float _beta;
        private float _dCutoff;
        private LowPassFilter _dx;

        private float _frequency;
        private float _lastTime;
        private float _minCutoff;
        private LowPassFilter _x;

        public OneEuroFilterOldNew(float freq)
        {
            Initialize(freq, 1f, 0f, 1f);
        }

        public OneEuroFilterOldNew(float freq, float minCutoff)
        {
            Initialize(freq, minCutoff, 0f, 1f);
        }

        public OneEuroFilterOldNew(float freq, float minCutoff, float beta)
        {
            Initialize(freq, minCutoff, beta, 1f);
        }

        public OneEuroFilterOldNew(float freq, float minCutoff, float beta, float dCutoff)
        {
            Initialize(freq, minCutoff, beta, dCutoff);
        }

        private float Alpha(float cutoff)
        {
            var te = 1f / _frequency;
            var tau = 1f / (2 * Mathf.PI * cutoff);
            return 1f / (1f + tau / te);
        }

        private void SetFrequency(float f)
        {
            if (f <= 0)
            {
                throw new Exception("freq should be >0");
            }

            _frequency = f;
        }

        private void SetMinCutoff(float mc)
        {
            if (mc <= 0)
            {
                throw new Exception($"[{nameof(OneEuroFilterOldNew)}] minCutoff should be >0");
            }

            _minCutoff = mc;
        }

        private void SetBeta(float b)
        {
            _beta = b;
        }

        private void SetDerivativeCutoff(float dc)
        {
            if (dc <= 0)
            {
                throw new Exception($"[{nameof(OneEuroFilterOldNew)}] dCutoff should be >0");
            }

            _dCutoff = dc;
        }

        public void UpdateParameters(float frequency = 30f, float minCutoff = 1f, float beta = 0f, float dCutoff = 1f)
        {
            SetFrequency(frequency);
            SetMinCutoff(minCutoff);
            SetBeta(beta);
            SetDerivativeCutoff(dCutoff);
        }

        private void Initialize(float frequency,
            float minCutoff, float beta, float dCutoff)
        {
            SetFrequency(frequency);
            SetMinCutoff(minCutoff);
            SetBeta(beta);
            SetDerivativeCutoff(dCutoff);
            _x = new LowPassFilter(Alpha(minCutoff));
            _dx = new LowPassFilter(Alpha(dCutoff));
            _lastTime = UNDEFINED_TIME;
        }

        public float Filter(float value, float timestamp = UNDEFINED_TIME)
        {
            // update the sampling frequency based on timestamps
            if (!Mathf.Approximately(_lastTime, UNDEFINED_TIME) && !Mathf.Approximately(timestamp, UNDEFINED_TIME) && timestamp > _lastTime)
            {
                _frequency = 1f / (timestamp - _lastTime);
            }

            _lastTime = timestamp;
            // estimate the current variation per second
            var dValue = _x.HasLastRawValue() ? (value - _x.LastFilteredValue()) * _frequency : 0f; // FIXME: 0.0 or value?
            var edValue = _dx.FilterWithAlpha(dValue, Alpha(_dCutoff));
            // use it to update the cutoff frequency
            var cutoff = _minCutoff + _beta * Mathf.Abs(edValue);
            // filter the given value
            return _x.FilterWithAlpha(value, Alpha(cutoff));
        }
    }
}