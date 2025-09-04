using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

namespace QuestMarkerTracking.Utilities
{
    public static class MathUtils
    {
        public static readonly Vector3[] CubeFaceNormals = { Vector3.right, Vector3.up, Vector3.forward, Vector3.left, Vector3.down, Vector3.back };
        
        public static Vector3 AveragePosition(List<Vector3> positions)
        {
            if (positions == null || positions.Count == 0)
                return Vector3.zero;

            var sum = positions.Aggregate(Vector3.zero, (current, pos) => current + pos);
            return sum / positions.Count;
        }

        public static Vector3 AveragePosition(List<Vector3> positions, List<float> weights)
        {
            if (positions == null || weights == null || positions.Count != weights.Count || positions.Count == 0)
            {
                Debug.LogError("Position and accuracy lists must be the same length and non-empty.");
                return Vector3.zero;
            }

            var sum = Vector3.zero;
            var weightSum = 0f;

            for (var i = 0; i < positions.Count; i++)
            {
                var weight = Mathf.Clamp01(weights[i]);
                sum += positions[i] * weight;
                weightSum += weight;
            }

            if (weightSum > 0f) return sum / weightSum;
            
            Debug.LogWarning("All weights are zero; returning default position.");
            return Vector3.zero;

        }
        
        public static Quaternion AverageRotation(List<Quaternion> quaternions)
        {
            if (quaternions == null || quaternions.Count == 0)
                return Quaternion.identity;
            
            var reference = quaternions[0];
            var sum = quaternions.Select(q => Quaternion.Dot(reference, q) < 0f
                    ? new Quaternion(-q.x, -q.y, -q.z, -q.w)
                    : q)
                .Aggregate(Vector4.zero, (current, qq) => current + new Vector4(qq.x, qq.y, qq.z, qq.w));

            sum /= quaternions.Count;
            return new Quaternion(sum.x, sum.y, sum.z, sum.w).normalized;
        }
        
        public static Quaternion AverageRotation(List<Quaternion> quaternions, List<float> weights)
        {
            if (quaternions == null || weights == null || quaternions.Count != weights.Count || quaternions.Count == 0)
                return Quaternion.identity;

            var reference = quaternions[0];
            var weightedSum = Vector4.zero;
            var totalWeight = 0f;

            for (var i = 0; i < quaternions.Count; i++)
            {
                var q = quaternions[i];
                var weight = Mathf.Max(0f, weights[i]); // Ensure weights are non-negative

                // Align to reference hemisphere
                if (Quaternion.Dot(reference, q) < 0f)
                    q = new Quaternion(-q.x, -q.y, -q.z, -q.w);

                weightedSum += new Vector4(q.x, q.y, q.z, q.w) * weight;
                totalWeight += weight;
            }

            if (totalWeight == 0f)
                return Quaternion.identity;

            weightedSum /= totalWeight;

            return new Quaternion(weightedSum.x, weightedSum.y, weightedSum.z, weightedSum.w).normalized;
        }

        
        public static float NanosecondsToSeconds(long time)
        {
            return time * 1e-9f;
        }

        public static float EaseInOutQuad(float x)
        {
            return x < 0.5f ? 2f * x * x : 1f - Mathf.Pow(-2f * x + 2f, 2f) / 2f;
        }

        private static (float? rotAngle, float? transX, float? transY) PointBasedMatching(List<(Vector2, Vector2)> pointPairs)
        {
            var n = pointPairs.Count;
            if (n == 0)
                return (null, null, null);

            float xMean = 0, yMean = 0, xpMean = 0, ypMean = 0;

            foreach (var (p, q) in pointPairs)
            {
                xMean += p.x;
                yMean += p.y;
                xpMean += q.x;
                ypMean += q.y;
            }

            xMean /= n;
            yMean /= n;
            xpMean /= n;
            ypMean /= n;

            float s_x_xp = 0, s_y_yp = 0, s_x_yp = 0, s_y_xp = 0;

            foreach (var (p, q) in pointPairs)
            {
                s_x_xp += (p.x - xMean) * (q.x - xpMean);
                s_y_yp += (p.y - yMean) * (q.y - ypMean);
                s_x_yp += (p.x - xMean) * (q.y - ypMean);
                s_y_xp += (p.y - yMean) * (q.x - xpMean);
            }

            var rotAngle = Mathf.Atan2(s_x_yp - s_y_xp, s_x_xp + s_y_yp);
            var cosR = Mathf.Cos(rotAngle);
            var sinR = Mathf.Sin(rotAngle);

            var transX = xpMean - (xMean * cosR - yMean * sinR);
            var transY = ypMean - (xMean * sinR + yMean * cosR);

            return (rotAngle, transX, transY);
        }

        private static Vector2 CalculateCentroid(List<Vector2> points)
        {
            if (points.Count == 0)
                return Vector2.zero;

            var sum = points.Aggregate(Vector2.zero, (current, point) => current + point);
            return sum / points.Count;
        }

