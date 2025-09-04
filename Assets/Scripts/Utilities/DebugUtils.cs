using UnityEngine;

namespace QuestMarkerTracking.Utilities
{
    public static class DebugUtils
    {
        public enum CubeFace
        {
            Top,
            Left,
            Front,
            Right,
            Back,
            Bottom
        }
        
        public static readonly Vector3[] CubeFaceNormals = { Vector3.right, Vector3.up, Vector3.forward, Vector3.left, Vector3.down, Vector3.back };
        
        public static readonly Color[] CubeFaceColors = { Color.red, Color.green, Color.blue, Color.cyan, Color.magenta, Color.yellow };
    }
}