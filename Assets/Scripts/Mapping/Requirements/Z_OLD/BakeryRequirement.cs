using System;
using System.Linq;
using QuestMarkerTracking.Utilities;
using UnityEngine;

namespace QuestMarkerTracking.Mapping.Requirements.Z_OLD
{
    [Obsolete]
    public class BakeryRequirement : AbstractSimulationRequirement
    {
        [SerializeField] private float requiredDistance = 3f;
        [SerializeField] private Transform centerTransform;

        protected override bool CheckSpecificRequirement(AbstractVirtualObject[] virtualObjects)
        {
            var schoolVirtualObject = virtualObjects.FirstOrDefault(virtualObject => virtualObject.ObjectType == AbstractVirtualObject.VirtualObjectType.Bakery);
            if (!schoolVirtualObject)
            {
                Debug.LogError("Bakery not found.");
                return false;
            }

            var bakeryPosition = schoolVirtualObject.transform.position.RemoveY();
            var centerPosition = centerTransform.position.RemoveY();
            var distance = Vector2.Distance(bakeryPosition, centerPosition); // Assuming the requirement is based on distance from origin

            return distance < requiredDistance;
        }
    }
}