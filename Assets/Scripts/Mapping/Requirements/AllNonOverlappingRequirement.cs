using System.Linq;
using QuestMarkerTracking.Utilities;
using UnityEngine;

namespace QuestMarkerTracking.Mapping.Requirements
{
    public class AllNonOverlappingRequirement : AbstractSimulationRequirement
    {
        [SerializeField] private float requiredDistance = 1.5f;

        protected override bool CheckSpecificRequirement(AbstractVirtualObject[] virtualObjects)
        {
            return virtualObjects.All(virtualObject =>
                virtualObjects
                    .Where(others => others != virtualObject)
                    .All(others
                        => Vector2.Distance(others.transform.position.RemoveY(), virtualObject.transform.position.RemoveY()) > requiredDistance));
        }
    }
}