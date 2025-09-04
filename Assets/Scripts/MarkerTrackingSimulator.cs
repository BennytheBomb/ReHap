using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using OpenCVForUnity.UnityUtils;
using QuestMarkerTracking.Tracking;
using QuestMarkerTracking.Tracking.Data;
using QuestMarkerTracking.Utilities;
using UnityEngine;

namespace QuestMarkerTracking
{
    [RequireComponent(typeof(TrackerSimulation))]
    public class MarkerTrackingSimulator : MonoBehaviour
    {
        private const string X_AXIS = "Mouse X";
        private const string Y_AXIS = "Mouse Y";

        [Header("Marker Settings")]
        [SerializeField] private List<Transform> markerTransforms;

        [Range(0.1f, 9f)] [SerializeField] private float sensitivity = 2f;

        [Tooltip("Limits vertical camera rotation. Prevents the flipping that happens when rotation goes above 90.")]
        [Range(0f, 90f)] [SerializeField] private float yRotationLimit = 88f;

        [SerializeField] private float frameUpdateIntervalSeconds = 0.033f;
        [SerializeField] private float positionNoise = 0.1f;
        [SerializeField] private float rotationNoise = 0.1f;
        [SerializeField] private float timestampNoise = 0.01f;

        private bool _isReady;
        private float _lastFrameUpdate;

        private GraphicsBuffer _graphicsBuffer;
        private long _previousTimestampNs;

        private TrackerSimulation _trackerSimulation;

        private PoseData _previousHeadPoseWithTimestamp;

        private Vector2 _rotation = Vector2.zero;

        private void Awake()
        {
            Application.targetFrameRate = 72;

            _trackerSimulation = GetComponent<TrackerSimulation>();
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _isReady = true;
        }

        private void Update()
        {
            UpdateCamera();

            if (Time.time > _lastFrameUpdate + frameUpdateIntervalSeconds)
            {
                var frameTimestampNs = SystemUtils.GetNanoTime();
                OnFrameReady(frameTimestampNs);

                _lastFrameUpdate = Time.time + Random.Range(-timestampNoise, timestampNoise);
            }
        }

        private void UpdateCamera()
        {
            _rotation.x += Input.GetAxis(X_AXIS) * sensitivity;
            _rotation.y += Input.GetAxis(Y_AXIS) * sensitivity;
            _rotation.y = Mathf.Clamp(_rotation.y, -yRotationLimit, yRotationLimit);
            var xQuat = Quaternion.AngleAxis(_rotation.x, Vector3.up);
            var yQuat = Quaternion.AngleAxis(_rotation.y, Vector3.left);

            transform.localRotation = xQuat * yQuat;
        }

        private void OnFrameReady(long frameTimestampNs)
        {
            ProcessMarkerTracking(frameTimestampNs).Forget();
        }

        private async UniTask ProcessMarkerTracking(long frameTimestampNs)
        {
            await DetectMarkersAsync(frameTimestampNs);
        }

        private async UniTask DetectMarkersAsync(long frameTimestampNs)
        {
            if (!_isReady) return;

            var trackedMarkers = markerTransforms.Select((markerTransform, index) =>
            {
                var pos = markerTransform.position + new Vector3(Random.Range(-positionNoise, positionNoise),
                    Random.Range(-positionNoise, positionNoise), Random.Range(-positionNoise, positionNoise));

                var rot = markerTransform.rotation * Quaternion.Euler(Random.Range(-rotationNoise, rotationNoise),
                    Random.Range(-rotationNoise, rotationNoise),
                    Random.Range(-rotationNoise, rotationNoise));

                return new TrackedMarker
                {
                    Id = index,
                    MarkerPoseData = new PoseData
                    {
                        pos = pos,
                        rot = rot
                    }
                };
            }).ToList();

            var randomFrameProcessingDelay = Random.Range(10, 20);

            await UniTask.SwitchToThreadPool();

            await UniTask.Delay(randomFrameProcessingDelay);

            _trackerSimulation.EnqueueFrameTrackerData(frameTimestampNs,
                trackedMarkers);
        }
    }
}