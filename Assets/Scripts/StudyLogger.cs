using System;
using System.Collections.Generic;
using System.IO;
using QuestMarkerTracking.Tracking;
using QuestMarkerTracking.Tracking.Data;
using UnityEngine;

namespace QuestMarkerTracking
{
    [DefaultExecutionOrder(-1)]
    public class StudyLogger : MonoBehaviour
    {
        public static StudyLogger Instance { get; private set; }
        
        private string _filePath;
        private readonly List<string> _logEntries = new();
        private string _scenarioName;
        private bool _taskActive;
        private DateTime _taskStartTime;
        private string _userId;

        private void Awake()
        {
            if (Instance)
            {
                Debug.LogWarning("[StudyLogger] Instance already exists.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            
            _userId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var folder = Path.Combine(Application.persistentDataPath, "StudyLogs");
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, $"{_userId}.csv");

            _logEntries.Add("timestamp;event;data");
        }

        private void AddLog(string eventType, string data)
        {
            if (!TrackerSettings.Instance.loggingEnabled) return;
            if (!_taskActive) return;
            
            var timestamp = DateTime.UtcNow.ToString("o");
            _logEntries.Add($"{timestamp};{eventType};{data}");
        }

        public void StartTask(string scenarioName)
        {
            _taskActive = true;
            _taskStartTime = DateTime.UtcNow;
            _scenarioName = scenarioName;
            AddLog("TaskStart", $"Scenario:{scenarioName}");
        }

        public void EndTask()
        {
            var duration = DateTime.UtcNow - _taskStartTime;
            AddLog("TaskEnd", $"Scenario:{_scenarioName}/Duration:{duration.TotalSeconds}s");
            _taskActive = false;
        }

        public void LogError(string description)
        {
            AddLog("Error", description);
        }

        public void LogCubeTrackerData(TrackedMarker trackedMarker)
        {
            AddLog("CubeMotion",
                $"CubeId:{trackedMarker.Id}/Pos:{trackedMarker.MarkerPoseData.pos}/Rot:{trackedMarker.MarkerPoseData.rot.eulerAngles}/Accuracy:{trackedMarker.Accuracy}");
        }
        
        public void LogCubeTrackingLost(int cubeId)
        {
            AddLog("TrackingLost", $"CubeId:{cubeId}");
        }

        public void LogHandMotion(string hand, Vector3 position, Quaternion rotation)
        {
            AddLog("HandMotion", $"Hand:{hand}/Pos:{position}/Rot:{rotation.eulerAngles}");
        }
        
        public void LogHandTrackingLost(string hand)
        {
            AddLog("HandTrackingLost", $"Hand:{hand}");
        }

        public void LogProposal()
        {
            AddLog("Proposal", "1");
        }

        public void LogPairing(string cubeId, string objectName)
        {
            AddLog("Pairing", $"CubeId:{cubeId}/Object:{objectName}");
        }

        public void LogHandPickup(string objectName)
        {
            AddLog("HandPickup", $"Object:{objectName}");
        }

        public void LogTilt(string cubeId)
        {
            AddLog("Tilt", $"CubeId:{cubeId}");
        }

        public void SaveLogs()
        {
            if (!TrackerSettings.Instance.loggingEnabled) return;
            
            File.WriteAllLines(_filePath, _logEntries);
            Debug.Log($"[StudyLogger] Logs saved to {_filePath}");
        }
    }
}