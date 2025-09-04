using Meta.XR.ImmersiveDebugger;
using Oculus.Interaction.Input;
using QuestMarkerTracking.Tracking.Data;
using UnityEngine;

namespace QuestMarkerTracking.Tracking
{
    [DefaultExecutionOrder(-1)]
    public class TrackerSettings : MonoBehaviour
    {
        private const string CATEGORY = nameof(TrackerSettings);

        public static TrackerSettings Instance { get; private set; }
        
        [Header("General Settings")]
        
        [DebugMember(Category = CATEGORY, Tweakable = true)] public bool loggingEnabled = true;

        
        [Header("Detection")]
        
        [DebugMember(Category = CATEGORY, Tweakable = true, Min = 0f, Max = 1f)] public float markerDetectionTimeoutSeconds = 0.1f;
        [DebugMember(Category = CATEGORY, Tweakable = true)] public bool useWeightedAverage = true;
        
        [Header("Prediction")]
        
        [DebugMember(Category = CATEGORY, Tweakable = true)] public bool useVelocityPrediction = true;
        [DebugMember(Category = CATEGORY, Tweakable = true)] public bool useAccelerationPrediction = true;
        [DebugMember(Category = CATEGORY, Tweakable = true)] public bool useAngularVelocityPrediction = true;
        [DebugMember(Category = CATEGORY, Tweakable = true)] public TrackerSnapshot.VelocityCalculatorType velocityCalculatorType = TrackerSnapshot.VelocityCalculatorType.Standard;

        [Header("Filtering")]
        
        [DebugMember(Category = CATEGORY, Tweakable = true)] public bool useFiltering = true;
        [Range(0f, 1f)] [DebugMember(Category = CATEGORY, Tweakable = true, Min = 0f, Max = 1f)] public float handDistanceThreshold = 0.3f;
        [DebugMember(Category = CATEGORY, Tweakable = true)] public bool useHandTracking = true;
        [DebugMember(Category = CATEGORY, Tweakable = true)] public bool useAccuracyFiltering = true;
        
        [Header("Filter Settings")]
        
        public OneEuroFilterPropertyBlock activePoseFilterProperties;
        public OneEuroFilterPropertyBlock passivePoseFilterProperties;
        public OneEuroFilterPropertyBlock velocityFilterProperties;
        public OneEuroFilterPropertyBlock angularVelocityFilterProperties;
        
        [Header("Tracking")]
        
        [DebugMember(Category = CATEGORY, Tweakable = true)] public float movementEpsilon = 0.05f;
        [DebugMember(Category = CATEGORY, Tweakable = true)] public float angleThresholdDegrees = 10f;
        [DebugMember(Category = CATEGORY, Tweakable = true)] public bool lockVirtualObjectXZRotation = true;

        [Header("Optional")]
        
        [SerializeField] private OVRHand leftHand;
        [SerializeField] private OVRHand rightHand;
        
        [Header("Debug")]
        
        [DebugMember(Category = CATEGORY, Tweakable = true)] public bool showGizmos = true;

        public bool IsLeftHandTracked { get; private set; }
        public bool IsRightHandTracked { get; private set; }
        public Vector3 LeftHandPosition { get; private set; }
        public Vector3 RightHandPosition { get; private set; }
        public Quaternion RightHandRotation { get; private set; }
        public Quaternion LeftHandRotation { get; private set; }
        public OVRHand LeftHand => leftHand;
        public OVRHand RightHand => rightHand;

        private void Awake()
        {
            if (Instance) Destroy(this);

            Instance = this;
        }

        private void LateUpdate()
        {
            if (leftHand)
            {
                if (Vector3.Distance(LeftHandPosition, leftHand.transform.position) > 0.02f || Quaternion.Angle(LeftHandRotation, leftHand.transform.rotation) > 3f)
                    StudyLogger.Instance?.LogHandMotion("LeftHand", leftHand.transform.position, leftHand.transform.rotation);
                LeftHandPosition = leftHand.transform.position;
                LeftHandRotation = leftHand.transform.rotation;
                if (!leftHand.IsTracked && IsLeftHandTracked) StudyLogger.Instance?.LogHandTrackingLost("LeftHand");
                IsLeftHandTracked = leftHand.IsTracked;
            }

            if (rightHand)
            {
                if (Vector3.Distance(RightHandPosition, rightHand.transform.position) > 0.02f || Quaternion.Angle(RightHandRotation, rightHand.transform.rotation) > 3f) 
                    StudyLogger.Instance?.LogHandMotion("RightHand", rightHand.transform.position, rightHand.transform.rotation);
                RightHandPosition = rightHand.transform.position;
                RightHandRotation = rightHand.transform.rotation;
                if (!rightHand.IsTracked && IsRightHandTracked) StudyLogger.Instance?.LogHandTrackingLost("RightHand");
                IsRightHandTracked = rightHand.IsTracked;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}