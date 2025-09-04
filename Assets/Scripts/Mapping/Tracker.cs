using System.Collections.Generic;
using System.Linq;
using QuestMarkerTracking.Tracking;
using QuestMarkerTracking.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Utilities.XR;

namespace QuestMarkerTracking.Mapping
{
    public class Tracker : MonoBehaviour
    {
        private static readonly List<Tracker> SimulationTrackers = new();

        [SerializeField] private Outline outline;
        [SerializeField] private Image progressFill;
        [SerializeField] private MeshRenderer modelRenderer;

        public UnityEvent<Tracker> onTrackerMoved;
        public UnityEvent<Tracker> onTopFaceChanged;

        private Vector3 _previousPosition;

        private CameraVirtualObject _pairedVirtualObject;

        public float LastUsedTime { get; private set; }
        public bool IsProposed { get; private set; }
        public bool IsPaired => _pairedVirtualObject;

        private float _pairingProgress;
        private int _upwardFace = -1;
        private int _currentTopFace = -1;

        private void Start()
        {
            progressFill.fillAmount = 0;
            _currentTopFace = transform.GetFaceClosestToUp(out _);
            Unpropose();
        }

        private void Update()
        {
            if (Vector3.Distance(transform.position, _previousPosition) > TrackerSettings.Instance.movementEpsilon)
            {
                onTrackerMoved?.Invoke(this);

                _previousPosition = transform.position;
            }

            var mostUpwardFace = transform.GetFaceClosestToUp(out var deltaAngle);

            if (mostUpwardFace != _upwardFace && deltaAngle < TrackerSettings.Instance.angleThresholdDegrees)
            {
                Debug.Log("Most upward face index: " + mostUpwardFace + ", angle to up: " + deltaAngle);
                _upwardFace = mostUpwardFace;
                onTopFaceChanged?.Invoke(this);
                StudyLogger.Instance.LogTilt(gameObject.name);
            }

            if (TrackerSettings.Instance.showGizmos)
            {
                var nextUpwardFace = transform.GetFaceClosestToUp(out var nextDeltaAngle, _upwardFace);
                var color = Color.Lerp(DebugUtils.CubeFaceColors[_upwardFace], DebugUtils.CubeFaceColors[nextUpwardFace], 0.5f * (90f - nextDeltaAngle) / 90f);
                XRGizmos.DrawWireCube(transform.position, transform.rotation, Vector3.one * 0.08f, color);
            }
        }

        private void OnEnable()
        {
            SimulationTrackers.Add(this);
        }

        private void OnDisable()
        {
            SimulationTrackers.Remove(this);
        }

        public void Pair(CameraVirtualObject selectedCameraVirtualObject, bool keepOffset = false)
        {
            if (selectedCameraVirtualObject.IsPaired) return;
            
            StudyLogger.Instance.LogPairing(gameObject.name, selectedCameraVirtualObject.name);
            
            LastUsedTime = Time.time;

            Unpair();

            _pairedVirtualObject = selectedCameraVirtualObject;
            _pairedVirtualObject.PairTracker(this, keepOffset);

            modelRenderer.enabled = false;

            foreach (var tracker in SimulationTrackers)
            {
                tracker.Unpropose();
            }
        }

        public void Unpair()
        {
            if (!IsPaired) return;
            
            modelRenderer.enabled = true;
            _pairedVirtualObject.UnpairTracker();
            _pairedVirtualObject = null;
        }

        public void Propose(bool deselectOthers = true)
        {
            if (deselectOthers)
            {
                foreach (var tracker in SimulationTrackers.Where(tracker => tracker != this))
                {
                    tracker.Unpropose();
                }
            }

            modelRenderer.enabled = true;
            outline.enabled = true;
            IsProposed = true;
        }

        public void Unpropose()
        {
            if (IsPaired) modelRenderer.enabled = false;
            outline.enabled = false;
            IsProposed = false;
        }

        public void Highlight()
        {
            outline.enabled = true;
        }

        public void Deselect()
        {
            outline.enabled = false;
        }

        public void RotateNext()
        {
            _currentTopFace = (_currentTopFace + 1) % MathUtils.CubeFaceNormals.Length;
            transform.rotation = Quaternion.LookRotation(transform.up, MathUtils.CubeFaceNormals[_currentTopFace]);
        }
    }
}