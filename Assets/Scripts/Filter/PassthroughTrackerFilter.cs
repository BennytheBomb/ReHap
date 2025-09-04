using OpenCVForUnity.UnityUtils;

namespace QuestMarkerTracking.Filter
{
    public class PassthroughTrackerFilter : AbstractTrackerFilter
    {
        public override PoseData UpdateTracker(int id, PoseData pose, float deltaTimestampSeconds)
        {
            return pose;
        }
    }
}