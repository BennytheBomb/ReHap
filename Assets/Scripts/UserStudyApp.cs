using System;
using System.Collections.Generic;
using QuestMarkerTracking.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QuestMarkerTracking
{
    public class UserStudyApp : MonoBehaviour
    {
        [Serializable]
        private class InteractionScenarios
        {
            public string tutorial;
            public List<string> scenarios;
        }
        
        public static UserStudyApp Instance { get; private set; }
        
        [SerializeField] private List<InteractionScenarios> interactionScenarios;
        
        [SerializeField] private GameObject userInterface;
        
        [SerializeField] private Toggle nextScenarioButton;
        [SerializeField] private Toggle startScenarioButton;
        [SerializeField] private Toggle finishScenarioButton;
        [SerializeField] private Toggle setTableHeightButton;

        [SerializeField] private GameObject headsetSetupView;
        
        [SerializeField] private Slider progressBar;
        [SerializeField] private TMP_Text progressText;
        
        [SerializeField] private TMP_Text descriptionText;
        
        [SerializeField] private OVRInput.Button forceShowUIButton = OVRInput.Button.Start;
        
        private int _currentScenarioIndex;
        private readonly List<string> _scenarios = new();
        
        public string CurrentScenario { get; private set; }

        private void Awake()
        {
            if (Instance)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            
            // interactionScenarios.Shuffle();

            foreach (var interactionScenario in interactionScenarios)
            {
                // interactionScenario.scenarios.Shuffle();
                
                _scenarios.Add(interactionScenario.tutorial);
                _scenarios.AddRange(interactionScenario.scenarios);
            }

            progressBar.minValue = 0;
            progressBar.maxValue = _scenarios.Count;
            progressBar.wholeNumbers = true;
            
            ShowUI();
            headsetSetupView.SetActive(true);
        }

        private void Update()
        {
            if (OVRInput.GetDown(forceShowUIButton))
            {
                ShowUI();
            }
        }

        private void HideUI()
        {
            userInterface.SetActive(false);
            headsetSetupView.SetActive(false);
        }
        
        public void ShowUI()
        {
            UpdateUI();
            userInterface.SetActive(true);
        }

        private void UpdateUI()
        {
            progressBar.value = _currentScenarioIndex;
            progressText.text = _currentScenarioIndex + "/" + _scenarios.Count;

            if (_currentScenarioIndex == 0)
            {
                startScenarioButton.gameObject.SetActive(true);
                nextScenarioButton.gameObject.SetActive(false);
                finishScenarioButton.gameObject.SetActive(false);
                setTableHeightButton.gameObject.SetActive(true);
                descriptionText.gameObject.SetActive(false);
            } else if (_currentScenarioIndex >= _scenarios.Count)
            {
                startScenarioButton.gameObject.SetActive(false);
                nextScenarioButton.gameObject.SetActive(false);
                finishScenarioButton.gameObject.SetActive(true);
                setTableHeightButton.gameObject.SetActive(false);
                descriptionText.gameObject.SetActive(false);
            }
            else
            {
                startScenarioButton.gameObject.SetActive(false);
                nextScenarioButton.gameObject.SetActive(true);
                finishScenarioButton.gameObject.SetActive(false);
                setTableHeightButton.gameObject.SetActive(false);

                descriptionText.gameObject.SetActive(interactionScenarios
                    .Find(scenario =>
                        scenario.tutorial == _scenarios[_currentScenarioIndex]) != null);
            }
        }
        
        public void LoadNextScenario(bool value)
        {
            HideUI();
            LoadNextScenarioAsync();
        }
        
        public void FinishScenario(bool value)
        {
            StudyLogger.Instance?.SaveLogs();
            Application.Quit();
        }

        private async void LoadNextScenarioAsync()
        {
            try
            {
                if (_currentScenarioIndex < 0 || _currentScenarioIndex >= _scenarios.Count)
                {
                    Debug.LogError("Scenario _currentScenarioIndex out of bounds: " + _currentScenarioIndex);
                    return;
                }
                var scenario = _scenarios[_currentScenarioIndex];
                
                if (_currentScenarioIndex > 0)
                {
                    var previousScene = FindSceneByName(_scenarios[_currentScenarioIndex - 1]);
                    await SceneManager.UnloadSceneAsync(previousScene);
                }
            
                await SceneManager.LoadSceneAsync(scenario, LoadSceneMode.Additive);
                CurrentScenario = scenario;
                _currentScenarioIndex++;
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }
        
        private static Scene FindSceneByName(string sceneName)
        {
            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var scene = SceneManager.GetSceneByBuildIndex(i);
                if (scene.name == sceneName)
                {
                    return scene;
                }
            }
            return default;
        }
    }
}
