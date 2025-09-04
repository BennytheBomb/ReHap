using QuestMarkerTracking.Utilities;
using UnityEngine;

namespace QuestMarkerTracking
{
    /// <summary>
    /// Fixes the Y rotation of the GameObject to only rotate around the global y axis no matter what side is pointing up.
    /// </summary>
    public class YRotationFix : MonoBehaviour
    {
        [SerializeField] private Transform targetTransform;
        private float _lastAngle;
        private int _upFace;
        private int _forwardFace;

        private void Update()
        {
            var faceClosestToUp = targetTransform.GetFaceClosestToUp();

            if (faceClosestToUp != _upFace)
            {
                _forwardFace = _upFace;
                _upFace = faceClosestToUp;
                Debug.Log("new up face: " + MathUtils.CubeFaceNormals[_upFace]);
                
                var angle = CalculateForwardAngle();
                _lastAngle = angle;
            }
            else
            {
                var angle = CalculateForwardAngle();
                var deltaAngle = Mathf.DeltaAngle(_lastAngle, angle);
                transform.Rotate(Vector3.up, -deltaAngle);
                _lastAngle = angle;
            }
        }

        private float CalculateForwardAngle()
        {
            var forwardAxis = targetTransform.TransformDirection(MathUtils.CubeFaceNormals[_forwardFace]).normalized;
            var projectedForward = Vector3.ProjectOnPlane(forwardAxis, Vector3.up).normalized.RemoveY();
            var angle = Mathf.Atan2(projectedForward.y, projectedForward.x) * Mathf.Rad2Deg;
            return angle;
        }
    }
}
