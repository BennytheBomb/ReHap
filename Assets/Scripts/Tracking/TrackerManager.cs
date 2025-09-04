using System.Collections.Generic;
using QuestMarkerTracking.Mapping;
using QuestMarkerTracking.Tracking.Factory;
using UnityEngine;

namespace QuestMarkerTracking.Tracking
{
    [DefaultExecutionOrder(-1), RequireComponent(typeof(AbstractTrackerFactory))]
    public class TrackerManager : MonoBehaviour
    {
        public static TrackerManager Instance { get; private set; }
        
        [SerializeField] private int trackerCount = 10;
        [SerializeField] private Camera gazeCamera;

        public List<Tracker> Trackers { get; private set; } = new();
        public Camera GetGazeCamera() => gazeCamera;
        
        private AbstractTrackerFactory _trackerFactory;

        private void Awake()
        {
            if (Instance)
            {
                Debug.LogError($"Instance of {nameof(TrackerManager)} already exists!");
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            
            _trackerFactory = GetComponent<AbstractTrackerFactory>();
            
            for (var i = 0; i < trackerCount; i++)
            {
                Trackers.Add(_trackerFactory.SpawnTracker(i));
            }
        }
    }
}