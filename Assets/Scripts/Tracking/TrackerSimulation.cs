using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using QuestMarkerTracking.Tracking.Data;
using QuestMarkerTracking.Utilities;
using TMPro;
using UnityEngine;

namespace QuestMarkerTracking.Tracking
{
    public class TrackerSimulation : MonoBehaviour
    {
        private readonly ConcurrentQueue<FrameTrackerData> _frameTrackerDataQueue = new();
        private readonly Dictionary<int, GameObject> _markerGameObjectDictionary = new();
        private readonly Dictionary<int, TrackerSnapshot> _trackerSnapshots = new();

        public int TrackerCount => _markerGameObjectDictionary.Values.Count;
        public List<GameObject> Trackers => _markerGameObjectDictionary.Values.ToList();

        private void Awake()
        {
            BuildMarkerDictionary();
        }
        
        private void OnEnable()
        {
            Application.onBeforeRender += OnBeforeRender;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= OnBeforeRender;
        }

        public bool HasTargetForMarkerId(int markerId)
        {
            return _markerGameObjectDictionary.TryGetValue(markerId, out var targetObject) && targetObject;
        }

        public void EnqueueFrameTrackerData(long frameTimestampNs, List<TrackedMarker> trackedMarkers)
        {
            var frameTrackerData = new FrameTrackerData
            {
                FrameTimestampNs = frameTimestampNs,
                TrackedMarkers = trackedMarkers,
                UntrackedMarkerIds = _markerGameObjectDictionary.Keys.Except(trackedMarkers.Select(marker => marker.Id)).ToList()
            };
            _frameTrackerDataQueue.Enqueue(frameTrackerData);
        }

        public void SetMarkerObjectsVisibility(bool isVisible)
        {
            foreach (var markerObject in _markerGameObjectDictionary.Values)
            {
                if (!markerObject) continue;

                var rendererList = markerObject.GetComponentsInChildren<Renderer>(true);
                foreach (var meshRenderer in rendererList)
                {
                    meshRenderer.enabled = isVisible;
                }
            }
        }

        // Catches the newest frame data right before rendering and updates the marker positions accordingly.
        // No heavy calculations! Make them in Update() or LateUpdate()
        private void OnBeforeRender()
        {
            UpdateTrackerSnapshots();
            UpdateTrackerPose();
        }

        private void UpdateTrackerPose()
        {
            foreach (var (markerId, targetObject) in _markerGameObjectDictionary)
            {
                var trackerSnapshot = _trackerSnapshots[markerId];

                var nowNs = SystemUtils.GetNanoTime();
                var markerPoseData = trackerSnapshot.CalculatePoseData(nowNs);
                
                targetObject.transform.position = markerPoseData.pos;
                targetObject.transform.rotation = markerPoseData.rot;
            }
        }

        private void UpdateTrackerSnapshots()
        {
            while (!_frameTrackerDataQueue.IsEmpty)
            {
                if (!_frameTrackerDataQueue.TryDequeue(out var frameTrackerData)) continue;

                var frameTimestampNs = frameTrackerData.FrameTimestampNs;

                foreach (var trackerData in frameTrackerData.TrackedMarkers)
                {
                    var markerId = trackerData.Id;
                    var trackerSnapshot = _trackerSnapshots[markerId];

                    // _markerGameObjectDictionary[markerId].GetComponentInChildren<TMP_Text>().text = $"id:{trackerData.Id}\n" +
                    //                                                                       $"confidence:{trackerData.IsHighConfidence}\n" +
                    //                                                                       $"accuracy:{trackerData.Accuracy:0.00}";

                    trackerSnapshot.Simulate(trackerData, frameTimestampNs);
                }

                foreach (var markerId in frameTrackerData.UntrackedMarkerIds)
                {
                    var markerSnapshot = _trackerSnapshots[markerId];
                    if (!markerSnapshot.trackingLost) StudyLogger.Instance.LogCubeTrackingLost(markerId);
                    markerSnapshot.trackingLost = true;
                }
            }
        }

        private void BuildMarkerDictionary()
        {
            _markerGameObjectDictionary.Clear();
            for (var i = 0; i < TrackerManager.Instance.Trackers.Count; i++)
            {
                _markerGameObjectDictionary[i] = TrackerManager.Instance.Trackers[i].gameObject;
                _trackerSnapshots[i] = new TrackerSnapshot();
            }
        }

        [Serializable]
        public class MarkerGameObjectPair
        {
            public int markerId;
            public GameObject gameObject;
        }

        private struct FrameTrackerData
        {
            public long FrameTimestampNs;
            public List<TrackedMarker> TrackedMarkers;
            public List<int> UntrackedMarkerIds;
        }
    }
}