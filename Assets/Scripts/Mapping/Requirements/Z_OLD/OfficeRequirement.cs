using System;
using System.Linq;
using QuestMarkerTracking.Utilities;
using UnityEngine;

namespace QuestMarkerTracking.Mapping.Requirements.Z_OLD
{
    [Obsolete]
    public class OfficeRequirement : AbstractSimulationRequirement
    {
        [SerializeField] private float requiredDistance = 3f;

        protected override bool CheckSpecificRequirement(AbstractVirtualObject[] virtualObjects)
        {
            var officeVirtualObject = virtualObjects.FirstOrDefault(virtualObject => virtualObject.ObjectType == AbstractVirtualObject.VirtualObjectType.Office);
            if (!officeVirtualObject)
            {
                Debug.LogError("Office not found.");
                return false;
            }

            return virtualObjects
                .Where(virtualObject => virtualObject != officeVirtualObject)
                .All(virtualObject => Vector2.Distance(virtualObject.transform.position.RemoveY(), officeVirtualObject.transform.position.RemoveY()) > requiredDistance);
        }
    }
}