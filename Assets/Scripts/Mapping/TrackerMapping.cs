using System;
using System.Linq;
using QuestMarkerTracking.Tracking;
using QuestMarkerTracking.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace QuestMarkerTracking.Mapping
{
    public class TrackerMapping : MonoBehaviour
    {
        private enum MappingMode
        {
            Proposal,
            Selection,
            Both
        }

        private enum ProposalMethod
        {
            RoundRobin,
            Random,
            LeastRecent,
            MostRecent,
            Gaze,
            Closest,
            All
        }

        [SerializeField] private MappingMode mappingMode = MappingMode.Proposal;
        [SerializeField] private ProposalMethod proposalMethod = ProposalMethod.RoundRobin;
        [SerializeField] private VirtualObjectManager virtualObjectManager;
        [SerializeField] private bool enableQuickMapping = true;
        [SerializeField] private float autoPairDistanceThreshold = 1f;
        [SerializeField] private float pairingDistanceThreshold = 0.1f;

        private int _currentTrackerIndex;
        private CameraVirtualObject _selectedCameraVirtualObject;
        private Tracker[] _trackers;
        private CameraVirtualObject[] _virtualObjects;

        private bool IsProposalMode => mappingMode == MappingMode.Proposal || mappingMode == MappingMode.Both;
        private bool IsSelectionMode => mappingMode == MappingMode.Selection || mappingMode == MappingMode.Both;

        private void Start()
        {
            _trackers = TrackerManager.Instance.Trackers.ToArray();
            _virtualObjects = virtualObjectManager.GetVirtualObjects.Select(virtualObject => virtualObject as CameraVirtualObject).ToArray();
            
            foreach (var tracker in _trackers)
            {
                tracker.onTrackerMoved.AddListener(OnTrackerMoved);
                tracker.onTopFaceChanged.AddListener(OnTopFaceChanged);
            }
        }

        private void OnTopFaceChanged(Tracker tracker)
        {
            if (IsSelectionMode)
            {
                if (tracker.IsPaired)
                {
                    tracker.Unpair();
                }
                else
                {
                    CameraVirtualObject closestCameraVirtualObject = null;
                    var closestDistance = float.MaxValue;
                    foreach (var virtualObject in _virtualObjects)
                    {
                        var distance = Vector2.Distance(virtualObject.transform.position.RemoveY(), tracker.transform.position.RemoveY());
                        
                        if (distance > pairingDistanceThreshold) continue;
                        if (distance > closestDistance) continue;
                        
                        closestDistance = distance;
                        closestCameraVirtualObject = virtualObject;
                    }

                    if (closestCameraVirtualObject)
                    {
                        tracker.Pair(closestCameraVirtualObject);
                    }
                }
            }
        }

        private void OnTrackerMoved(Tracker tracker)
        {
            if (IsProposalMode && tracker.IsProposed)// && tracker.IsDragged)
            {
                tracker.Pair(_selectedCameraVirtualObject);
            }
        }

        public void SelectVirtualObject(CameraVirtualObject cameraVirtualObject)
        {
            if (!IsProposalMode || cameraVirtualObject.IsPaired) return;

            _selectedCameraVirtualObject = cameraVirtualObject;
            _selectedCameraVirtualObject.Select();
            ProposeTracker(_selectedCameraVirtualObject);
            StudyLogger.Instance?.LogProposal();
        }

        public void QuickMap()
        {
            if (!enableQuickMapping)
            {
                Debug.LogWarning("Quick mapping is disabled.");
                return;
            }

            var trackerPositions = _trackers.Select(tracker => tracker.transform.position.RemoveY()).ToList();
            var virtualObjectPositions = _virtualObjects.Select(virtualObject => virtualObject.transform.position.RemoveY()).ToList();
            var (_, transformation, _) = MathUtils.Icp(trackerPositions, virtualObjectPositions, distanceThreshold: 5f, pointPairsThreshold: 0, verbose: true);

            foreach (var virtualObject in _virtualObjects)
            {
                var yPosition = virtualObject.transform.position.y;

                virtualObject.transform.position = virtualObject.transform.position.RemoveY()
                    .Transform(transformation).AddY(yPosition);

                virtualObject.Deselect();
            }

            foreach (var tracker in _trackers)
            {
                CameraVirtualObject closestCameraVirtualObject = null;
                var closestDistance = float.MaxValue;
                foreach (var virtualObject in _virtualObjects)
                {
                    var distance = Vector3.Distance(tracker.transform.position, virtualObject.transform.position);
                    if (distance > autoPairDistanceThreshold) continue;
                    if (distance > closestDistance) continue;

                    closestDistance = distance;
                    closestCameraVirtualObject = virtualObject;
                }

                if (closestCameraVirtualObject)
                {
                    Debug.Log("Found closest virtual object: " + closestCameraVirtualObject.name, this);
                    tracker.Pair(closestCameraVirtualObject, true);
                }
                else
                {
                    tracker.Unpair();
                }
            }
        }

        private void ProposeTracker(CameraVirtualObject cameraVirtualObject)
        {
            switch (proposalMethod)
            {
                case ProposalMethod.RoundRobin:
                    ProposeTrackerRoundRobin();
                    break;
                case ProposalMethod.Random:
                    ProposeTrackerRandom();
                    break;
                case ProposalMethod.LeastRecent:
                    ProposeTrackerLeastRecent();
                    break;
                case ProposalMethod.MostRecent:
                    ProposeTrackerMostRecent();
                    break;
                case ProposalMethod.Gaze:
                    ProposeTrackersGaze();
                    break;
                case ProposalMethod.All:
                    ProposeTrackersAll();
                    break;
                case ProposalMethod.Closest:
                    ProposeTrackerClosest(cameraVirtualObject);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ProposeTrackerClosest(CameraVirtualObject cameraVirtualObject)
        {
            _trackers
                .OrderBy(target =>
                    Vector3.Distance(target.transform.position, cameraVirtualObject.transform.position))
                .FirstOrDefault()?
                .Propose();
        }

        private void ProposeTrackersAll()
        {
            foreach (var tracker in _trackers)
            {
                tracker.Propose(false);
            }
        }

        private void ProposeTrackersGaze()
        {
            var visibleTrackers = _trackers.Where(tracker =>
            {
                var screenPoint = (Vector2)TrackerManager.Instance.GetGazeCamera().WorldToScreenPoint(tracker.transform.position);

                return screenPoint.x >= 0 && screenPoint.x <= Screen.width &&
                       screenPoint.y >= 0 && screenPoint.y <= Screen.height;
            }).ToList();
            visibleTrackers.ForEach(tracker => tracker.Propose(false));

            var invisibleTrackers = _trackers.Except(visibleTrackers).ToList();
            invisibleTrackers.ForEach(tracker => tracker.Unpropose());
        }

        private void ProposeTrackerMostRecent()
        {
            _trackers.OrderByDescending(tracker => tracker.LastUsedTime).FirstOrDefault()?.Propose();
        }

        private void ProposeTrackerLeastRecent()
        {
            _trackers.OrderBy(tracker => tracker.LastUsedTime).FirstOrDefault()?.Propose();
        }

        private void ProposeTrackerRandom()
        {
            _trackers[Random.Range(0, _trackers.Length)].Propose();
        }

        private void ProposeTrackerRoundRobin()
        {
            if (_trackers.Length == 0)
            {
                Debug.LogError("No trackers available for round-robin selection.", this);
                return;
            }

            _trackers[_currentTrackerIndex++].Propose();
            _currentTrackerIndex %= _trackers.Length;
        }
    }
}