        private static float ComputeInitialRotationAngle(List<Vector2> centeredRef, List<Vector2> centeredPts)
        {
            float sumXX_ref = 0, sumXY_ref = 0, sumYY_ref = 0;
            foreach (var p in centeredRef)
            {
                sumXX_ref += p.x * p.x;
                sumXY_ref += p.x * p.y;
                sumYY_ref += p.y * p.y;
            }

            float covXX_ref = sumXX_ref / centeredRef.Count;
            float covXY_ref = sumXY_ref / centeredRef.Count;
            float covYY_ref = sumYY_ref / centeredRef.Count;

            float sumXX_pts = 0, sumXY_pts = 0, sumYY_pts = 0;
            foreach (var p in centeredPts)
            {
                sumXX_pts += p.x * p.x;
                sumXY_pts += p.x * p.y;
                sumYY_pts += p.y * p.y;
            }

            float covXX_pts = sumXX_pts / centeredPts.Count;
            float covXY_pts = sumXY_pts / centeredPts.Count;
            float covYY_pts = sumYY_pts / centeredPts.Count;

            double angle_ref = 0.5f * Mathf.Atan2(2 * covXY_ref, covXX_ref - covYY_ref);
            double angle_pts = 0.5f * Mathf.Atan2(2 * covXY_pts, covXX_pts - covYY_pts);

            float rotationAngle = (float)(angle_ref - angle_pts);
            return rotationAngle;
        }


        /// <summary>
        ///     Iterative Closest Point (ICP) implementation for 2D point clouds.
        ///     <br />
        ///     Inspired by https://github.com/richardos/icp/blob/master/icp.py
        /// </summary>
        /// <param name="referencePoints">Points to match to</param>
        /// <param name="points">Points that move to reference points</param>
        /// <param name="maxIterations">Number of maximum iterations</param>
        /// <param name="distanceThreshold">Threshold to match point pairs</param>
        /// <param name="convergenceTranslationThreshold">Threshold for convergence with translation</param>
        /// <param name="convergenceRotationThreshold">Threshold for convergence with rotation</param>
        /// <param name="pointPairsThreshold">Number of pairs thresholds for checking</param>
        /// <param name="verbose">Should log steps</param>
        /// <returns>Matrix to transform points to reference</returns>
        public static (List<Matrix3x2> transformationHistory, Matrix3x2 transformation, List<Vector2> alignedPoints)
            Icp(
                List<Vector2> referencePoints,
                List<Vector2> points,
                bool initialAlignment = true,
                int maxIterations = 100,
                float distanceThreshold = 0.3f,
                float convergenceTranslationThreshold = 1e-3f,
                float convergenceRotationThreshold = 1e-4f,
                int pointPairsThreshold = 10,
                bool verbose = false)
        {
            var transformationHistory = new List<Matrix3x2>();
            var transformation = Matrix3x2.Identity;

            if (initialAlignment)
            {
                var centroidReference = CalculateCentroid(referencePoints);
                var centroidPoints = CalculateCentroid(points);

                var centeredReferencePoints = referencePoints.Select(p => p - centroidReference).ToList();
                var centeredPoints = points.Select(p => p - centroidPoints).ToList();

                var rotationAngle = ComputeInitialRotationAngle(centeredReferencePoints, centeredPoints);

                var rotationMatrix = Matrix3x2.CreateRotation(rotationAngle);
                var translationVector = centroidReference - centroidPoints;

                var translationMatrix = Matrix3x2.CreateTranslation(translationVector.x, translationVector.y);
                var initialAlignmentMatrix = rotationMatrix * translationMatrix;

                points = points.Select(p => p.Transform(initialAlignmentMatrix)).ToList();

                transformationHistory.Add(initialAlignmentMatrix);
                transformation *= initialAlignmentMatrix;
            }

            for (var iter = 0; iter < maxIterations; iter++)
            {
                if (verbose)
                    Debug.Log($"--- Iteration {iter} ---");

                var pointPairs = new List<(Vector2, Vector2)>();

                foreach (var p in points)
                {
                    var minDist = float.MaxValue;
                    var closest = Vector2.zero;

                    foreach (var rp in referencePoints)
                    {
                        var dist = Vector2.Distance(p, rp);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closest = rp;
                        }
                    }

                    if (minDist < distanceThreshold)
                        pointPairs.Add((p, closest));
                }

                if (verbose) Debug.Log($"Number of pairs found: {pointPairs.Count}");

                if (pointPairs.Count < pointPairsThreshold)
                {
                    if (verbose) Debug.Log("Too few pairs, stopping.");
                    break;
                }

                var (rotAngle, transX, transY) = PointBasedMatching(pointPairs);

                if (!rotAngle.HasValue || !transX.HasValue || !transY.HasValue)
                {
                    if (verbose) Debug.Log("No better solution can be found!");
                    break;
                }

                if (verbose)
                {
                    Debug.Log($"Rotation: {Mathf.Rad2Deg * rotAngle} degrees");
                    Debug.Log($"Translation: ({transX}, {transY})");
                }

                var translationMatrix = Matrix3x2.CreateTranslation(transX.Value, transY.Value);
                var rotationMatrix = Matrix3x2.CreateRotation(rotAngle.Value);
                var rotationTranslationMatrix = rotationMatrix * translationMatrix;

                transformationHistory.Add(rotationTranslationMatrix);

                transformation *= rotationTranslationMatrix;

                points = points.Select(p => p.Transform(rotationTranslationMatrix)).ToList();

                if (Mathf.Abs(rotAngle.Value) < convergenceRotationThreshold &&
                    Mathf.Abs(transX.Value) < convergenceTranslationThreshold &&
                    Mathf.Abs(transY.Value) < convergenceTranslationThreshold)
                {
                    if (verbose)
                        Debug.Log("Converged.");
                    break;
                }
            }

            return (transformationHistory, transformation, points);
        }
    }
}