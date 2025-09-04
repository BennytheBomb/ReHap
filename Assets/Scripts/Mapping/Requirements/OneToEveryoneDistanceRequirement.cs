using System;
using System.Linq;
using QuestMarkerTracking.Utilities;
using UnityEngine;
using UnityEngine.Assertions;

namespace QuestMarkerTracking.Mapping.Requirements
{
    public class OneToEveryoneDistanceRequirement : AbstractSimulationRequirement
    {
        [SerializeField] private float requiredDistance;
        [SerializeField] private AbstractVirtualObject.VirtualObjectType virtualObjectType = AbstractVirtualObject.VirtualObjectType.School;
        [SerializeField] private DistanceType distanceType = DistanceType.CloseTo;
        
        protected override bool CheckSpecificRequirement(AbstractVirtualObject[] virtualObjects)
        {
            var virtualObject = virtualObjects.FirstOrDefault(virtualObject => virtualObject.ObjectType == virtualObjectType);
            
            Assert.IsNotNull(virtualObject, $"Virtual object of type {virtualObjectType} not found.");

            var position = virtualObject.transform.position.RemoveY();
            
            return virtualObjects
                .Where(vo => vo != virtualObject)
                .All(vo =>
                {
                    var distance = Vector2.Distance(vo.transform.position.RemoveY(), position);
                    return distanceType switch
                    {
                        DistanceType.CloseTo => distance < requiredDistance,
                        DistanceType.AwayFrom => distance > requiredDistance,
                        _ => throw new ArgumentOutOfRangeException(nameof(distanceType))
                    };
                });
        }
    }
}