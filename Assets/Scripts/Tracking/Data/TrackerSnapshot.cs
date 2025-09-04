using System;
using Oculus.Interaction.Input;
using Oculus.Interaction.Throw;
using OpenCVForUnity.UnityUtils;
using QuestMarkerTracking.Utilities;
using UnityEngine;

namespace QuestMarkerTracking.Tracking.Data
{
    public class TrackerSnapshot
    {
        public enum VelocityCalculatorType
        {
            Standard,
            OneEuroVelocity,
            RANSAC
        }

        private readonly IOneEuroFilter<Pose> _activePoseFilter = OneEuroFilter.CreatePose();
        private readonly IOneEuroFilter<Vector3> _angularVelocityFilter = OneEuroFilter.CreateVector3();
        private readonly IOneEuroFilter<Pose> _passivePoseFilter = OneEuroFilter.CreatePose();

        private readonly RANSACVelocity _velocityCalculator = new(3, 0);
        private readonly IOneEuroFilter<Vector3> _velocityFilter = OneEuroFilter.CreateVector3();
        private Vector3 _acceleration;
        private Vector3 _angularVelocity = Vector3.forward;

        private long _frameTimestampNs = long.MinValue;

        private bool _isHandInRange;

        private PoseData _trackerPoseData = new() { pos = Vector3.zero, rot = Quaternion.identity };

        private Vector3 _velocity = Vector3.zero;

        public bool trackingLost = true;

        public PoseData CalculatePoseData(long nowNs)
        {
            var lastFrameTimestampNs = _frameTimestampNs;
            var deltaTime = MathUtils.NanosecondsToSeconds(nowNs - lastFrameTimestampNs);

            if ((trackingLost && deltaTime > TrackerSettings.Instance.markerDetectionTimeoutSeconds) || (TrackerSettings.Instance.useHandTracking && !_isHandInRange))
            {
                // Stop predicting if the marker is not detected for too long or hand isn't interacting and use the last known pose
                return _trackerPoseData;
            }

            var predPos = _trackerPoseData.pos;

            if (TrackerSettings.Instance.useVelocityPrediction) predPos += _velocity * deltaTime;
            if (TrackerSettings.Instance.useAccelerationPrediction) predPos += _acceleration * 0.5f * deltaTime * deltaTime;

            var predRot = _trackerPoseData.rot;

            var angle = _angularVelocity.magnitude * deltaTime;
            var axis = _angularVelocity.normalized;
            if (TrackerSettings.Instance.useAngularVelocityPrediction) predRot *= Quaternion.AngleAxis(angle, axis);

            return new PoseData
            {
                pos = predPos,
                rot = predRot
            };
        }

