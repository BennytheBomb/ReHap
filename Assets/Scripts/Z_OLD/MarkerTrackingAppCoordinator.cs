using System;
using System.Collections;
using System.Collections.Generic;
using PassthroughCameraSamples;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;
using Uralstech.UXR.QuestCamera;

namespace QuestMarkerTracking.Z_OLD
{
    public class MarkerTrackingAppCoordinator : MonoBehaviour
    {
        [Header("Camera Texture View")]
        [SerializeField] private Transform m_cameraAnchor;

        [SerializeField] private Canvas m_cameraCanvas;
        [SerializeField] private RawImage m_resultRawImage;
        [SerializeField] private float m_canvasDistance = 1f;

        [Header("Marker Tracking")]
        [SerializeField] private ArUcoMarkerTracking m_arucoMarkerTracking;

        [SerializeField] [Tooltip("List of marker IDs mapped to their corresponding GameObjects")]
        private List<MarkerGameObjectPair> m_markerGameObjectPairs = new List<MarkerGameObjectPair>();

        private readonly Dictionary<int, GameObject> m_markerGameObjectDictionary = new Dictionary<int, GameObject>();
        private bool _isDetecting;
        private bool m_showCameraCanvas = true;
        private CameraDevice _cameraDevice;
        private CameraInfo _cameraInfo;
        private CaptureSessionObject<ContinuousCaptureSession> _captureSession;
        private long _previousTimestampNs;
        private Resolution _resolution;

        private Texture2D m_resultTexture;

        /// <summary>
        ///     Initializes the camera, permissions, and marker tracking system.
        /// </summary>
        private IEnumerator Start()
        {
            // Check if the camera permission has been given.
            if (Permission.HasUserAuthorizedPermission(UCameraManager.HeadsetCameraPermission))
            {
                // Get the left eye camera.
                _cameraInfo = UCameraManager.Instance.GetCamera(CameraInfo.CameraEye.Left);
                Debug.Log($"Got camera info: {_cameraInfo}");
            }
            else
            {
                // Callback to set _cameraInfo when the permission is granted.
                PermissionCallbacks callbacks = new();
                callbacks.PermissionGranted += _ =>
                {
                    _cameraInfo = UCameraManager.Instance.GetCamera(CameraInfo.CameraEye.Left);
                    Debug.Log($"Got new camera info after camera permission was granted: {_cameraInfo}");
                };

                // Request the permission and set the flag to true.
                Permission.RequestUserPermission(UCameraManager.HeadsetCameraPermission, callbacks);
                Debug.Log("Camera permission requested.");
            }

            yield return new WaitUntil(() => Permission.HasUserAuthorizedPermission(UCameraManager.HeadsetCameraPermission));

            // Initialize camera
            yield return StartCamera();

            // Configure UI and tracking components
            ScaleCameraCanvas();

            //======================================================================================
            // CORE SETUP: Initialize the marker tracking system with camera parameters
            // This configures the ArUco detection with proper camera calibration values
            // and prepares the marker-to-GameObject mapping dictionary
            //======================================================================================
            InitializeMarkerTracking();

            // Set initial visibility states
            m_cameraCanvas.gameObject.SetActive(m_showCameraCanvas);
            SetMarkerObjectsVisibility(!m_showCameraCanvas);
        }

        private void Update()
        {
            if (_captureSession != null && _captureSession.CaptureSession.IsActiveAndUsable && !m_arucoMarkerTracking.IsReady)
                return;

            HandleVisualizationToggle();

            // Update tracking and visualization
            UpdateCameraPoses();
        }

