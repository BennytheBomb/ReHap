using System;
using System.Linq;
using QuestMarkerTracking.Utilities;
using UnityEngine;
using UnityEngine.Assertions;

namespace QuestMarkerTracking.Mapping.Requirements
{
    public enum DistanceType
    {
        CloseTo,
        AwayFrom
    }
    
    public class DistanceRequirement : AbstractSimulationRequirement
    {
        [SerializeField] private float requiredDistance;
        [SerializeField] private AbstractVirtualObject.VirtualObjectType virtualObjectType1 = AbstractVirtualObject.VirtualObjectType.School;
        [SerializeField] private DistanceType distanceType = DistanceType.CloseTo;
        [SerializeField] private AbstractVirtualObject.VirtualObjectType virtualObjectType2 = AbstractVirtualObject.VirtualObjectType.Bakery;
        
        protected override bool CheckSpecificRequirement(AbstractVirtualObject[] virtualObjects)
        {
            Assert.AreNotEqual(virtualObjectType1, virtualObjectType2, "Virtual object types must be different for distance requirement.");
            
            var virtualObject1 = virtualObjects.FirstOrDefault(virtualObject => virtualObject.ObjectType == virtualObjectType1);
            var virtualObject2 = virtualObjects.FirstOrDefault(virtualObject => virtualObject.ObjectType == virtualObjectType2);
            
            Assert.IsNotNull(virtualObject1, $"Virtual object of type {virtualObjectType1} not found.");
            Assert.IsNotNull(virtualObject2, $"Virtual object of type {virtualObjectType2} not found.");

            var position1 = virtualObject1.transform.position.RemoveY();
            var position2 = virtualObject2.transform.position.RemoveY();
            
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