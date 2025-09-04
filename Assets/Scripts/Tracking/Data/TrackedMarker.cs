using OpenCVForUnity.UnityUtils;

namespace QuestMarkerTracking.Tracking.Data
{
    public struct TrackedMarker
    {
        public int Id;
        public PoseData MarkerPoseData;
        public bool IsHighConfidence; // Whether multiple marker on a tracker have been detected
        public float Accuracy; // From 0 to 1, where 1 is perfect accuracy
    }
}