        private IEnumerator StartCamera()
        {
            // Check if _cameraInfo is null.
            if (_cameraInfo == null)
            {
                // if null, log an error, as the camera permission was not given.
                Debug.LogError("Camera permission was not given.");
                yield break;
            }

            // If already open, return.
            if (_cameraDevice != null || _captureSession != null)
            {
                Debug.Log("Camera or capture session is already open.");
                yield break;
            }

            // Open the camera.
            _cameraDevice = UCameraManager.Instance.OpenCamera(_cameraInfo);

            // Wait for initialization and check its state.
            yield return _cameraDevice.WaitForInitialization();
            if (_cameraDevice.CurrentState != NativeWrapperState.Opened)
            {
                Debug.LogError("Could not open camera!");

                // Very important, this frees up any resources held by the camera.
                _cameraDevice.Destroy();
                _cameraDevice = null;
                yield break;
            }

            Debug.Log("Camera opened.");

            // Open the capture session.
            _resolution = _cameraInfo.SupportedResolutions[^1];
            _captureSession = _cameraDevice.CreateContinuousCaptureSession(_resolution);

            // Wait for initialization and check its state.
            yield return _captureSession.CaptureSession.WaitForInitialization();
            if (_captureSession.CaptureSession.CurrentState != NativeWrapperState.Opened)
            {
                Debug.LogError("Could not open camera session!");

                // Both of these are important for releasing the camera and session resources.
                _captureSession.Destroy();
                _cameraDevice.Destroy();

                (_cameraDevice, _captureSession) = (null, null);
                yield break;
            }

            // Set _cameraPreview to the texture.
            m_resultRawImage.texture = _captureSession.TextureConverter.FrameRenderTexture;

            // Set a callback for when each frame is ready for the AI.
            _captureSession.TextureConverter.OnFrameProcessedWithTimestamp.AddListener(OnFrameReady);
            Debug.Log("Capture session opened.");
        }

        private void OnFrameReady(RenderTexture renderTexture, long timestampNs)
        {
            if (_isDetecting) // Skip if already processing a frame
            {
                Debug.LogWarning($"[{nameof(MarkerTrackingAppCoordinator)}] Frame is already being processed.");
                return;
            }

            var deltaTime = (timestampNs - _previousTimestampNs) / 1e-9f;
            ProcessMarkerTracking(renderTexture, deltaTime);
            _previousTimestampNs = timestampNs;
        }

        /// <summary>
        ///     Handles button input to toggle between camera view and AR visualization.
        /// </summary>
        private void HandleVisualizationToggle()
        {
            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                m_showCameraCanvas = !m_showCameraCanvas;
                m_cameraCanvas.gameObject.SetActive(m_showCameraCanvas);
                SetMarkerObjectsVisibility(!m_showCameraCanvas);
            }
        }

        /// <summary>
        ///     Performs marker detection and pose estimation.
        ///     This is the core functionality that processes camera frames to detect markers
        ///     and position virtual objects in 3D space.
        /// </summary>
        private async void ProcessMarkerTracking(RenderTexture renderTexture, float deltaTime)
        {
            _isDetecting = true;

            await Awaitable.BackgroundThreadAsync();

            // Step 1: Detect ArUco markers in the current camera frame
            await m_arucoMarkerTracking.DetectMarker(renderTexture, m_resultTexture, m_showCameraCanvas);

            // Step 2: Estimate the pose of markers and position 3D objects accordingly
            // This maps the 2D marker positions to 3D space using the camera parameters
            await m_arucoMarkerTracking.EstimatePoseGridBoard(m_markerGameObjectDictionary, m_cameraAnchor, deltaTime);
            // m_arucoMarkerTracking.EstimatePoseCanonicalMarker(m_markerGameObjectDictionary, m_cameraAnchor);

            await Awaitable.MainThreadAsync();

            _isDetecting = false;
        }

        /// <summary>
        ///     Toggles the visibility of all marker-associated GameObjects in the dictionary.
        /// </summary>
        /// <param name="isVisible">Whether the marker objects should be visible or not.</param>
        private void SetMarkerObjectsVisibility(bool isVisible)
        {
            // Toggle visibility for all GameObjects in the marker dictionary
            foreach (var markerObject in m_markerGameObjectDictionary.Values)
            {
                if (markerObject != null)
                {
                    var rendererList = markerObject.GetComponentsInChildren<Renderer>(true);
                    foreach (var meshRenderer in rendererList)
                    {
                        meshRenderer.enabled = isVisible;
                    }
                }
            }
        }

        /// <summary>
        ///     Initializes the marker tracking system with camera parameters and builds the marker dictionary.
        ///     This method configures the ArUco marker detection system with the correct camera parameters
        ///     for accurate pose estimation.
        /// </summary>
        private void InitializeMarkerTracking()
        {
            // Step 1: Set up camera parameters for tracking
            // These intrinsic parameters are essential for accurate marker pose estimation
            var intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(PassthroughCameraEye.Left);
            var cx = intrinsics.PrincipalPoint.x; // Principal point X (optical center)
            var cy = intrinsics.PrincipalPoint.y; // Principal point Y (optical center)
            var fx = intrinsics.FocalLength.x; // Focal length X
            var fy = intrinsics.FocalLength.y; // Focal length Y
            var width = intrinsics.Resolution.x; // Image width
            var height = intrinsics.Resolution.y; // Image height

            // Initialize the ArUco tracking with camera parameters
            m_arucoMarkerTracking.Initialize(width, height, cx, cy, fx, fy);

            // Step 2: Build marker dictionary from serialized list
            // This maps marker IDs to the GameObjects that should be positioned at each marker
            BuildMarkerDictionary();

            // Step 3: Set up texture for visualization
            ConfigureResultTexture(width, height);
        }

