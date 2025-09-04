using System.Collections.Generic;
using System.Linq;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using QuestMarkerTracking.Filter;
using TMPro;
using UnityEngine;

namespace QuestMarkerTracking.Z_OLD
{
    /// <summary>
    ///     ArUco marker detection and tracking component.
    ///     Handles detection of ArUco markers in camera frames and provides pose estimation.
    /// </summary>
    public class ArUcoMarkerTracking : MonoBehaviour
    {
        /// <summary>
        ///     Available ArUco dictionaries for marker detection
        /// </summary>
        public enum ArUcoDictionary
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

        /// <summary>
        ///     Type of ArUco marker to detect
        /// </summary>
        public enum MarkerType
        {
            CanonicalMarker,
            GridBoard,
            ChArUcoBoard,
            ChArUcoDiamondMarker
        }

        private const int GridBoardMarkersY = 3;
        private const int GridBoardMarkersX = 2;

        /// <summary>
        ///     The ArUco dictionary to use for marker detection.
        /// </summary>
        [SerializeField] private ArUcoDictionary _dictionaryId = ArUcoDictionary.DICT_4X4_50;

        [Space(10)]
        /// <summary>
        /// The length of the markers' side in meters.
        /// </summary>
        [SerializeField] private float _markerLength = 0.1f;

        /// <summary>
        ///     Coefficient for low-pass filter (0-1). Higher values mean more smoothing.
        /// </summary>
        [Range(0, 1)]
        [SerializeField] private float _poseFilterCoefficient = 0.5f;

        /// <summary>
        ///     Division factor for input image resolution. Higher values improve performance but reduce detection accuracy.
        /// </summary>
        [SerializeField] [Range(1f, 4f)] private float _divideNumber = 2;

        [SerializeField] private int gridBoardCount = 2;
        [SerializeField] private float length = 0.046f;
        [SerializeField] private float markerSeparation = 0.01f;

        [SerializeField] private TMP_Text text;
        [SerializeField] private OVRInput.Button switchDictButton = OVRInput.Button.One;
        [SerializeField] private OVRInput.Button switchFilterButton = OVRInput.Button.PrimaryThumbstickDown;

        [SerializeField] private AbstractTrackerFilter[] trackerFilters;
        private readonly Dictionary<int, long> _timestampDataDictionary = new();

        /// <summary>
        ///     Dictionary storing previous pose data for each marker ID for smoothing
        /// </summary>
        private readonly Dictionary<int, PoseData> _prevPoseDataDictionary = new();

        private readonly List<GridBoard> _gridBoards = new();
        private ArucoDetector _arucoDetector;
        private DetectorParameters _detectorParams;
        private Dictionary _markerDictionary;
        private GraphicsBuffer _graphicsBuffer;

        private GridBoard _gridBoard;

        private int _currentDict;
        private int _currentFilter;
        private List<Mat> _detectedMarkerCorners;
        private List<Mat> _rejectedMarkerCandidates;

        /// <summary>
        ///     The camera intrinsic parameters matrix.
        /// </summary>
        private Mat _cameraIntrinsicMatrix;

        // ArUco detection related mats and variables
        private Mat _detectedMarkerIds;

        /// <summary>
        ///     Resized mat for intermediate processing.
        /// </summary>
        private Mat _halfSizeMat;

        /// <summary>
        ///     Full-size RGBA mat from original webcam image.
        /// </summary>
        private Mat _originalWebcamMat;

        // OpenCV matrices for image processing
        /// <summary>
        ///     RGB format mat for marker detection and result display.
        /// </summary>
        private Mat _processingRgbMat;

        private Mat _recoveredMarkerIndices;

        /// <summary>
        ///     The camera distortion coefficients.
        /// </summary>
        private MatOfDouble _cameraDistortionCoeffs;

        /// <summary>
        ///     Read-only access to the divide number value
        /// </summary>
        public float DivideNumber => _divideNumber;

        /// <summary>
        ///     Read-only access to determine if tracking is ready
        /// </summary>
        public bool IsReady { get; private set; }

        private void Update()
        {
            if (OVRInput.GetDown(switchDictButton) || Input.GetKeyDown(KeyCode.Space))
            {
                _currentDict = (_currentDict + 1) % 22;
                _markerDictionary = Objdetect.getPredefinedDictionary(_currentDict);
                _arucoDetector.setDictionary(_markerDictionary);
                SetText();
            }

            if (OVRInput.GetDown(switchFilterButton))
            {
                _currentFilter = (_currentFilter + 1) % trackerFilters.Length;
                SetText();
            }
        }

