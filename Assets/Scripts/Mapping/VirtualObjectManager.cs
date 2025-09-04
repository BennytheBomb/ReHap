using UnityEngine;

namespace QuestMarkerTracking.Mapping
{
    public class VirtualObjectManager : MonoBehaviour
    {
        [SerializeField] private AbstractVirtualObject[] virtualObjects;
        public AbstractVirtualObject[] GetVirtualObjects => virtualObjects;
    }
}