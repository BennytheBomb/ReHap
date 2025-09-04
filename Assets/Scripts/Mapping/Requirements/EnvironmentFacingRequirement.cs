using System;
using System.Linq;
using QuestMarkerTracking.Utilities;
using UnityEngine;
using UnityEngine.Assertions;

namespace QuestMarkerTracking.Mapping.Requirements
{
    public class EnvironmentFacingRequirement : AbstractSimulationRequirement
    {
        [SerializeField] private float requiredAngle = 30f;
        [SerializeField] private AbstractVirtualObject.VirtualObjectType virtualObjectType = AbstractVirtualObject.VirtualObjectType.School;
        [SerializeField] private FacingType facingType = FacingType.FacingAt;
        [SerializeField] private Transform externalObject;
        
        protected override bool CheckSpecificRequirement(AbstractVirtualObject[] virtualObjects)
        {
            var virtualObject = virtualObjects.FirstOrDefault(virtualObject => virtualObject.ObjectType == virtualObjectType);
            
            Assert.IsNotNull(virtualObject, $"Virtual object of type {virtualObjectType} not found.");

            var position1 = virtualObject.transform.position.RemoveY();
            var position2 = externalObject.position.RemoveY();
            
            var direction = (position2 - position1).normalized;
            
            var forward = Vector3.ProjectOnPlane(virtualObject.transform.forward, Vector3.up).normalized.RemoveY();
            
            var angle = Vector2.Angle(forward, direction);
            
            return facingType switch
            {
                FacingType.FacingAt => angle < requiredAngle,
                FacingType.FacingAway => angle > requiredAngle,
                _ => throw new ArgumentOutOfRangeException(nameof(facingType))
            };
        }
    }
}