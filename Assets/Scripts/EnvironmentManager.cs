using QuestMarkerTracking.Mapping;
using UnityEngine;

namespace QuestMarkerTracking
{
    public class EnvironmentManager : MonoBehaviour
    {
        private void Start()
        {
            var newHeight = TableHeightCalibration.Instance.FloorHeight;
            transform.position = new Vector3(transform.position.x, newHeight, transform.position.z);
            var resetObjectHeights = FindObjectsByType<AbstractVirtualObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var resetObject in resetObjectHeights)
            {
                resetObject.transform.position = new Vector3(resetObject.transform.position.x, newHeight + 0.04f, resetObject.transform.position.z);
            }
            var resetTracker = FindObjectsByType<VirtualTracker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var resetObject in resetTracker)
            {
                resetObject.transform.position = new Vector3(resetObject.transform.position.x, newHeight + 0.04f, resetObject.transform.position.z);
            }
        }
    }
}
