using UnityEngine;

namespace QuestMarkerTracking.Filter
{
    public class OneEuroFilterFloat
    {
        private readonly LowPass _dxFilter;

        private readonly LowPass _xFilter;
        private float _beta;
        private float _dCutoff;

        private float _lastTime = -1f;
        private float _lastValue;
        private float _minCutoff;

        public OneEuroFilterFloat(float minCutoff = 0.1f, float beta = 0.01f, float dCutoff = 1.0f)
        {
            _minCutoff = minCutoff;
            _beta = beta;
            _dCutoff = dCutoff;

            _xFilter = new LowPass();
            _dxFilter = new LowPass();
        }

        public float LastDerivative { get; private set; }

        public void UpdateParameters(float minCutoff, float beta, float dCutoff)
        {
            _minCutoff = minCutoff;
            _beta = beta;
            _dCutoff = dCutoff;
        }

        public float Filter(float value, float timestamp)
        {
            if (_lastTime < 0f)
            {
                _lastTime = timestamp;
                _lastValue = value;
                LastDerivative = 0f;

                _xFilter.Reset(value);
                _dxFilter.Reset();
                return value;
            }

            var dt = timestamp - _lastTime;
            if (dt <= 0f)
            {
                return _lastValue;
            }

            var dxRaw = (value - _lastValue) / dt;

            var alphaD = Alpha(_dCutoff, dt);
            var dx = _dxFilter.Filter(dxRaw, alphaD);

            var cutoff = _minCutoff + _beta * Mathf.Abs(dx);

            var alphaX = Alpha(cutoff, dt);
            var x = _xFilter.Filter(value, alphaX);

            _lastTime = timestamp;
            _lastValue = x;
            LastDerivative = dx;

            return x;
        }

        public void Reset()
        {
            _lastTime = -1f;
            _lastValue = 0f;
            LastDerivative = 0f;
            _xFilter.Reset();
            _dxFilter.Reset();
        }

        private float Alpha(float cutoff, float dt)
        {
            var tau = 1f / (2f * Mathf.PI * cutoff);
            return 1f / (1f + tau / dt);
        }

        private class LowPass
        {
            private bool _initialized;
            private float _y;

            public float Filter(float x, float alpha)
            {
                if (!_initialized)
                {
                    _y = x;
                    _initialized = true;
                    return _y;
                }

                _y = alpha * x + (1f - alpha) * _y;
                return _y;
            }

            public void Reset(float init = 0f)
            {
                _initialized = false;
                _y = init;
            }
        }
    }

    public class OneEuroFilterVector3Old
    {
        private readonly OneEuroFilterFloat _xFilter;
        private readonly OneEuroFilterFloat _yFilter;
        private readonly OneEuroFilterFloat _zFilter;

        public OneEuroFilterVector3Old(float minCutoff = 0.1f, float beta = 0.01f, float dCutoff = 1.0f)
        {
            _xFilter = new OneEuroFilterFloat(minCutoff, beta, dCutoff);
            _yFilter = new OneEuroFilterFloat(minCutoff, beta, dCutoff);
            _zFilter = new OneEuroFilterFloat(minCutoff, beta, dCutoff);
        }

        public Vector3 LastVelocity => new Vector3(_xFilter.LastDerivative, _yFilter.LastDerivative, _zFilter.LastDerivative);

        public void UpdateParameters(float minCutoff, float beta, float dCutoff)
        {
            _xFilter.UpdateParameters(minCutoff, beta, dCutoff);
            _yFilter.UpdateParameters(minCutoff, beta, dCutoff);
            _zFilter.UpdateParameters(minCutoff, beta, dCutoff);
        }

        public Vector3 Filter(Vector3 value, float timestamp)
        {
            var fx = _xFilter.Filter(value.x, timestamp);
            var fy = _yFilter.Filter(value.y, timestamp);
            var fz = _zFilter.Filter(value.z, timestamp);
            return new Vector3(fx, fy, fz);
        }

