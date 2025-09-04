using System.IO;
using System.Linq;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgcodecsModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Rect = OpenCVForUnity.CoreModule.Rect;

namespace QuestMarkerTracking.Z_OLD
{
    public class ArUcoGridBoardGenerator : MonoBehaviour
    {
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

        public enum MarkerID
        {
            MarkerID_0,
            MarkerID_1,
            MarkerID_2,
            MarkerID_3,
            MarkerID_4,
            MarkerID_5,
            MarkerID_6,
            MarkerID_7,
            MarkerID_8,
            MarkerID_9
        }

        // width of the marker borders.
        private const int borderBits = 1;

        // for GridBoard.
        // number of markers in X direction
        private const int gridBoradMarkersX = 5;

        // number of markers in Y direction
        private const int gridBoradMarkersY = 7;

        // marker side length (normally in meters)
        private const float gridBoradMarkerLength = 0.04f;

        // separation between two markers (same unit as markerLength)
        private const float gridBoradMarkerSeparation = 0.01f;

        // id of first marker in dictionary to use on board.
        private const int gridBoradMarkerFirstMarker = 0;

        // minimum margins (in pixels) of the board in the output image
        private const int gridBoradMarginSize = 10;

        [Header("Output")]
        /// <summary>
        /// The RawImage for previewing the result.
        /// </summary>
        public RawImage resultPreview;

        [Space(10)]
        /// <summary>
        /// The size of the output marker image (px).
        /// </summary>
        public int markerSize = 1000;

        /// <summary>
        ///     The dictionary identifier.
        /// </summary>
        public ArUcoDictionary dictionaryId = ArUcoDictionary.DICT_6X6_250;

        /// <summary>
        ///     The dictionary id dropdown.
        /// </summary>
        public Dropdown dictionaryIdDropdown;

        /// <summary>
        ///     The marker identifier.
        /// </summary>
        public MarkerID markerId = MarkerID.MarkerID_1;

        /// <summary>
        ///     The offset for the grid board id.
        /// </summary>
        public int markerIdOffset;

        /// <summary>
        ///     The marker id dropdown.
        /// </summary>
        public Dropdown markerIdDropdown;

        /// <summary>
        ///     The save path input field.
        /// </summary>
        public InputField savePathInputField;

        /// <summary>
        ///     The marker img mat.
        /// </summary>
        private Mat markerImg;

        /// <summary>
        ///     The texture.
        /// </summary>
        private Texture2D texture;


        // Use this for initialization
        private void Start()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            markerImg = new Mat(markerSize, markerSize, CvType.CV_8UC3);
            texture = new Texture2D(markerImg.cols(), markerImg.rows(), TextureFormat.RGB24, false);

            resultPreview.texture = texture;
            resultPreview.GetComponent<AspectRatioFitter>().aspectRatio = (float)texture.width / texture.height;

            markerIdDropdown.value = (int)markerId;
            dictionaryIdDropdown.value = (int)dictionaryId;

            CreateMarkerImg();
        }

        /// <summary>
        ///     Raises the destroy event.
        /// </summary>
        private void OnDestroy()
        {
            if (markerImg != null)
                markerImg.Dispose();

            Utils.setDebugMode(false);
        }

