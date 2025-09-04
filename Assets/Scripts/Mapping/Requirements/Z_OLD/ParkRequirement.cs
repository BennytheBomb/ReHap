using System;
using System.Linq;
using QuestMarkerTracking.Utilities;
using UnityEngine;

namespace QuestMarkerTracking.Mapping.Requirements.Z_OLD
{
    [Obsolete]
    public class ParkRequirement : AbstractSimulationRequirement
    {
        [SerializeField] private float requiredDistance = 10f;
        [SerializeField] private Transform parkTransform;

        protected override bool CheckSpecificRequirement(AbstractVirtualObject[] virtualObjects)
        {
            var schoolVirtualObject = virtualObjects.FirstOrDefault(virtualObject => virtualObject.ObjectType == AbstractVirtualObject.VirtualObjectType.School);
            if (!schoolVirtualObject)
            {
                Debug.LogError("Bakery not found.");
                return false;
            }

            if (!parkTransform)
            {
                Debug.LogError("Park transform not assigned.");
                return false;
            }

            var schoolPosition = schoolVirtualObject.transform.position.RemoveY();
            var parkPosition = parkTransform.position.RemoveY();
            var distance = Vector2.Distance(schoolPosition, parkPosition);

            return distance < requiredDistance;
        }
    }
}