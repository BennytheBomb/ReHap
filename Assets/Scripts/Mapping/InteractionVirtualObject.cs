using QuestMarkerTracking.Tracking;
using QuestMarkerTracking.Utilities;
using UnityEngine;

namespace QuestMarkerTracking.Mapping
{
    public class InteractionVirtualObject : AbstractVirtualObject
    {
        [SerializeField] private Transform virtualTrackerTransform;
        
        private int _upFace;
        private int _forwardFace;
        private float _lastAngle;

        private void Update()
        {
            var trackerPosition = virtualTrackerTransform.transform.position;
            transform.position = trackerPosition;

            if (TrackerSettings.Instance.lockVirtualObjectXZRotation)
            {
                var targetTransform = virtualTrackerTransform.transform;
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
                transform.rotation = virtualTrackerTransform.transform.rotation;
            }
        }

        private float CalculateForwardAngle(Transform targetTransform)
        {
            var forwardAxis = targetTransform.TransformDirection(MathUtils.CubeFaceNormals[_forwardFace]).normalized;
            var projectedForward = Vector3.ProjectOnPlane(forwardAxis, Vector3.up).normalized.RemoveY();
            var angle = Mathf.Atan2(projectedForward.y, projectedForward.x) * Mathf.Rad2Deg;
            return angle;
        }
    }
}