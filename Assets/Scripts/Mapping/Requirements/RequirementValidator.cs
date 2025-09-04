using System.Collections.Generic;
using System.Linq;
using QuestMarkerTracking.Utilities;
using TMPro;
using UnityEngine;

namespace QuestMarkerTracking.Mapping.Requirements
{
    public class RequirementValidator : MonoBehaviour
    {
        [SerializeField] private VirtualObjectManager virtualObjectManager;
        [SerializeField] private RectTransform requirementLabelParent;
        [SerializeField] private TMP_Text requirementLabelPrefab;

        private readonly Dictionary<AbstractSimulationRequirement, TMP_Text> _requirementLabels = new();
        
        private List<AbstractSimulationRequirement> _requirements;

        private bool _isCompleted;

        private void Start()
        {
            _requirements = GetComponents<AbstractSimulationRequirement>().ToList();
            _requirements.Shuffle();

            foreach (var requirement in _requirements)
            {
                var label = Instantiate(requirementLabelPrefab, requirementLabelParent);
                label.text = requirement.requirementText;
                _requirementLabels[requirement] = label;
            }

            StudyLogger.Instance?.StartTask(UserStudyApp.Instance.CurrentScenario);
        }

        private void Update()
        {
            if (_isCompleted) return;
            
            foreach (var requirement in _requirements)
            {
                requirement.CheckRequirement(virtualObjectManager.GetVirtualObjects);
                if (_requirementLabels.TryGetValue(requirement, out var label))
                {
                    label.color = requirement.isRequirementMet ? Color.green : Color.red;
                }
            }
            
            if (_requirements.All(r => r.isRequirementMet))
            {
                StudyLogger.Instance?.EndTask();
                UserStudyApp.Instance.ShowUI();
                _isCompleted = true;
            }
        }
    }
}