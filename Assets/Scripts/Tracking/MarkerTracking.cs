using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Meta.XR.ImmersiveDebugger;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using PassthroughCameraSamples;
using QuestMarkerTracking.Tracking.Data;
using QuestMarkerTracking.Utilities;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;
using Uralstech.UXR.QuestCamera;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace QuestMarkerTracking.Tracking
{
    [RequireComponent(typeof(TrackerSimulation))]
    public class MarkerTracking : MonoBehaviour
    {
        private enum ArUcoDictionary
        {
            DICT_4X4_50 = Objdetect.DICT_4X4_50,
            DICT_4X4_100 = Objdetect.DICT_4X4_100,
            DICT_4X4_250 = Objdetect.DICT_4X4_250,
            DICT_4X4_1000 = Objdetect.DICT_4X4_1000,
            DICT_5X5_50 = Objdetect.DICT_5X5_50,
            DICT_5X5_100 = Objdetect.DICT_5X5_100,
            DICT_5X5_250 = Objdetect.DICT_5X5_250,
            DICT_5X5_1000 = Objdetect.DICT_5X5_1000,
            DICT_6X6_50 = Objdetect.DICT_6X6_50,
            DICT_6X6_100 = Objdetect.DICT_6X6_100,
            DICT_6X6_250 = Objdetect.DICT_6X6_250,
            DICT_6X6_1000 = Objdetect.DICT_6X6_1000,
            DICT_7X7_50 = Objdetect.DICT_7X7_50,
            DICT_7X7_100 = Objdetect.DICT_7X7_100,
            DICT_7X7_250 = Objdetect.DICT_7X7_250,
            DICT_7X7_1000 = Objdetect.DICT_7X7_1000,
            DICT_ARUCO_ORIGINAL = Objdetect.DICT_ARUCO_ORIGINAL,
            DICT_APRILTAG_16h5 = Objdetect.DICT_APRILTAG_16h5,
            DICT_APRILTAG_25h9 = Objdetect.DICT_APRILTAG_25h9,
            DICT_APRILTAG_36h10 = Objdetect.DICT_APRILTAG_36h10,
            DICT_APRILTAG_36h11 = Objdetect.DICT_APRILTAG_36h11,
            DICT_ARUCO_MIP_36h12 = Objdetect.DICT_ARUCO_MIP_36h12
        }

        private enum CameraResolution
        {
            Low,
            Medium,
            High,
            VeryHigh
        }

        private enum MarkerArrangement
        {
            Single,
            GridBoard,
            Cube
        }

        private const int CUBE_SIDES = 6;
        private const string CATEGORY = nameof(MarkerTracking);
        private const long MAX_AGE_NS = 500_000_000; // Keep 0.5 seconds of data

        [Header("Image Tracking Visualisation")]
        [SerializeField] private RawImage resultRawImage;

        [Header("Tracking Settings")]
        [SerializeField] private CameraResolution cameraResolution = CameraResolution.High;

        [SerializeField] private ArUcoDictionary dictionaryId = ArUcoDictionary.DICT_6X6_50;

        [SerializeField] [Range(0.001f, 400f)] [DebugMember(Category = CATEGORY, Tweakable = true, Min = 0.001f, Max = 400f)]
        private float offsetCorrectionCoefficient = 180f;

        [SerializeField] [Range(0f, 0.1f)] [DebugMember(Category = CATEGORY, Tweakable = true, Min = 0f, Max = 0.1f)]
        private float maxOffsetCorrection = 0.05f;

        [SerializeField] [Range(0f, 1f)] [DebugMember(Category = CATEGORY, Tweakable = true, Min = 0f, Max = 1f)]
        private float weightingCoefficient = 0.5f;
        
        [DebugMember(Category = CATEGORY, Tweakable = true, Min = -180f, Max = 180f)] public float rotationXOffset;
        [DebugMember(Category = CATEGORY, Tweakable = true, Min = -180f, Max = 180f)] public float rotationYOffset;
        [DebugMember(Category = CATEGORY, Tweakable = true, Min = -180f, Max = 180f)] public float rotationZOffset;

        [Header("Marker Settings")]
        [SerializeField] private float markerLength = 0.035f;
        [SerializeField] private MarkerArrangement markerArrangement = MarkerArrangement.GridBoard;

        [Header("Grid Board Settings")]
        [SerializeField] private int gridBoardRows = 3;
        [SerializeField] private int gridBoardColumns = 2;
        [SerializeField] private float markerSeparation = 0.004375f;

        [Header("Debug Settings")]
        [SerializeField] private OVRInput.Button visualisationToggleButton = OVRInput.Button.One;

        [SerializeField] private OVRInput.Controller controllerMask = OVRInput.Controller.Touch;
        [SerializeField] private bool showCameraFeed = true;

        private Vector3 MarkerOffset => new(
            (markerLength * gridBoardColumns + markerSeparation * (gridBoardColumns - 1)) / 2f,
            0f,
            -(markerLength * gridBoardRows + markerSeparation * (gridBoardRows - 1)) / 2f);

        private Quaternion TrackerRotationOffset => Quaternion.Euler(rotationXOffset, rotationYOffset, rotationZOffset);

        private readonly LinkedList<FrameHeadPoseData> _poseHistory = new();
        private readonly List<GridBoard> _gridBoards = new();
        private ArucoDetector _arucoDetector;
        private bool _isReady;
        private CameraDevice _cameraDevice;

        private CameraInfo _cameraInfo;
        private CaptureSessionObject<ContinuousCaptureSession> _captureSession;

        private DetectorParameters _detectorParams;
        private Dictionary _markerDictionary;
        private int _currentDict;

        private TrackerSimulation _trackerSimulation;
        private Mat _cameraIntrinsicMatrix;
        private Mat _halfSizeMat;
        private Mat _originalFrameMat;
        private Mat _processingRgbMat;
        private Mat _recoveredMarkerIndices;

        private MatOfDouble _cameraDistortionCoeffs;

        private PoseData _previousHeadPoseWithTimestamp;
        private Resolution _resolution;
        private Texture2D _resultTexture;

        private void Awake()
        {
            _trackerSimulation = GetComponent<TrackerSimulation>();
        }

        private async void Start()
        {
            try
            {
                if (Permission.HasUserAuthorizedPermission(UCameraManager.HeadsetCameraPermission))
                {
                    if (UCameraManager.Instance.Cameras == null)
                    {
                        Debug.LogError("Can't access device camera.");
                        return;
                    }

                    _cameraInfo = UCameraManager.Instance.GetCamera(CameraInfo.CameraEye.Left);
                    Debug.Log($"Got camera info: {_cameraInfo}");
                }
                else
                {
                    PermissionCallbacks callbacks = new();
                    callbacks.PermissionGranted += _ =>
                    {
                        _cameraInfo = UCameraManager.Instance.GetCamera(CameraInfo.CameraEye.Left);
                        Debug.Log($"Got new camera info after camera permission was granted: {_cameraInfo}");
                    };

                    Permission.RequestUserPermission(UCameraManager.HeadsetCameraPermission, callbacks);
                    Debug.Log("Camera permission requested.");
                }

                await UniTask.WaitUntil(() => Permission.HasUserAuthorizedPermission(UCameraManager.HeadsetCameraPermission));

                await StartCamera();

                InitializeMarkerTracking();

                resultRawImage.gameObject.SetActive(showCameraFeed);
                _trackerSimulation.SetMarkerObjectsVisibility(!showCameraFeed);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void Update()
        {
            if (!_cameraDevice || _captureSession == null || !_captureSession.CaptureSession.IsActiveAndUsable || !_isReady)
                return;

            HandleVisualizationToggle();
        }

        private void OnDestroy()
        {
            StopCamera();
            ReleaseResources();
        }

        private Task OnFrameReady(IntPtr yBuffer, IntPtr uBuffer, IntPtr vBuffer, int ySize, int uSize, int vSize, int yRowStride, int uvRowStride, int uvPixelStride, long timestampNs)
        {
            var grayscaleMat = ConvertYBufferToMat(yBuffer, ySize, _resolution.width, _resolution.height, yRowStride);

            ProcessMarkerTracking(grayscaleMat, timestampNs);
            // ProcessMarkerTracking(grayscaleMat, timestampNs).Forget();

            return Task.CompletedTask; // Callback is being awaited, need to return Task to not block the thread
        }

        private static Mat ConvertYBufferToMat(IntPtr yBuffer, int ySize, int width, int height, int yRowStride)
        {
            if (ySize < width * height)
            {
                Debug.LogWarning("Y buffer size is smaller than expected. Skipping frame.");
                return null;
            }

            if (yRowStride == width)
            {
                return new Mat(height, width, CvType.CV_8UC1, yBuffer);
            }

            using var fullImage = new Mat(height, yRowStride, CvType.CV_8UC1, yBuffer);
            using var cropped = new Mat(fullImage, new Rect(0, 0, width, height));
            return cropped.clone(); // Detach from shared buffer
        }

        private async UniTask ProcessMarkerTracking(Mat mat, long timestampNs)
        {
            await DetectMarkersAsync(mat, timestampNs);
        }

        private void AddPose(FrameHeadPoseData pose)
        {
            _poseHistory.AddLast(pose);

            // Remove old poses
            while (_poseHistory.First != null && pose.FrameTimestampNs - _poseHistory.First.Value.FrameTimestampNs > MAX_AGE_NS)
            {
                _poseHistory.RemoveFirst();
            }
        }

        private bool TryGetInterpolatedPose(long targetTimestampNs, out PoseData interpolated)
        {
            LinkedListNode<FrameHeadPoseData> before = null;
            LinkedListNode<FrameHeadPoseData> after = null;

            for (var node = _poseHistory.First; node?.Next != null; node = node.Next)
            {
                if (node.Value.FrameTimestampNs <= targetTimestampNs && node.Next.Value.FrameTimestampNs >= targetTimestampNs)
                {
                    before = node;
                    after = node.Next;
                    break;
                }
            }

            if (before == null || after == null)
            {
                interpolated = default;
                return false;
            }

            // Interpolation factor
            var t = (targetTimestampNs - before.Value.FrameTimestampNs) /
                    (float)(after.Value.FrameTimestampNs - before.Value.FrameTimestampNs);

            var interpPos = Vector3.Lerp(before.Value.HeadPoseData.pos, after.Value.HeadPoseData.pos, t);
            var interpRot = Quaternion.Slerp(before.Value.HeadPoseData.rot, after.Value.HeadPoseData.rot, t);

            interpolated = new PoseData
            {
                pos = interpPos,
                rot = interpRot
            };
            return true;
        }

        private static Quaternion CorrectSideRotation(int sideIndex) => sideIndex switch
        {
            0 => Quaternion.identity,
            1 => Quaternion.Euler(0, 0, -90f),
            2 => Quaternion.Euler(-90f, 0, 0),
            3 => Quaternion.Euler(0, 0, 90f),
            4 => Quaternion.Euler(90f, 0, 0f),
            5 => Quaternion.Euler(180f, 0, 0),
            _ => throw new ArgumentException("Cube side index is out of range.")
        };
        
        private async UniTask DetectMarkersAsync(Mat processingMat, long frameTimestampNs)
        {
            if (!_isReady) return;

            // Important to call this in the main thread
            var currentTimestampNs = SystemUtils.GetNanoTime(); // Android Request, time takes longer to retrieve so get it before the pose
            var headPose = PassthroughCameraUtils.GetCameraPoseInWorld(PassthroughCameraEye.Left); // Android request
            var headPoseData = new PoseData
            {
                pos = headPose.position,
                rot = headPose.rotation
            };

            var currentHeadPoseData = new FrameHeadPoseData
            {
                HeadPoseData = headPoseData,
                FrameTimestampNs = currentTimestampNs
            };

            AddPose(currentHeadPoseData);
            var interpolatedHeadPoseData = TryGetInterpolatedPose(frameTimestampNs, out var interpolated)
                ? interpolated
                : headPoseData;

            var headPoseMatrix = ARUtils.ConvertPoseDataToMatrix(ref interpolatedHeadPoseData);

            // Do the heavy lifting on another background thread for a smooth 72 fps
            await UniTask.SwitchToThreadPool();

            using var detectedMarkerIds = new Mat();
            detectedMarkerIds.create(0, 1, CvType.CV_32S);

            var detectedMarkerCorners = new List<Mat>();
            var rejectedMarkerCandidates = new List<Mat>();

            _arucoDetector.detectMarkers(processingMat, detectedMarkerCorners, detectedMarkerIds, rejectedMarkerCandidates);

            if (showCameraFeed)
            {
                await UniTask.SwitchToMainThread();

                var processingRgbMat = _processingRgbMat.clone();
                Imgproc.cvtColor(processingMat, processingRgbMat, Imgproc.COLOR_GRAY2RGB);

                if (detectedMarkerCorners.Count == detectedMarkerIds.total() || detectedMarkerIds.total() == 0)
                {
                    Objdetect.drawDetectedMarkers(processingRgbMat, detectedMarkerCorners, detectedMarkerIds,
                        new Scalar(0, 255, 0));
                }

                // Update result texture for visualization
                if (_resultTexture)
                {
                    Utils.matToTexture2D(processingRgbMat, _resultTexture);
                }
            }
            else
            {
                var trackedMarkers = new List<TrackedMarker>();

                if (markerArrangement == MarkerArrangement.Single)
                {
                    using var objectPoints = new MatOfPoint3f(new Point3(-markerLength / 2f, markerLength / 2f, 0),
                        new Point3(markerLength / 2f, markerLength / 2f, 0),
                        new Point3(markerLength / 2f, -markerLength / 2f, 0),
                        new Point3(-markerLength / 2f, -markerLength / 2f, 0));
                    
                    for (var i = 0; i < detectedMarkerCorners.Count; i++)
                    {
                        var currentMarkerId = (int)detectedMarkerIds.get(i, 0)[0];

                        if (!_trackerSimulation.HasTargetForMarkerId(currentMarkerId))
                            continue;

                        using var rotationVec = new Mat(1, 1, CvType.CV_64FC3);
                        using var translationVec = new Mat(1, 1, CvType.CV_64FC3);
                        using var corner_4x1 = detectedMarkerCorners[i].reshape(2, 4);
                        using var imagePoints = new MatOfPoint2f(corner_4x1);
                        Calib3d.solvePnP(objectPoints, imagePoints, _cameraIntrinsicMatrix, _cameraDistortionCoeffs,
                            rotationVec, translationVec);
                        
                        var rvecArr = new double[3];
                        rotationVec.get(0, 0, rvecArr);
                        var tvecArr = new double[3];
                        translationVec.get(0, 0, tvecArr);
                        
                        var markerPoseData = ARUtils.ConvertRvecTvecToPoseData(rvecArr, tvecArr); // OpenCV right-handed coordinate system
                        var localSpaceMarkerMatrix = ARUtils.ConvertPoseDataToMatrix(ref markerPoseData, true);
                        var localSpaceMarkerPoseData = ARUtils.ConvertMatrixToPoseData(ref localSpaceMarkerMatrix); // Unity left-handed coordinate system

                        // Correct z-offset of marker to camera based on distance due to perspective projection
                        var z = localSpaceMarkerPoseData.pos.z;
                        if (!Mathf.Approximately(z, 0f) && !Mathf.Approximately(offsetCorrectionCoefficient, 0f))
                        {
                            localSpaceMarkerPoseData.pos.z -= Mathf.Clamp(1f / (offsetCorrectionCoefficient * z), 0f, maxOffsetCorrection);
                        }

                        var worldSpaceMarkerPoseData = new PoseData
                        {
                            pos = headPoseMatrix.MultiplyPoint(localSpaceMarkerPoseData.pos),
                            rot = interpolatedHeadPoseData.rot * localSpaceMarkerPoseData.rot
                        };

                        // var correctedRotation = worldSpaceMarkerPoseData.rot * TrackerRotationOffset;
                        // var centeredMarkerPose = new PoseData
                        // {
                        //     pos = worldSpaceMarkerPoseData.pos + correctedRotation * MarkerOffset,
                        //     rot = correctedRotation
                        // };

                        var accuracy = CalculateTrackerAccuracy(worldSpaceMarkerPoseData, interpolatedHeadPoseData);

                        trackedMarkers.Add(new TrackedMarker
                        {
                            Id = i,
                            MarkerPoseData = worldSpaceMarkerPoseData,
                            IsHighConfidence = accuracy > 0.9f,
                            Accuracy = accuracy
                        });
                    }
                }
                else
                {
                    var cubeMarkerDict = new Dictionary<int, List<CubeMarkerData>>();

                    if (detectedMarkerCorners.Count > 0)
                    {
                        for (var i = 0; i < _gridBoards.Count; i++)
                        {
                            var gridBoard = _gridBoards[i];
                            
                            var markerId = markerArrangement == MarkerArrangement.Cube
                                ? i / CUBE_SIDES
                                : i;

                            if (!_trackerSimulation.HasTargetForMarkerId(markerId))
                                continue;

                            using var rvec = new Mat(1, 1, CvType.CV_64FC3);
                            using var tvec = new Mat(1, 1, CvType.CV_64FC3);
                            using var gridBoardObjectPoints = new Mat();
                            using var imagePoints = new Mat();

                            gridBoard.matchImagePoints(detectedMarkerCorners, detectedMarkerIds, gridBoardObjectPoints, imagePoints);

                            if (imagePoints.total() != gridBoardObjectPoints.total())
                                continue;

                            if (gridBoardObjectPoints.total() == 0) // 0 of the detected markers in board
                                continue;

                            using var objectPointsP3F = new MatOfPoint3f(gridBoardObjectPoints);
                            using var imagePointsP3F = new MatOfPoint2f(imagePoints);
                            Calib3d.solvePnP(objectPointsP3F, imagePointsP3F, _cameraIntrinsicMatrix, _cameraDistortionCoeffs,
                                rvec, tvec);

                            var markersOfBoardDetected = (int)gridBoardObjectPoints.total() / 4;
                            if (markersOfBoardDetected <= 0) continue;

                            var rvecArr = new double[3];
                            rvec.get(0, 0, rvecArr);
                            var tvecArr = new double[3];
                            tvec.get(0, 0, tvecArr);

                            var markerPoseData = ARUtils.ConvertRvecTvecToPoseData(rvecArr, tvecArr); // OpenCV right-handed coordinate system
                            var localSpaceMarkerMatrix = ARUtils.ConvertPoseDataToMatrix(ref markerPoseData, true);
                            var localSpaceMarkerPoseData = ARUtils.ConvertMatrixToPoseData(ref localSpaceMarkerMatrix); // Unity left-handed coordinate system

                            // Correct z-offset of marker to camera based on distance due to perspective projection
                            var z = localSpaceMarkerPoseData.pos.z;
                            if (!Mathf.Approximately(z, 0f) && !Mathf.Approximately(offsetCorrectionCoefficient, 0f))
                            {
                                localSpaceMarkerPoseData.pos.z -= Mathf.Clamp(1f / (offsetCorrectionCoefficient * z), 0f, maxOffsetCorrection);
                            }

                            var worldSpaceMarkerPoseData = new PoseData
                            {
                                pos = headPoseMatrix.MultiplyPoint(localSpaceMarkerPoseData.pos),
                                rot = interpolatedHeadPoseData.rot * localSpaceMarkerPoseData.rot
                            };

                            var correctedRotation = worldSpaceMarkerPoseData.rot * TrackerRotationOffset;
                            var centeredMarkerPose = new PoseData
                            {
                                pos = worldSpaceMarkerPoseData.pos + correctedRotation * MarkerOffset,
                                rot = correctedRotation
                            };
                            
                            var accuracy = markersOfBoardDetected / (float)(gridBoardRows * gridBoardColumns);

                            if (markerArrangement == MarkerArrangement.Cube)
                            {
                                var cubeId = i / CUBE_SIDES;
                                if (!cubeMarkerDict.ContainsKey(cubeId))
                                {
                                    cubeMarkerDict[cubeId] = new List<CubeMarkerData>();
                                }
                                
                                cubeMarkerDict[cubeId].Add(new CubeMarkerData
                                {
                                    SideIndex = i % CUBE_SIDES,
                                    MarkerPoseData = centeredMarkerPose,
                                    Accuracy = accuracy,
                                });
                            }
                            else
                            {
                                trackedMarkers.Add(new TrackedMarker
                                {
                                    Id = i,
                                    MarkerPoseData = centeredMarkerPose,
                                    IsHighConfidence = markersOfBoardDetected >= gridBoardRows * gridBoardColumns,
                                    Accuracy = accuracy
                                });
                            }
                        }

                        if (markerArrangement == MarkerArrangement.Cube)
                        {
                            foreach (var (cubeId, cubeMarkers) in cubeMarkerDict)
                            {
                                var centerPositions = cubeMarkers.Select(marker =>
                                {
                                    var inwardsDirection = (marker.MarkerPoseData.rot * Vector3.down).normalized;
                                    return marker.MarkerPoseData.pos + inwardsDirection * (markerLength + 1.5f * markerSeparation); // TODO: generalize for gridboard
                                }).ToList();
                                
                                var centerRotations = cubeMarkers.Select(marker => marker.MarkerPoseData.rot * CorrectSideRotation(marker.SideIndex)).ToList();
                                
                                // var markerAccuracies = cubeMarkers.Select(marker =>
                                //     CalculateTrackerAccuracy(marker.MarkerPoseData, interpolatedHeadPoseData)).ToList();
                                
                                var markerAccuracies = cubeMarkers.Select(marker => marker.Accuracy).ToList();

                                var averageMarkerAccuracy = markerAccuracies.Average();

                                var centerPosition = TrackerSettings.Instance.useWeightedAverage ? MathUtils.AveragePosition(centerPositions, markerAccuracies) : MathUtils.AveragePosition(centerPositions);
                                var centerRotation = TrackerSettings.Instance.useWeightedAverage ? MathUtils.AverageRotation(centerRotations, markerAccuracies) : MathUtils.AverageRotation(centerRotations);
                                
                                var markerPoseData = new PoseData
                                {
                                    pos = centerPosition,
                                    rot = centerRotation
                                };

                                // var accuracy = CalculateTrackerAccuracy(markerPoseData, interpolatedHeadPoseData);

                                const int halfCubeSides = CUBE_SIDES / 2;

                                trackedMarkers.Add(new TrackedMarker
                                {
                                    Id = cubeId,
                                    MarkerPoseData = markerPoseData,
                                    IsHighConfidence = cubeMarkers.Count >= halfCubeSides,
                                    Accuracy = averageMarkerAccuracy
                                });
                            }
                        }
                    }
                }
                
                _trackerSimulation.EnqueueFrameTrackerData(frameTimestampNs,
                    trackedMarkers);
            }
            
            processingMat.Dispose();
        }

        private float CalculateTrackerAccuracy(PoseData markerPoseData, PoseData interpolatedHeadPoseData)
        {
            var distance = Vector3.Distance(markerPoseData.pos, interpolatedHeadPoseData.pos);
            var maxDistance = MaxDistanceEstimation(cameraResolution);
            var distanceAccuracy = Mathf.Clamp01(distance / maxDistance);

            var cameraDirection = (interpolatedHeadPoseData.rot * Vector3.forward).normalized;
            var markerNormal = (markerPoseData.rot * Vector3.up).normalized;
            var angleAccuracy = Mathf.Clamp01(Mathf.Abs(Vector3.Dot(cameraDirection, markerNormal)));
            
            return weightingCoefficient * distanceAccuracy + (1f - weightingCoefficient) * angleAccuracy;
        }

        private void HandleVisualizationToggle()
        {
            if (OVRInput.GetDown(visualisationToggleButton, controllerMask))
            {
                showCameraFeed = !showCameraFeed;
                resultRawImage.gameObject.SetActive(showCameraFeed);
                _trackerSimulation.SetMarkerObjectsVisibility(!showCameraFeed);
            }
        }

        private void ReleaseResources()
        {
            Debug.Log("Releasing ArUco tracking resources");

            _processingRgbMat?.Dispose();
            _originalFrameMat?.Dispose();
            _halfSizeMat?.Dispose();
            _arucoDetector?.Dispose();
            _recoveredMarkerIndices?.Dispose();
        }

        private void InitializeMarkerTracking()
        {
            var intrinsics = _cameraInfo.Intrinsics;
            var cX = intrinsics.PrincipalPoint.x; // Principal point X (optical center)
            var cY = intrinsics.PrincipalPoint.y; // Principal point Y (optical center)
            var fX = intrinsics.FocalLength.x; // Focal length X
            var fY = intrinsics.FocalLength.y; // Focal length Y
            var s = intrinsics.Skew;
            var originalWidth = _resolution.width; // intrinsics.Resolution.x;   // Image width
            var originalHeight = _resolution.height; // intrinsics.Resolution.y;  // Image height

            var processingWidth = _resolution.width; // Mathf.FloorToInt(originalWidth / divideNumber);
            var processingHeight = _resolution.height; // Mathf.FloorToInt(originalHeight / divideNumber);
            var divideNumber = _cameraInfo.Intrinsics.Resolution.x / _resolution.width;
            fX = Mathf.Floor(fX / divideNumber);
            fY = Mathf.Floor(fY / divideNumber);
            cX = Mathf.Floor(cX / divideNumber);
            cY = Mathf.Floor(cY / divideNumber);

            _cameraIntrinsicMatrix = new Mat(3, 3, CvType.CV_64FC1);
            _cameraIntrinsicMatrix.put(0, 0, fX);
            // _cameraIntrinsicMatrix.put(0, 1, s);
            _cameraIntrinsicMatrix.put(0, 1, 0);
            _cameraIntrinsicMatrix.put(0, 2, cX);
            _cameraIntrinsicMatrix.put(1, 0, 0);
            _cameraIntrinsicMatrix.put(1, 1, fY);
            _cameraIntrinsicMatrix.put(1, 2, cY);
            _cameraIntrinsicMatrix.put(2, 0, 0);
            _cameraIntrinsicMatrix.put(2, 1, 0);
            _cameraIntrinsicMatrix.put(2, 2, 1.0f);
            
            Debug.Log("Camera intrinsic matrix: " + _cameraIntrinsicMatrix.dump());

            _cameraDistortionCoeffs = new MatOfDouble(0, 0, 0, 0);
            _originalFrameMat = new Mat(originalHeight, originalWidth, CvType.CV_8UC4);

            _halfSizeMat = new Mat(processingHeight, processingWidth, CvType.CV_8UC4);
            _processingRgbMat = new Mat(processingHeight, processingWidth, CvType.CV_8UC3);
            
            _currentDict = (int)dictionaryId;
            _markerDictionary = Objdetect.getPredefinedDictionary(_currentDict);

            _recoveredMarkerIndices = new Mat();

            _detectorParams = new DetectorParameters();
            _detectorParams.set_minDistanceToBorder(3);
            _detectorParams.set_useAruco3Detection(true);
            _detectorParams.set_cornerRefinementMethod(Objdetect.CORNER_REFINE_SUBPIX);
            _detectorParams.set_minSideLengthCanonicalImg(20);
            _detectorParams.set_errorCorrectionRate(0.8);
            var refineParameters = new RefineParameters(10f, 3f, true);

            _arucoDetector = new ArucoDetector(_markerDictionary, _detectorParams, refineParameters);

            var gridBoardTotalMarkers = gridBoardRows * gridBoardColumns;
            var totalGridBoards = _trackerSimulation.TrackerCount;
            if (markerArrangement == MarkerArrangement.Cube) totalGridBoards *= CUBE_SIDES;

            for (var i = 0; i < totalGridBoards; i++)
            {
                var gridBoardIds = new Mat(gridBoardTotalMarkers, 1, CvType.CV_32SC1);
                gridBoardIds.put(0, 0, Enumerable.Range(i * gridBoardTotalMarkers, (i + 1) * gridBoardTotalMarkers - 1).ToArray());

                _gridBoards.Add(new GridBoard(new Size(gridBoardColumns, gridBoardRows), markerLength, markerSeparation, _markerDictionary, gridBoardIds));
            }

            ConfigureResultTexture(originalWidth, originalHeight);

            _isReady = true;
        }

        private void ConfigureResultTexture(int width, int height)
        {
            _resultTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
            resultRawImage.texture = _resultTexture;
        }

        private async UniTask StartCamera()
        {
            if (_cameraInfo == null)
            {
                Debug.LogError("Camera permission was not given.");
                return;
            }

            if (_cameraDevice || _captureSession != null)
            {
                Debug.Log("Camera or capture session is already open.");
                return;
            }

            _cameraDevice = UCameraManager.Instance.OpenCamera(_cameraInfo);

            var cameraState = await _cameraDevice.WaitForInitializationAsync().AsUniTask();
            if (cameraState != NativeWrapperState.Opened)
            {
                Debug.LogError("Could not open camera!");

                _cameraDevice.Destroy();
                _cameraDevice = null;
                return;
            }

            Debug.Log("Camera opened.");

            Debug.Log(_cameraInfo.ToString());

            var resolutionIndex = Mathf.Clamp((int)cameraResolution, 0, _cameraInfo.SupportedResolutions.Length);
            _resolution = _cameraInfo.SupportedResolutions[resolutionIndex];
            _captureSession = _cameraDevice.CreateContinuousCaptureSession(_resolution);

            Debug.Log("Capture session created.");

            var captureState = await _captureSession.CaptureSession.WaitForInitializationAsync().AsUniTask();
            if (captureState != NativeWrapperState.Opened)
            {
                Debug.LogError("Could not open camera session!");

                _captureSession.Destroy();
                _cameraDevice.Destroy();

                (_cameraDevice, _captureSession) = (null, null);
                return;
            }

            Debug.Log("Capture session opened.");

            _captureSession.CameraFrameForwarder.OnFrameReady += OnFrameReady;
            Debug.Log("Subscribed to frame processed event.");
        }

        private void StopCamera()
        {
            if (_captureSession != null)
            {
                _captureSession.Destroy();
                _captureSession = null;
            }

            if (_cameraDevice)
            {
                _cameraDevice.Destroy();
                _cameraDevice = null;
            }
        }

        private static float MaxDistanceEstimation(CameraResolution resolution)
        {
            return resolution switch
            {
                CameraResolution.Low => 0.2f,
                CameraResolution.Medium => 0.4f,
                CameraResolution.High => 0.8f,
                CameraResolution.VeryHigh => 1.0f,
                _ => throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null)
            };
        }

        private struct FrameHeadPoseData
        {
            public PoseData HeadPoseData;
            public long FrameTimestampNs;
        }
    }
}