# ReHap
Remappable Haptics: Design and Implementation of an Optically Tracked Tangible User Interface with Dynamic Mapping for Extended Reality (XR)

Keywords:
- Optical/Computer Vision Tracking
- Tangible User Interface
- Extended Reality (XR)
- Dynamic Mapping

## Setup

1. Clone the repository
2. Open the Unity project with version 6000.0.43f1 (Unity Hub should automatically suggest installing the engine)
3. Be sure that the Android Plugin is also installed
4. On opening there will be an error message: *Enter Safe Mode?* —> Press **Ignore**
5. When asked to restart Unity for the OVRPlugin → Press **Restart Editor** (ignore Safe Mode again)
6. Buy and add the [OpenCV for Unity](https://assetstore.unity.com/packages/tools/integration/opencv-for-unity-21088) package from the Asset store to the project. Be sure to install version 2.6.6 (not tested with newest version 3.0.0). Import everything!
7. You should have no errors in the console at this point!
8. Optional: Open Scene *Assets/Scenes/AR Marker Tracking/MarkerTracking.unity*
9. Go to File > Build Profiles and switch platform to Android or better MarkerTracking which has the correct scene in the list. Otherwise edit the scene list to load the correct scene
10. Build the project either with **Build and Run** or **Build** and then upload manually

## Testing

1. Generate an ArUco Marker using this website: https://chev.me/arucogen/
2. Set Dictionary to 4x4 (50, 100, 250, 1000); Marker ID to 0; Marker size, mm to 100
3. Look at the marker, you should see a sphere

## Development

- most interesting classes are MarkerTracking and TrackerSimulation, responsible for entire tracking logic including
    - OpenCV detection pipeline
    - head pose interpolation
    - tracker pose filtering
    - tracker pose prediction
- customize the tracker itself and add functionality there

## Tracker

- to build a tracker generate markers using a website like https://chev.me/arucogen/
- currently no tool at hand to generate easily gridboards or cubes
    - GridBoards: use Illustrator or a similar SVG editor to precisely set markers in a grid with correct separation
    - Cubes: combination of gridboards arranged as a cube
        - tedious process of placing gridboards on the correct position
        - use scene *Assets/Prototyping/ArUcoCube/ArUcoCube.unity*
        - 2x2 gridboard cube has total markers of 4 x 6 = 24 with id range of 0-23
        - use Marker Tracking scene with 6 axis to place each gridboard on the appropriate side on the cube
            - id 0-23: id 0-3 → top, id 4-6 → front, …
            - id 24-47: id 24-27 → top, id 28-31 → front, …
        - no simpler way of setting up as of now
- tracker has UnityEvents for onMoved and topFaceChanged (important for cube tilting detection)

## Tracker Settings

- Component MarkerTracking
    - camera resolution should be High (very high is slow on hardware)
    - Dictionary Id to your ArUco dictionary (only one dict id supported at a time)
    - marker length to your marker size in meters
    - marker arrangement to single for single markers, gridboard or cube
    - gridboard settings only important for gridboard and cube
- Component TrackerSettings
    - lag vs. jitter: changing the settings can help both reducing lag or jitter while also increasing one of the two
    - MarkerTracking Scene already uses good defaults for most people
    - use velocity prediction is good for reducing lag but increases jitter
    - use acceleration prediction is good for reducing lag even more (for accelerating motions) but increases jitter even more (recommended to turn off)
    - use filtering is good for reducing jitter but slightly increases lag (recommended to turn on)
        - use hand tracking switches the filtering between active pose filter (hand closeby) and passive pose filter (hand far away) which can help increasing tracking quality
    - show gizmos is good for debugging (size based on cube tracking)

## Quirks

- when going to sleep with the Meta Quest, the camera access freezes and the app has to be restarted