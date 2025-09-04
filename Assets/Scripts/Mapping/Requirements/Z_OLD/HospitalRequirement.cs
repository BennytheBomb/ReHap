using System;
using System.Linq;
using QuestMarkerTracking.Utilities;
using UnityEngine;

namespace QuestMarkerTracking.Mapping.Requirements.Z_OLD
{
    [Obsolete]
    public class HospitalRequirement : AbstractSimulationRequirement
    {
        [SerializeField] private float requiredDistance = 2f;

        protected override bool CheckSpecificRequirement(AbstractVirtualObject[] virtualObjects)
        {
            var schoolVirtualObject = virtualObjects.FirstOrDefault(virtualObject => virtualObject.ObjectType == AbstractVirtualObject.VirtualObjectType.School);
            var hospitalVirtualObject = virtualObjects.FirstOrDefault(virtualObject => virtualObject.ObjectType == AbstractVirtualObject.VirtualObjectType.Hospital);

            if (!schoolVirtualObject || !hospitalVirtualObject)
            {
                Debug.LogError("School or hospital not found.");
                return false;
            }

            var schoolPosition = schoolVirtualObject.transform.position.RemoveY();
            var hospitalPosition = hospitalVirtualObject.transform.position.RemoveY();
            var distance = Vector2.Distance(schoolPosition, hospitalPosition);

            return distance < requiredDistance;
        }
    }
}