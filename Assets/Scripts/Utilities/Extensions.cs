using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace QuestMarkerTracking.Utilities
{
    public static class Extensions
    {
        public static Vector2 RemoveY(this Vector3 point)
        {
            return new Vector2(point.x, point.z);
        }

        public static Vector3 AddY(this Vector2 point, float y = 0f)
        {
            return new Vector3(point.x, y, point.y);
        }

        public static Vector2 ToUnityVector2(this System.Numerics.Vector2 point)
        {
            return new Vector2(point.X, point.Y);
        }

        public static System.Numerics.Vector2 ToSystemNumericVector2(this Vector2 point)
        {
            return new System.Numerics.Vector2(point.x, point.y);
        }

        public static float GetRotation(this Matrix3x2 matrix)
        {
            return Mathf.Atan2(matrix.M12, matrix.M11);
        }

        public static Vector2 Transform(this Vector2 vector, Matrix3x2 matrix)
        {
            return System.Numerics.Vector2.Transform(vector.ToSystemNumericVector2(), matrix).ToUnityVector2();
        }
        
        public static int GetFaceClosestToDirection(this Transform transform, Vector3 direction)
        {
            var bestIndex = 0;
            var bestDot = -1f;

            for (var i = 0; i < MathUtils.CubeFaceNormals.Length; i++)
            {
                var worldNormal = transform.TransformDirection(MathUtils.CubeFaceNormals[i]);
                var dot = Vector3.Dot(worldNormal.normalized, direction);
                if (dot < bestDot) continue;
                
                bestDot = dot;
                bestIndex = i;
            }
            
            return bestIndex;
        }
        
        public static int GetFaceClosestToUp(this Transform transform)
        {
            var bestIndex = 0;
            var bestDot = -1f;

            for (var i = 0; i < MathUtils.CubeFaceNormals.Length; i++)
            {
                var worldNormal = transform.TransformDirection(MathUtils.CubeFaceNormals[i]);
                var dot = Vector3.Dot(worldNormal.normalized, Vector3.up);
                if (dot < bestDot) continue;
                
                bestDot = dot;
                bestIndex = i;
            }
            
            return bestIndex;
        }
        
        public static int GetFaceClosestToUp(this Transform transform, out float angleToUp)
        {
            var bestIndex = 0;
            var bestDot = -1f;

            for (var i = 0; i < MathUtils.CubeFaceNormals.Length; i++)
            {
                var worldNormal = transform.TransformDirection(MathUtils.CubeFaceNormals[i]);
                var dot = Vector3.Dot(worldNormal.normalized, Vector3.up);
                if (dot < bestDot) continue;
                
                bestDot = dot;
                bestIndex = i;
            }

            angleToUp = Mathf.Acos(bestDot) * Mathf.Rad2Deg;
            return bestIndex;
        }
        
        public static int GetFaceClosestToUp(this Transform transform, out float angleToUp, int excludeFaceIndex)
        {
            var bestIndex = 0;
            var bestDot = -1f;

            for (var i = 0; i < MathUtils.CubeFaceNormals.Length; i++)
            {
                if (i == excludeFaceIndex) continue;
                var worldNormal = transform.TransformDirection(MathUtils.CubeFaceNormals[i]);
                var dot = Vector3.Dot(worldNormal.normalized, Vector3.up);
                if (dot < bestDot) continue;
                
                bestDot = dot;
                bestIndex = i;
            }

            angleToUp = Mathf.Acos(bestDot) * Mathf.Rad2Deg;
            return bestIndex;
        }

        public static void Shuffle<T>(this List<T> list)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));

            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = Random.Range(0, list.Count); // 0 <= k <= n
                (list[n], list[k]) = (list[k], list[n]);
            }
        }
    }
}