        public void Reset()
        {
            _xFilter.Reset();
            _yFilter.Reset();
            _zFilter.Reset();
        }
    }

    public class OneEuroFilterQuaternion
    {
        private readonly OneEuroFilterFloat _angleFilter;
        private readonly OneEuroFilterFloat _axisXFilter;
        private readonly OneEuroFilterFloat _axisYFilter;
        private readonly OneEuroFilterFloat _axisZFilter;
        private float _lastAngle;

        private Vector3 _lastAxis = Vector3.forward;
        private float _lastTime = -1f;

        public OneEuroFilterQuaternion(float minCutoff = 0.1f, float beta = 0.01f, float dCutoff = 1.0f)
        {
            _angleFilter = new OneEuroFilterFloat(minCutoff, beta, dCutoff);
            _axisXFilter = new OneEuroFilterFloat(minCutoff, beta, dCutoff);
            _axisYFilter = new OneEuroFilterFloat(minCutoff, beta, dCutoff);
            _axisZFilter = new OneEuroFilterFloat(minCutoff, beta, dCutoff);
        }

        public Vector3 LastAngularVelocity
        {
            get
            {
                var angVelDeg = _angleFilter.LastDerivative;
                var angVelRad = angVelDeg * Mathf.Deg2Rad;
                return _lastAxis * angVelRad;
            }
        }

        public void UpdateParameters(float minCutoff, float beta, float dCutoff)
        {
            _angleFilter.UpdateParameters(minCutoff, beta, dCutoff);
            _axisXFilter.UpdateParameters(minCutoff, beta, dCutoff);
            _axisYFilter.UpdateParameters(minCutoff, beta, dCutoff);
            _axisZFilter.UpdateParameters(minCutoff, beta, dCutoff);
        }

        public Quaternion Filter(Quaternion rawQuaternion, float timestamp)
        {
            rawQuaternion.Normalize();

            if (_lastTime < 0f)
            {
                _lastTime = timestamp;
                rawQuaternion.ToAngleAxis(out _lastAngle, out _lastAxis);
                if (_lastAngle > 180f) _lastAngle -= 360f;

                _angleFilter.Filter(_lastAngle, timestamp);
                _axisXFilter.Filter(_lastAxis.x, timestamp);
                _axisYFilter.Filter(_lastAxis.y, timestamp);
                _axisZFilter.Filter(_lastAxis.z, timestamp);

                return rawQuaternion;
            }

            var dt = timestamp - _lastTime;
            if (dt <= 0f)
            {
                return Quaternion.AngleAxis(_lastAngle, _lastAxis);
            }

            rawQuaternion.ToAngleAxis(out var rawAngle, out var rawAxis);
            if (rawAngle > 180f) rawAngle -= 360f;

            if (Vector3.Dot(rawAxis, _lastAxis) < 0f)
            {
                rawAxis = -rawAxis;
                rawAngle = -rawAngle;
            }

            var filteredAngle = _angleFilter.Filter(rawAngle, timestamp);

            var fx = _axisXFilter.Filter(rawAxis.x, timestamp);
            var fy = _axisYFilter.Filter(rawAxis.y, timestamp);
            var fz = _axisZFilter.Filter(rawAxis.z, timestamp);
            var filteredAxisRaw = new Vector3(fx, fy, fz);

            if (filteredAxisRaw.sqrMagnitude < 1e-6f)
            {
                filteredAxisRaw = _lastAxis;
            }

            var filteredAxis = filteredAxisRaw.normalized;

            var result = Quaternion.AngleAxis(filteredAngle, filteredAxis);

            _lastTime = timestamp;
            _lastAngle = filteredAngle;
            _lastAxis = filteredAxis;

            return result;
        }

        public void Reset()
        {
            _lastTime = -1f;
            _lastAngle = 0f;
            _lastAxis = Vector3.forward;

            _angleFilter.Reset();
            _axisXFilter.Reset();
            _axisYFilter.Reset();
            _axisZFilter.Reset();
        }
    }
}