        /// <summary>
        ///     Clean up when object is destroyed
        /// </summary>
        private void OnDestroy()
        {
            ReleaseResources();
        }

        /// <summary>
        ///     Initialize the marker tracking system with camera parameters
        /// </summary>
        /// <param name="imageWidth">Camera image width in pixels</param>
        /// <param name="imageHeight">Camera image height in pixels</param>
        /// <param name="cx">Principal point X coordinate</param>
        /// <param name="cy">Principal point Y coordinate</param>
        /// <param name="fx">Focal length X</param>
        /// <param name="fy">Focal length Y</param>
        public void Initialize(int imageWidth, int imageHeight, float cx, float cy, float fx, float fy)
        {
            InitializeMatrices(imageWidth, imageHeight, cx, cy, fx, fy);
        }

        /// <summary>
        ///     Initialize all OpenCV matrices and detector parameters
        /// </summary>
        private void InitializeMatrices(int originalWidth, int originalHeight, float cX, float cY, float fX, float fY)
        {
            // Processing dimensions (scaled by divide number)
            var processingWidth = Mathf.FloorToInt(originalWidth / _divideNumber);
            var processingHeight = Mathf.FloorToInt(originalHeight / _divideNumber);
            fX = Mathf.Floor(fX / _divideNumber);
            fY = Mathf.Floor(fY / _divideNumber);
            cX = Mathf.Floor(cX / _divideNumber);
            cY = Mathf.Floor(cY / _divideNumber);

            // Create camera intrinsic matrix
            _cameraIntrinsicMatrix = new Mat(3, 3, CvType.CV_64FC1);
            _cameraIntrinsicMatrix.put(0, 0, fX);
            _cameraIntrinsicMatrix.put(0, 1, 0);
            _cameraIntrinsicMatrix.put(0, 2, cX);
            _cameraIntrinsicMatrix.put(1, 0, 0);
            _cameraIntrinsicMatrix.put(1, 1, fY);
            _cameraIntrinsicMatrix.put(1, 2, cY);
            _cameraIntrinsicMatrix.put(2, 0, 0);
            _cameraIntrinsicMatrix.put(2, 1, 0);
            _cameraIntrinsicMatrix.put(2, 2, 1.0f);

            // No distortion coefficients for Quest cameras
            _cameraDistortionCoeffs = new MatOfDouble(0, 0, 0, 0);

            // Initialize all processing mats
            _originalWebcamMat = new Mat(originalHeight, originalWidth, CvType.CV_8UC4);

            _graphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)_originalWebcamMat.total(), 4);

            _halfSizeMat = new Mat(processingHeight, processingWidth, CvType.CV_8UC4);
            _processingRgbMat = new Mat(processingHeight, processingWidth, CvType.CV_8UC3);

            // Create ArUco detection mats
            _detectedMarkerIds = new Mat();
            _detectedMarkerCorners = new List<Mat>();
            _rejectedMarkerCandidates = new List<Mat>();
            _currentDict = (int)_dictionaryId;
            _markerDictionary = Objdetect.getPredefinedDictionary(_currentDict);

            SetText();

            _recoveredMarkerIndices = new Mat();

            // Configure detector parameters for optimal performance
            _detectorParams = new DetectorParameters();
            _detectorParams.set_minDistanceToBorder(3);
            _detectorParams.set_useAruco3Detection(true);
            _detectorParams.set_cornerRefinementMethod(Objdetect.CORNER_REFINE_SUBPIX);
            _detectorParams.set_minSideLengthCanonicalImg(20);
            _detectorParams.set_errorCorrectionRate(0.8);
            // _detectorParams.set_perspectiveRemovePixelPerCell(20);
            var refineParameters = new RefineParameters(10f, 3f, true);

            // Create the ArUco detector
            _arucoDetector = new ArucoDetector(_markerDictionary, _detectorParams, refineParameters);

            const int gridBoardTotalMarkers = GridBoardMarkersX * GridBoardMarkersY;

