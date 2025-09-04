using Oculus.Interaction;
using UnityEngine;

namespace QuestMarkerTracking.Mapping
{
    public class VirtualTracker : MonoBehaviour
    {
        public void OnGrab()
        {
            Debug.Log("VirtualTracker grabbed: " + gameObject.name);
            StudyLogger.Instance?.LogHandPickup(gameObject.name);
        }
    }
}