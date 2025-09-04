using UnityEngine;

namespace QuestMarkerTracking.Mapping
{
    public class AbstractVirtualObject : MonoBehaviour
    {
        public enum VirtualObjectType
        {
            School,
            Military,
            Hospital,
            Office,
            Bakery
        }
        
        [SerializeField] private VirtualObjectType objectType;
        public VirtualObjectType ObjectType => objectType;
    }
}