            for (var i = 0; i < gridBoardCount; i++)
            {
                var gridBoardIds = new Mat(gridBoardTotalMarkers, 1, CvType.CV_32SC1);
                gridBoardIds.put(0, 0, Enumerable.Range(i * gridBoardTotalMarkers, (i + 1) * gridBoardTotalMarkers - 1).ToArray());

                _gridBoards.Add(new GridBoard(new Size(GridBoardMarkersX, GridBoardMarkersY), length, markerSeparation, _markerDictionary, gridBoardIds));
            }

            IsReady = true;
        }

        private void SetText()
        {
            text.text = "Current Dictionary: " + (ArUcoDictionary)_currentDict + "\nCurrent Filter: " + trackerFilters[_currentFilter].GetType();
        }

        /// <summary>
        ///     Release all OpenCV resources
        /// </summary>
        private void ReleaseResources()
        {
            Debug.Log("Releasing ArUco tracking resources");

            _processingRgbMat?.Dispose();
            _originalWebcamMat?.Dispose();
            _halfSizeMat?.Dispose();
            _arucoDetector?.Dispose();
            _detectedMarkerIds?.Dispose();


            foreach (var corner in _detectedMarkerCorners)
            {
                corner.Dispose();
            }

            _detectedMarkerCorners.Clear();

            foreach (var rejectedCorner in _rejectedMarkerCandidates)
            {
                rejectedCorner.Dispose();
            }

            _rejectedMarkerCandidates.Clear();

            _recoveredMarkerIndices?.Dispose();
        }

        /// <summary>
        ///     Handle errors that occur during tracking operations
        /// </summary>
        /// <param name="errorCode">Error code</param>
        /// <param name="message">Error message</param>
        public void HandleError(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("ArUco tracking error: " + errorCode + ":" + message);
        }

        /// <summary>
        ///     Detect ArUco markers in the provided webcam texture
        /// </summary>
        /// <param name="webCamTexture">Input webcam texture</param>
        /// <param name="resultTexture">Optional output texture for visualization</param>
        public void DetectMarker(WebCamTexture webCamTexture, Texture2D resultTexture = null)
        {
            if (!IsReady) return;
            if (webCamTexture == null)
            {
                return;
            }

            // Get image from webcam at full size
            Utils.webCamTextureToMat(webCamTexture, _originalWebcamMat);

            // Resize for processing
            Imgproc.resize(_originalWebcamMat, _halfSizeMat, _halfSizeMat.size());

            // Convert to RGB for ArUco processing
            Imgproc.cvtColor(_halfSizeMat, _processingRgbMat, Imgproc.COLOR_RGBA2RGB);

            // Reset detection containers
            _detectedMarkerIds.create(0, 1, CvType.CV_32S);
            _detectedMarkerCorners.Clear();
            _rejectedMarkerCandidates.Clear();

            _arucoDetector.detectMarkers(_processingRgbMat, _detectedMarkerCorners, _detectedMarkerIds, _rejectedMarkerCandidates);

            // Draw detected markers for visualization
            if (_detectedMarkerCorners.Count == _detectedMarkerIds.total() || _detectedMarkerIds.total() == 0)
            {
                Objdetect.drawDetectedMarkers(_processingRgbMat, _detectedMarkerCorners, _detectedMarkerIds, new Scalar(0, 255, 0));
            }

            // Update result texture for visualization
            if (resultTexture != null)
            {
                Utils.matToTexture2D(_processingRgbMat, resultTexture);
            }
        }

        public async Awaitable DetectMarker(RenderTexture renderTexture, Texture2D resultTexture = null, bool drawDetectedMarkers = true)
        {
            if (!renderTexture || !IsReady)
            {
                return;
            }

            await Awaitable.BackgroundThreadAsync();

            // Get image from webcam at full size
            Utils.renderTextureToMat(renderTexture, _originalWebcamMat, _graphicsBuffer);

            // Resize for processing
            Imgproc.resize(_originalWebcamMat, _halfSizeMat, _halfSizeMat.size());

            // Convert to RGB for ArUco processing
            Imgproc.cvtColor(_halfSizeMat, _processingRgbMat, Imgproc.COLOR_RGBA2RGB);

            // Reset detection containers
            _detectedMarkerIds.create(0, 1, CvType.CV_32S);
            _detectedMarkerCorners.Clear();
            _rejectedMarkerCandidates.Clear();

            _arucoDetector.detectMarkers(_processingRgbMat, _detectedMarkerCorners, _detectedMarkerIds, _rejectedMarkerCandidates);

            if (drawDetectedMarkers)
            {
                // Draw detected markers for visualization
                if (_detectedMarkerCorners.Count == _detectedMarkerIds.total() || _detectedMarkerIds.total() == 0)
                {
                    Objdetect.drawDetectedMarkers(_processingRgbMat, _detectedMarkerCorners, _detectedMarkerIds,
                        new Scalar(0, 255, 0));
                }

                // Update result texture for visualization
                if (resultTexture)
                {
                    Utils.matToTexture2D(_processingRgbMat, resultTexture);
                }
            }
        }

        /// <summary>
        ///     Estimate pose for each detected marker and update corresponding game objects
        /// </summary>
        /// <param name="arObjects">Dictionary mapping marker IDs to game objects</param>
        /// <param name="camTransform">Camera transform for world-space positioning</param>
        public void EstimatePoseCanonicalMarker(Dictionary<int, GameObject> arObjects, Transform camTransform)
        {
            // Skip if not ready or no markers detected
            if (!IsReady || _detectedMarkerCorners == null || _detectedMarkerCorners.Count == 0)
            {
                return;
            }

            // Define 3D coordinates of marker corners (marker center is at origin)
            using var objectPoints = new MatOfPoint3f(new Point3(-_markerLength / 2f, _markerLength / 2f, 0),
                new Point3(_markerLength / 2f, _markerLength / 2f, 0),
                new Point3(_markerLength / 2f, -_markerLength / 2f, 0),
                new Point3(-_markerLength / 2f, -_markerLength / 2f, 0));
            // Process each detected marker
            for (var i = 0; i < _detectedMarkerCorners.Count; i++)
            {
                // Get marker ID
                var currentMarkerId = (int)_detectedMarkerIds.get(i, 0)[0];

                // Check if this marker has a corresponding game object
                if (!arObjects.TryGetValue(currentMarkerId, out var targetObject) || targetObject == null)
                    continue;

                using var rotationVec = new Mat(1, 1, CvType.CV_64FC3);
                using var translationVec = new Mat(1, 1, CvType.CV_64FC3);
                using var corner_4x1 = _detectedMarkerCorners[i].reshape(2, 4);
                using var imagePoints = new MatOfPoint2f(corner_4x1);
                // Solve PnP to get marker pose
                Calib3d.solvePnP(objectPoints, imagePoints, _cameraIntrinsicMatrix, _cameraDistortionCoeffs, rotationVec, translationVec);

                // Convert to Unity coordinate system
                var rvecArr = new double[3];
                rotationVec.get(0, 0, rvecArr);
                var tvecArr = new double[3];
                translationVec.get(0, 0, tvecArr);
                var poseData = ARUtils.ConvertRvecTvecToPoseData(rvecArr, tvecArr);

                // Get previous pose for this marker (or create new)
                if (!_prevPoseDataDictionary.TryGetValue(currentMarkerId, out var prevPose))
                {
                    prevPose = new PoseData();
                    _prevPoseDataDictionary[currentMarkerId] = prevPose;
                }

                // Apply low-pass filter if we have previous pose data
                if (prevPose.pos != Vector3.zero)
                {
                    var t = _poseFilterCoefficient;

                    // Filter position with linear interpolation
                    poseData.pos = Vector3.Lerp(poseData.pos, prevPose.pos, t);

                    // Filter rotation with spherical interpolation
                    poseData.rot = Quaternion.Slerp(poseData.rot, prevPose.rot, t);
                }

                // Store current pose for next frame
                _prevPoseDataDictionary[currentMarkerId] = poseData;

                // Convert pose to matrix and apply to game object
                var arMatrix = ARUtils.ConvertPoseDataToMatrix(ref poseData, true);
                arMatrix = camTransform.localToWorldMatrix * arMatrix;
                ARUtils.SetTransformFromMatrix(targetObject.transform, ref arMatrix);
            }

            // Optional feature to deactivate objects for markers that weren't detected
            // (Use only if required by your application)
            // foreach (var kvp in arObjects)
            // {
            //     int markerId = kvp.Key;
            //     GameObject obj = kvp.Value;
            //     
            //     // Check if this marker was detected in this frame
            //     bool markerDetectedThisFrame = false;
            //     for (int i = 0; i < _detectedMarkerIds.total(); i++)
            //     {
            //         if ((int)_detectedMarkerIds.get(i, 0)[0] == markerId)
            //         {
            //             markerDetectedThisFrame = true;
            //             break;
            //         }
            //     }
            //     
            //     // Deactivate the object if the marker wasn't detected
            //     if (!markerDetectedThisFrame && obj != null)
            //     {
            //         obj.SetActive(false);
            //     }
            // }
        }

        public async Awaitable EstimatePoseGridBoard(Dictionary<int, GameObject> arObjects, Transform camTransform,
            float deltaTime)
        {
            // Skip if not ready or no markers detected
            if (!IsReady || _detectedMarkerCorners == null || _detectedMarkerCorners.Count == 0)
            {
                return;
            }

            await Awaitable.BackgroundThreadAsync();

            for (var i = 0; i < _gridBoards.Count; i++)
            {
                var gridBoard = _gridBoards[i];

                if (!arObjects.TryGetValue(i, out var targetObject) || targetObject == null)
                    continue;

                using (var rvec = new Mat(1, 1, CvType.CV_64FC3))
                using (var tvec = new Mat(1, 1, CvType.CV_64FC3))
                using (var objectPoints = new Mat())
                using (var imagePoints = new Mat())
                {
                    // Get object and image points for the solvePnP function
                    gridBoard.matchImagePoints(_detectedMarkerCorners, _detectedMarkerIds, objectPoints, imagePoints);

                    if (imagePoints.total() != objectPoints.total())
                        continue;

                    if (objectPoints.total() == 0) // 0 of the detected markers in board
                        continue;

                    // Find pose
                    var obectjPoints_p3f = new MatOfPoint3f(objectPoints);
                    var imagePoints_p3f = new MatOfPoint2f(imagePoints);
                    Calib3d.solvePnP(obectjPoints_p3f, imagePoints_p3f, _cameraIntrinsicMatrix, _cameraDistortionCoeffs, rvec, tvec);

                    // If at least one board marker detected
                    var markersOfBoardDetected = (int)objectPoints.total() / 4;
                    if (markersOfBoardDetected > 0)
                    {
                        var rvecArr = new double[3];
                        rvec.get(0, 0, rvecArr);
                        var tvecArr = new double[3];
                        tvec.get(0, 0, tvecArr);
                        var poseData = ARUtils.ConvertRvecTvecToPoseData(rvecArr, tvecArr);

                        // // Get previous pose for this marker (or create new)
                        // if (!_prevPoseDataDictionary.TryGetValue(i, out var prevPose))
                        // {
                        //     prevPose = new PoseData();
                        //     _prevPoseDataDictionary[i] = prevPose;
                        // }
                        //
                        // // Apply low-pass filter if we have previous pose data
                        // if (prevPose.pos != Vector3.zero)
                        // {
                        //     var t = _poseFilterCoefficient;
                        //     
                        //     // Filter position with linear interpolation
                        //     poseData.pos = Vector3.Lerp(poseData.pos, prevPose.pos, t);
                        //     
                        //     // Filter rotation with spherical interpolation
                        //     poseData.rot = Quaternion.Slerp(poseData.rot, prevPose.rot, t);
                        // }
                        //
                        // // Store current pose for next frame
                        // _prevPoseDataDictionary[i] = poseData;

                        var filteredPose = trackerFilters[_currentFilter].UpdateTracker(i, poseData, deltaTime);
                        // newPoses[i] = trackerFilters[_currentFilter].UpdateTracker(i, poseData, deltaTime);

                        // Convert pose to matrix and apply to game object
                        var arMatrix = ARUtils.ConvertPoseDataToMatrix(ref filteredPose, true);
                        arMatrix = camTransform.localToWorldMatrix * arMatrix;
                        ARUtils.SetTransformFromMatrix(targetObject.transform, ref arMatrix);
                    }
                }
            }

            // await Awaitable.MainThreadAsync();

            // for (var i = 0; i < _gridBoards.Count; i++)
            // {
            //     if (!arObjects.TryGetValue(i, out var targetObject) || targetObject == null)
            //         continue;
            //
            //     var filteredPose = newPoses[i];
            //     
            //     var arMatrix = ARUtils.ConvertPoseDataToMatrix(ref filteredPose, true);
            //     arMatrix = camTransform.localToWorldMatrix * arMatrix;
            //     ARUtils.SetTransformFromMatrix(targetObject.transform, ref arMatrix);
            // }
        }

        /// <summary>
        ///     Explicitly release resources when the object is disposed
        /// </summary>
        public void Dispose()
        {
            ReleaseResources();
        }
    }
}