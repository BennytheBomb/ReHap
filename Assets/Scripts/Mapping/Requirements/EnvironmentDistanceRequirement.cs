using System;
using System.Linq;
using QuestMarkerTracking.Utilities;
using UnityEngine;
using UnityEngine.Assertions;

namespace QuestMarkerTracking.Mapping.Requirements
{
    public class EnvironmentDistanceRequirement : AbstractSimulationRequirement
    {
        [SerializeField] private float requiredDistance;
        [SerializeField] private AbstractVirtualObject.VirtualObjectType virtualObjectType = AbstractVirtualObject.VirtualObjectType.School;
        [SerializeField] private DistanceType distanceType = DistanceType.CloseTo;
        [SerializeField] private Transform externalObject;

        protected override bool CheckSpecificRequirement(AbstractVirtualObject[] virtualObjects)
        {
            var virtualObject = virtualObjects.FirstOrDefault(virtualObject => virtualObject.ObjectType == virtualObjectType);
            
            Assert.IsNotNull(virtualObject, $"Virtual object of type {virtualObjectType} not found.");

            var position1 = virtualObject.transform.position.RemoveY();
            var position2 = externalObject.position.RemoveY();
            
            var distance = Vector2.Distance(position1, position2);

            return distanceType switch
            {
                DistanceType.CloseTo => distance < requiredDistance,
                DistanceType.AwayFrom => distance > requiredDistance,
                _ => throw new ArgumentOutOfRangeException(nameof(distanceType))
            };
        }
    }
}