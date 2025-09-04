using OpenCVForUnity.UnityUtils;
using UnityEngine;

namespace QuestMarkerTracking.Filter
{
    public abstract class AbstractTrackerFilter : MonoBehaviour
    {
        public abstract PoseData UpdateTracker(int id, PoseData pose, float deltaTimestampSeconds);
    }
}