        public void CreateMarkerImg()
        {
            if (markerImg.cols() != markerSize)
            {
                markerImg.Dispose();
                markerImg = new Mat(markerSize, markerSize, CvType.CV_8UC3);
                texture = new Texture2D(markerImg.cols(), markerImg.rows(), TextureFormat.RGB24, false);
            }
            else
            {
                markerImg.setTo(Scalar.all(255));
            }

            // create dictinary.
            Dictionary dictionary = Objdetect.getPredefinedDictionary((int)dictionaryId);

            Objdetect.generateImageMarker(dictionary, (int)markerId, markerSize, markerImg, borderBits);

            // draw marker.
            int gridBoardTotalMarkers = gridBoradMarkersX * gridBoradMarkersY;
            Mat gridBoardIds = new Mat(gridBoardTotalMarkers, 1, CvType.CV_32SC1);
            gridBoardIds.put(0, 0, Enumerable.Range(gridBoradMarkerFirstMarker, gridBoradMarkerFirstMarker + gridBoardTotalMarkers).ToArray());

            GridBoard gridBoard = new GridBoard(new Size(gridBoradMarkersX, gridBoradMarkersY), gridBoradMarkerLength, gridBoradMarkerSeparation, dictionary, gridBoardIds);

            // This code includes adjustments to address an issue in the OpenCV GridBoard::generateImage method,
            // where the vertical and horizontal margins between AR markers in the grid are not evenly spaced.

            // Calculate the aspect ratio of the grid (width/height)
            // Calculate the total aspect ratio of the marker grid including markers and separations
            double gridAspectRatio = (gridBoradMarkersX * gridBoradMarkerLength + (gridBoradMarkersX - 1) * gridBoradMarkerSeparation) /
                                     (gridBoradMarkersY * gridBoradMarkerLength + (gridBoradMarkersY - 1) * gridBoradMarkerSeparation);

            // Adjust the output size to fit within the specified markerSize
            int adjustedWidth, adjustedHeight;
            if (gridAspectRatio >= 1.0)
            {
                // If the grid is wider than tall, fix the width to markerSize and scale the height proportionally
                adjustedWidth = markerSize;
                adjustedHeight = (int)(markerSize / gridAspectRatio);
            }
            else
            {
                // If the grid is taller than wide, fix the height to markerSize and scale the width proportionally
                adjustedHeight = markerSize;
                adjustedWidth = (int)(markerSize * gridAspectRatio);
            }

            Mat adjustedMarkerImg = new Mat(markerImg,
                new Rect((markerSize - adjustedWidth) / 2, (markerSize - adjustedHeight) / 2, adjustedWidth, adjustedHeight));

            gridBoard.generateImage(new Size(adjustedWidth, adjustedHeight), adjustedMarkerImg, gridBoradMarginSize, borderBits);
            gridBoard.Dispose();
            Debug.Log("draw GridBoard: " + "markersX " + gridBoradMarkersX + " markersY " + gridBoradMarkersY + " markerLength " + gridBoradMarkerLength +
                      " markerSeparation " + gridBoradMarkerSeparation + " dictionaryId " + (int)dictionaryId + " firstMarkerId " + gridBoradMarkerFirstMarker +
                      " outSize " + markerSize + " marginSize " + gridBoradMarginSize + " borderBits " + borderBits);

            Utils.matToTexture2D(markerImg, texture);
        }

        private void SaveMarkerImg()
        {
            // save the markerImg.
            string saveDirectoryPath = Path.Combine(Application.persistentDataPath, "ArUcoCreateMarkerExample");
            string savePath = "";
#if UNITY_WEBGL && !UNITY_EDITOR
            string format = "jpg";
            MatOfInt compressionParams = new MatOfInt(Imgcodecs.IMWRITE_JPEG_QUALITY, 100);
#else
            string format = "png";
            MatOfInt compressionParams = new MatOfInt(Imgcodecs.IMWRITE_PNG_COMPRESSION, 0);
#endif
            savePath = Path.Combine(saveDirectoryPath, "GridBoard-mx" + gridBoradMarkersX + "-my" + gridBoradMarkersY + "-ml" + gridBoradMarkerLength + "-ms" +
                                                       gridBoradMarkerSeparation + "-d" + (int)dictionaryId + "-fi" + gridBoradMarkerFirstMarker + "-os" + markerSize + "-ms" + gridBoradMarginSize + "-bb" + borderBits + "." + format);

            if (!Directory.Exists(saveDirectoryPath))
            {
                Directory.CreateDirectory(saveDirectoryPath);
            }

            Imgcodecs.imwrite(savePath, markerImg, compressionParams);

            savePathInputField.text = savePath;
            Debug.Log("savePath: " + savePath);
        }

        /// <summary>
        ///     Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        ///     Raises the dictionary id dropdown value changed event.
        /// </summary>
        public void OnDictionaryIdDropdownValueChanged(int result)
        {
            if ((int)dictionaryId != result)
            {
                dictionaryId = (ArUcoDictionary)result;
                CreateMarkerImg();
            }
        }

        /// <summary>
        ///     Raises the marker id dropdown value changed event.
        /// </summary>
        public void OnMarkerIdDropdownValueChanged(int result)
        {
            if ((int)markerId != result)
            {
                markerId = (MarkerID)result;
                CreateMarkerImg();
            }
        }

        /// <summary>
        ///     Raises the save marker img button click event.
        /// </summary>
        public void OnSaveMarkerImgButtonClick()
        {
            SaveMarkerImg();
        }
    }
}