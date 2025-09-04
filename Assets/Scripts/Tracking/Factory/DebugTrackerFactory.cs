using QuestMarkerTracking.Mapping;
using UnityEngine;

namespace QuestMarkerTracking.Tracking.Factory
{
    public class DebugTrackerFactory : AbstractTrackerFactory
    {
        [SerializeField] private Tracker trackerPrefab;
        
        public override Tracker SpawnTracker(int i)
        {
            // Get DebugUtils.CubeFace enum value based on the index as a string
            // var cubeFaceName = ((DebugUtils.CubeFace)(i % 6)).ToString();
            var trackerGameObject = Instantiate(trackerPrefab, Vector3.zero, Quaternion.identity);
            trackerGameObject.name = $"Tracker #{i}";
            
            // trackerGameObject.GetComponentInChildren<TMP_Text>().text = $"{cubeFaceName} / {i % 6} / {i * 4}-{(i + 1) * 4 - 1}";

            return trackerGameObject;
        }
    }
}