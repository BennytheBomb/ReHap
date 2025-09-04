using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;
using Uralstech.UXR.QuestCamera;

namespace QuestMarkerTracking.Z_OLD
{
    /// <summary>
    ///     Does digit recognition using the Meta Quest Passthrough Camera API.
    /// </summary>
    public class CameraTest : MonoBehaviour
    {
        [Tooltip("The text to output results in.")]
        [SerializeField] private Text _outputText;

        [Tooltip("Preview to show the camera feed.")]
        [SerializeField] private RawImage _cameraPreview;

        private CameraDevice _cameraDevice; // Camera device.
        private CameraInfo _cameraInfo; // Camera metadata.

        private CaptureSessionObject<ContinuousCaptureSession> _captureSession; // Camera capture session data.
        // private SurfaceTextureCaptureSession _captureSession; // Camera capture session data.

        // Start is called on the frame when a script is enabled for the first time.
        protected async void Start()
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

            await Task.Delay(1000);

            StartCamera();
        }

        // Destroying the attached Behaviour will result in the game or Scene receiving OnDestroy.
        protected void OnDestroy()
        {
            // Stop the camera and release the model worker and input tensors when the GameObject is destroyed.

            StopCamera();
        }

        /// <summary>
        ///     Starts the camera.
        /// </summary>
        private async void StartCamera()
        {
            try
            {
                // Check if _cameraInfo is null.
                if (_cameraInfo == null)
                {
                    // if null, log an error, as the camera permission was not given.
                    Debug.LogError("Camera permission was not given.");
                    return;
                }

                // If already open, return.
                if (_cameraDevice != null || _captureSession != null)
                {
                    Debug.Log("Camera or capture session is already open.");
                    return;
                }

                // Open the camera.
                _cameraDevice = UCameraManager.Instance.OpenCamera(_cameraInfo);

                // Wait for initialization and check its state.
                NativeWrapperState state = await _cameraDevice.WaitForInitializationAsync();
                if (state != NativeWrapperState.Opened)
                {
                    Debug.LogError("Failed to open camera.");

                    // Destroy the camera to release native resources.
                    _cameraDevice.Destroy();
                    _cameraDevice = null;
                    return;
                }

                Debug.Log("Camera opened.");

                // Open the capture session.
                // _captureSession = _cameraDevice.CreateSurfaceTextureCaptureSession(_cameraInfo.SupportedResolutions[^1]);
                _captureSession = _cameraDevice.CreateContinuousCaptureSession(_cameraInfo.SupportedResolutions[^1]);

                // Wait for initialization and check its state.
                state = await _captureSession.CaptureSession.WaitForInitializationAsync();
                if (state != NativeWrapperState.Opened)
                {
                    Debug.LogError("Failed to open capture session.");

                    // Destroy the camera AND capture session to release native resources.
                    _captureSession.Destroy();
                    _cameraDevice.Destroy();

                    (_cameraDevice, _captureSession) = (null, null);
                    return;
                }

                // Set _cameraPreview to the texture.
                _cameraPreview.texture = _captureSession.TextureConverter.FrameRenderTexture;

                // Set a callback for when each frame is ready for the AI.
                // _captureSession.OnFrameProcessed.AddListener(OnFrameReady);
                Debug.Log("Capture session opened.");
            }
            catch (Exception e)
            {
                Debug.Log("Error while starting camera: " + e.Message);
            }
        }

        /// <summary>
        ///     Stops the camera.
        /// </summary>
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
    }
}