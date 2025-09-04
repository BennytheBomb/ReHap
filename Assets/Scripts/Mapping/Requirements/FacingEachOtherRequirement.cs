using System;
using System.Linq;
using QuestMarkerTracking.Utilities;
using UnityEngine;
using UnityEngine.Assertions;

namespace QuestMarkerTracking.Mapping.Requirements
{
    public enum FacingType
    {
        FacingAt,
        FacingAway
    }
    
    public class FacingEachOtherRequirement : AbstractSimulationRequirement
    {
        [SerializeField] private float requiredAngle = 30f;
        [SerializeField] private AbstractVirtualObject.VirtualObjectType virtualObjectType1 = AbstractVirtualObject.VirtualObjectType.School;
        [SerializeField] private FacingType facingType = FacingType.FacingAt;
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
            
            var direction = (position2 - position1).normalized;
            
            var forward = Vector3.ProjectOnPlane(virtualObject1.transform.forward, Vector3.up).normalized.RemoveY();
            
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