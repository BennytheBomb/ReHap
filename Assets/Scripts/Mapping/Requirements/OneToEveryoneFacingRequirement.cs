using System;
using System.Linq;
using QuestMarkerTracking.Utilities;
using UnityEngine;
using UnityEngine.Assertions;

namespace QuestMarkerTracking.Mapping.Requirements
{
    public class OneToEveryoneFacingRequirement : AbstractSimulationRequirement
    {
        [SerializeField] private float requiredAngle = 30f;
        [SerializeField] private AbstractVirtualObject.VirtualObjectType virtualObjectType = AbstractVirtualObject.VirtualObjectType.School;
        [SerializeField] private FacingType facingType = FacingType.FacingAt;
        
        protected override bool CheckSpecificRequirement(AbstractVirtualObject[] virtualObjects)
        {
            var virtualObject = virtualObjects.FirstOrDefault(virtualObject => virtualObject.ObjectType == virtualObjectType);
            
            Assert.IsNotNull(virtualObject, $"Virtual object of type {virtualObjectType} not found.");

            var position = virtualObject.transform.position.RemoveY();
            
            return virtualObjects
                .Where(vo => vo != virtualObject)
                .All(vo =>
                {
                    var voPosition = vo.transform.position.RemoveY();
                    var direction = (voPosition - position).normalized;
                    var forward = Vector3.ProjectOnPlane(virtualObject.transform.forward, Vector3.up).normalized.RemoveY();
                    var angle = Vector2.Angle(forward, direction);
            
                    return facingType switch
                    {
                        FacingType.FacingAt => angle < requiredAngle,
                        FacingType.FacingAway => angle > requiredAngle,
                        _ => throw new ArgumentOutOfRangeException(nameof(facingType))
                    };
                });
        }
    }
}