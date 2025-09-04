using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace QuestMarkerTracking
{
    public class ScenarioButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text buttonText;

        private Toggle _toggle;

        private void Awake()
        {
            _toggle = GetComponent<Toggle>();
        }

        private void OnDestroy()
        {
            _toggle.onValueChanged.RemoveAllListeners();
        }

        public void SetupScenarioButton(int index, UnityAction<bool> onButtonPressed)
        {
            if (buttonText) buttonText.text = "Scenario " + (index + 1);
            else Debug.LogWarning("Button text component is not assigned.");
            
            _toggle.onValueChanged.AddListener(onButtonPressed);
        }
    }
}
