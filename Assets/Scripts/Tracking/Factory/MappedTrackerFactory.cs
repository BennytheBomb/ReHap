using QuestMarkerTracking.Mapping;
using UnityEngine;

namespace QuestMarkerTracking.Tracking.Factory
{
    public class MappedTrackerFactory : AbstractTrackerFactory
    {
        [SerializeField] private Tracker trackerPrefab;
        
        public override Tracker SpawnTracker(int i)
        {
            var trackerGameObject = Instantiate(trackerPrefab, Vector3.zero, Quaternion.identity);
            trackerGameObject.name = $"Tracker #{i}";
            return trackerGameObject;
        }
    }
}