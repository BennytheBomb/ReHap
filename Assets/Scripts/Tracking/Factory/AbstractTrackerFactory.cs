using QuestMarkerTracking.Mapping;
using UnityEngine;

namespace QuestMarkerTracking.Tracking.Factory
{
    public abstract class AbstractTrackerFactory : MonoBehaviour
    {
        public abstract Tracker SpawnTracker(int i);
    }
}