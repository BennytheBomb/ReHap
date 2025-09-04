using UnityEngine;

namespace QuestMarkerTracking.Mapping
{
    public class DesktopTrackerMapping : MonoBehaviour
    {
        private const float MAX_DISTANCE = 100f;

        [SerializeField] private LayerMask hitLayers;
        [SerializeField] private LayerMask floorLayer;
        [SerializeField] private TrackerMapping trackerMapping;

        private Camera _camera;
        private Tracker _selectedTracker;

        private void Start()
        {
            _camera = Camera.main;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var ray = _camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, MAX_DISTANCE, hitLayers))
                {
                    if (hit.transform.TryGetComponent(out Tracker tracker))
                    {
                        _selectedTracker = tracker;
                    }
                    else if (hit.transform.TryGetComponent(out CameraVirtualObject virtualObject))
                    {
                        trackerMapping.SelectVirtualObject(virtualObject);
                    }
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (_selectedTracker)
                {
                    _selectedTracker = null;
                }
            }

            if (_selectedTracker)
            {
                var ray = _camera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, MAX_DISTANCE, floorLayer))
                {
                    _selectedTracker.transform.position = hit.point;
                }
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                trackerMapping.QuickMap();
            }

            if (Input.GetKeyDown(KeyCode.LeftShift) && _selectedTracker)
            {
                _selectedTracker.RotateNext();
            }
        }
    }
}