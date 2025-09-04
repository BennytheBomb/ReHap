using TMPro;
using UnityEngine;

namespace QuestMarkerTracking.Mapping.Requirements
{
    public abstract class AbstractSimulationRequirement : MonoBehaviour
    {
        public string requirementText;
        
        [HideInInspector] public bool isRequirementMet;

        protected abstract bool CheckSpecificRequirement(AbstractVirtualObject[] virtualObjects);

        public void CheckRequirement(AbstractVirtualObject[] virtualObjects)
        {
            var requirementMet = CheckSpecificRequirement(virtualObjects);
            
            if (isRequirementMet && !requirementMet)
            {
                // Log requirement failure
                StudyLogger.Instance?.LogError(requirementText);
            }

            isRequirementMet = requirementMet;
        }
    }
}