        public void Simulate(TrackedMarker trackerData, long frameTimestampNs)
        {
            if (_frameTimestampNs > frameTimestampNs)
            {
                Debug.LogWarning($"[{nameof(MarkerTracking)}] Received stale data for marker {trackerData.Id} with timestamp {frameTimestampNs}. Ignoring it.");
                return;
            }

            _activePoseFilter.SetProperties(in TrackerSettings.Instance.activePoseFilterProperties);
            var passivePoseProperties = TrackerSettings.Instance.passivePoseFilterProperties;
            // if (TrackerSettings.Instance.useAccuracyFiltering) passivePoseProperties._minCutoff *= trackerData.Accuracy;
            _passivePoseFilter.SetProperties(in passivePoseProperties);

            _angularVelocityFilter.SetProperties(in TrackerSettings.Instance.angularVelocityFilterProperties);
            _velocityFilter.SetProperties(in TrackerSettings.Instance.velocityFilterProperties);

            var previousPosition = _trackerPoseData.pos;
            var previousRotation = _trackerPoseData.rot;
            var previousTimestampNs = _frameTimestampNs;
            var previousVelocity = _velocity;
            var deltaTime = MathUtils.NanosecondsToSeconds(frameTimestampNs - previousTimestampNs);

            var isHandInRange = IsHandInRange(trackerData);

            var rawPose = new Pose(trackerData.MarkerPoseData.pos, trackerData.MarkerPoseData.rot);
            var activePose = _activePoseFilter.Step(rawPose, deltaTime);
            var passivePose = _passivePoseFilter.Step(rawPose, deltaTime);

            var filteredPose = isHandInRange ? activePose : passivePose;
            
            var finalPose = TrackerSettings.Instance.useAccuracyFiltering ? new Pose
            {
                position = Vector3.Lerp(previousPosition, filteredPose.position, trackerData.Accuracy),
                rotation = Quaternion.Slerp(previousRotation, filteredPose.rotation, trackerData.Accuracy)
            } : filteredPose;

            var currentPosition = TrackerSettings.Instance.useFiltering ? finalPose.position : rawPose.position;
            var currentRotation = TrackerSettings.Instance.useFiltering ? finalPose.rotation : rawPose.rotation;

            var standardVelocity = (currentPosition - previousPosition) / deltaTime;
            
            // Log here to ensure we log the latest data and not the stale one
            if (Vector3.Distance(currentPosition, previousPosition) > 0.01f || Quaternion.Angle(currentRotation, previousRotation) > 5f) StudyLogger.Instance?.LogCubeTrackerData(trackerData);
            
            var filteredVelocity = _velocityFilter.Step(standardVelocity, deltaTime);

            _velocityCalculator.Process(finalPose, MathUtils.NanosecondsToSeconds(frameTimestampNs), trackerData.IsHighConfidence);
            _velocityCalculator.GetVelocities(out var ransacVelocity, out var ransacAngularVelocity);

            var velocity = TrackerSettings.Instance.velocityCalculatorType switch
            {
                VelocityCalculatorType.Standard => standardVelocity,
                VelocityCalculatorType.OneEuroVelocity => filteredVelocity,
                VelocityCalculatorType.RANSAC => ransacVelocity,
                _ => throw new NotImplementedException($"Velocity calculator type {TrackerSettings.Instance.velocityCalculatorType} is not implemented.")
            };

            var acceleration = (velocity - previousVelocity) / deltaTime;

            var deltaRotation = Quaternion.Inverse(previousRotation) * currentRotation;
            deltaRotation.ToAngleAxis(out var angle, out var axis);
            var standardAngularVelocity = axis * angle / deltaTime;
            var filteredAngularVelocity = _angularVelocityFilter.Step(standardAngularVelocity, deltaTime);

            var angularVelocity = TrackerSettings.Instance.velocityCalculatorType switch
            {
                VelocityCalculatorType.Standard => standardAngularVelocity,
                VelocityCalculatorType.OneEuroVelocity => filteredAngularVelocity,
                VelocityCalculatorType.RANSAC => ransacAngularVelocity,
                _ => throw new NotImplementedException($"Angular velocity calculator type {TrackerSettings.Instance.velocityCalculatorType} is not implemented.")
            };

            _trackerPoseData = new PoseData
            {
                pos = currentPosition,
                rot = currentRotation
            };
            _velocity = velocity;
            _acceleration = acceleration;
            _angularVelocity = angularVelocity;
            _frameTimestampNs = frameTimestampNs;
            _isHandInRange = isHandInRange;
            trackingLost = false;
        }

        private static bool IsHandInRange(TrackedMarker trackerData)
        {
            if (!TrackerSettings.Instance.useHandTracking) return true;

            var leftHandDistance = Vector3.Distance(trackerData.MarkerPoseData.pos, TrackerSettings.Instance.LeftHandPosition);
            var rightHandDistance = Vector3.Distance(trackerData.MarkerPoseData.pos, TrackerSettings.Instance.RightHandPosition);

            var isLeftHandInRange = TrackerSettings.Instance.IsLeftHandTracked && leftHandDistance < TrackerSettings.Instance.handDistanceThreshold;
            var isRightHandInRange = TrackerSettings.Instance.IsRightHandTracked && rightHandDistance < TrackerSettings.Instance.handDistanceThreshold;

            var isHandInRange = isLeftHandInRange || isRightHandInRange;
            return isHandInRange;
        }
    }
}