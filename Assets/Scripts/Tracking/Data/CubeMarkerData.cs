using OpenCVForUnity.UnityUtils;

namespace QuestMarkerTracking.Tracking.Data
{
    public struct CubeMarkerData
    {
        public int SideIndex { get; set; }
        public PoseData MarkerPoseData { get; set; }
        public float Accuracy { get; set; }
    }
}