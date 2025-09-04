using System;
using System.Linq;
using QuestMarkerTracking.Utilities;
using UnityEngine;

namespace QuestMarkerTracking.Mapping.Requirements.Z_OLD
{
    [Obsolete]
    public class MilitaryRequirement : AbstractSimulationRequirement
    {
        [SerializeField] private float requiredDistance = 5f;

        protected override bool CheckSpecificRequirement(AbstractVirtualObject[] virtualObjects)
        {
            var schoolVirtualObject = virtualObjects.FirstOrDefault(virtualObject => virtualObject.ObjectType == AbstractVirtualObject.VirtualObjectType.School);
            var militaryVirtualObject = virtualObjects.FirstOrDefault(virtualObject => virtualObject.ObjectType == AbstractVirtualObject.VirtualObjectType.Military);

            if (!schoolVirtualObject || !militaryVirtualObject)
            {
                Debug.LogError("School or military not found.");
                return false;
            }

            var schoolPosition = schoolVirtualObject.transform.position.RemoveY();
            var militaryPosition = militaryVirtualObject.transform.position.RemoveY();
            var distance = Vector2.Distance(schoolPosition, militaryPosition);

            return distance > requiredDistance;
        }
    }
}