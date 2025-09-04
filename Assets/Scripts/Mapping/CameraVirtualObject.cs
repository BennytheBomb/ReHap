using System.Collections.Generic;
using System.Linq;
using QuestMarkerTracking.Tracking;
using QuestMarkerTracking.Utilities;
using UnityEngine;

namespace QuestMarkerTracking.Mapping
{
    public class CameraVirtualObject : AbstractVirtualObject
    {
        private static readonly List<CameraVirtualObject> SimulationVirtualObjects = new();

        [SerializeField] private Outline outline;
        
        private bool _keepOffset;

        private Tracker _pairedTracker;
        private int _upFace;
        private int _forwardFace;
        private float _lastAngle;
        public bool IsPaired => _pairedTracker;
        
        private void Start()
        {
            SimulationVirtualObjects.Add(this);
            
            Deselect();
        }

        private void OnDestroy()
        {
            SimulationVirtualObjects.Remove(this);
            
            if (_pairedTracker) _pairedTracker.Unpair();
        }

        private void Update()
        {
            if (_pairedTracker && !_keepOffset && !_pairedTracker.IsProposed)
            {
                var trackerPosition = _pairedTracker.transform.position;
                // if (TrackerSettings.Instance.lockVirtualObjectXZRotation) trackerPosition.y -= 0.04f;
                transform.position = trackerPosition;

                if (TrackerSettings.Instance.lockVirtualObjectXZRotation)
                {
                    var targetTransform = _pairedTracker.transform;
                    var faceClosestToUp = targetTransform.GetFaceClosestToUp();

                    if (faceClosestToUp != _upFace)
                    {
                        _forwardFace = _upFace;
                        _upFace = faceClosestToUp;
                        // Debug.Log("new up face: " + MathUtils.CubeFaceNormals[_upFace]);
                
                        var angle = CalculateForwardAngle(targetTransform);
                        _lastAngle = angle;
                    }
                    else
                    {
                        var angle = CalculateForwardAngle(targetTransform);
                        var deltaAngle = Mathf.DeltaAngle(_lastAngle, angle);
                        transform.Rotate(Vector3.up, -deltaAngle);
                        _lastAngle = angle;
                    }
                }
                else
                {
                    transform.rotation = _pairedTracker.transform.rotation;
                }
            }
        }

        private float CalculateForwardAngle(Transform targetTransform)
        {
            var forwardAxis = targetTransform.TransformDirection(MathUtils.CubeFaceNormals[_forwardFace]).normalized;
            var projectedForward = Vector3.ProjectOnPlane(forwardAxis, Vector3.up).normalized.RemoveY();
            var angle = Mathf.Atan2(projectedForward.y, projectedForward.x) * Mathf.Rad2Deg;
            return angle;
        }

        private void OnPairedTrackerMoved(Tracker tracker)
        {
            if (_pairedTracker == tracker)
            {
                Debug.Log("Paired tracker moved: " + tracker.name);
                _keepOffset = false;
            }
        }

        public void PairTracker(Tracker tracker, bool keepOffset)
        {
            _pairedTracker = tracker;
            _pairedTracker.onTrackerMoved.AddListener(OnPairedTrackerMoved);
            _keepOffset = keepOffset;
            Deselect();
            
            // Align the virtual object with the tracker
            _upFace = tracker.transform.GetFaceClosestToUp();
            _forwardFace = tracker.transform.GetFaceClosestToDirection(transform.forward);
            var forwardAxis = tracker.transform.TransformDirection(MathUtils.CubeFaceNormals[_forwardFace]).normalized;
            var projectedForward = Vector3.ProjectOnPlane(forwardAxis, Vector3.up).normalized;
            transform.rotation = Quaternion.LookRotation(projectedForward, Vector3.up);
        }

        public void UnpairTracker()
        {
            _pairedTracker?.onTrackerMoved.RemoveListener(OnPairedTrackerMoved);
            _pairedTracker = null;
        }

        public void Select(bool deselectOthers = true)
        {
            if (deselectOthers)
            {
                foreach (var obj in SimulationVirtualObjects.Where(obj => obj != this))
                {
                    obj.Deselect();
                }
            }

            outline.enabled = true;
        }

        public void Deselect()
        {
            outline.enabled = false;
        }
    }
}