        /// <summary>
        ///     Builds the dictionary mapping marker IDs to GameObjects.
        /// </summary>
        private void BuildMarkerDictionary()
        {
            m_markerGameObjectDictionary.Clear();
            foreach (var pair in m_markerGameObjectPairs)
            {
                if (pair.gameObject != null)
                {
                    m_markerGameObjectDictionary[pair.markerId] = pair.gameObject;
                }
            }
        }

        /// <summary>
        ///     Configures the texture for displaying camera and tracking results.
        /// </summary>
        /// <param name="width">Width of the camera resolution</param>
        /// <param name="height">Height of the camera resolution</param>
        private void ConfigureResultTexture(int width, int height)
        {
            float divideNumber = m_arucoMarkerTracking.DivideNumber;
            m_resultTexture = new Texture2D(Mathf.FloorToInt(width / divideNumber), Mathf.FloorToInt(height / divideNumber), TextureFormat.RGB24, false);
            m_resultRawImage.texture = m_resultTexture;
        }

        /// <summary>
        ///     Calculates the dimensions of the canvas based on the distance from the camera origin and the camera resolution.
        /// </summary>
        private void ScaleCameraCanvas()
        {
            var cameraCanvasRectTransform = m_cameraCanvas.GetComponentInChildren<RectTransform>();

            // Calculate field of view based on camera parameters
            var leftSidePointInCamera = PassthroughCameraUtils.ScreenPointToRayInCamera(PassthroughCameraEye.Left, new Vector2Int(0, _resolution.width / 2));
            var rightSidePointInCamera = PassthroughCameraUtils.ScreenPointToRayInCamera(PassthroughCameraEye.Left, new Vector2Int(_resolution.width, _resolution.height / 2));
            var horizontalFoVDegrees = Vector3.Angle(leftSidePointInCamera.direction, rightSidePointInCamera.direction);
            var horizontalFoVRadians = horizontalFoVDegrees / 180 * Math.PI;

            // Calculate canvas size to match camera view
            var newCanvasWidthInMeters = 2 * m_canvasDistance * Math.Tan(horizontalFoVRadians / 2);
            var localScale = (float)(newCanvasWidthInMeters / cameraCanvasRectTransform.sizeDelta.x);
            cameraCanvasRectTransform.localScale = new Vector3(localScale, localScale, localScale);
        }

        /// <summary>
        ///     Updates the positions and rotations of camera-related transforms based on head and camera poses.
        /// </summary>
        private void UpdateCameraPoses()
        {
            // Get current head pose
            var headPose = OVRPlugin.GetNodePoseStateImmediate(OVRPlugin.Node.Head).Pose.ToOVRPose();

            // Update camera anchor position and rotation
            var cameraPose = PassthroughCameraUtils.GetCameraPoseInWorld(PassthroughCameraEye.Left);
            m_cameraAnchor.position = cameraPose.position;
            m_cameraAnchor.rotation = cameraPose.rotation;

            // Position the canvas in front of the camera
            m_cameraCanvas.transform.position = cameraPose.position + cameraPose.rotation * Vector3.forward * m_canvasDistance;
            m_cameraCanvas.transform.rotation = cameraPose.rotation;
        }

        private void StopCamera()
        {
            if (_captureSession != null)
            {
                // Destroy the session to release native resources.
                _captureSession.Destroy();
                _captureSession = null;
            }

            if (_cameraDevice != null)
            {
                // Destroy the camera to release native resources.
                _cameraDevice.Destroy();
                _cameraDevice = null;
            }
        }

        /// <summary>
        ///     Serializable class for mapping marker IDs to GameObjects in the Inspector.
        /// </summary>
        [Serializable]
        public class MarkerGameObjectPair
        {
            /// <summary>
            ///     The unique ID of the AR marker to track.
            /// </summary>
            public int markerId;

            /// <summary>
            ///     The GameObject to associate with this marker.
            /// </summary>
            public GameObject gameObject;
        }
    }
}