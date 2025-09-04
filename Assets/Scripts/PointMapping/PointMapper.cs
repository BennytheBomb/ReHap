using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using QuestMarkerTracking.Utilities;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace QuestMarkerTracking.PointMapping
{
    public class PointMapper : MonoBehaviour
    {
        [SerializeField] private Transform pointsA;
        [SerializeField] private Transform pointsB;
        private List<Vector2> _alignedPoints;
        private Matrix3x2 _transformation;
        private Quaternion _startRotation;

        private Vector3 _startPosition;

        private void Start()
        {
            Realign();
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Reset"))
            {
                ResetPointsA();
            }

            if (GUILayout.Button("Realign"))
            {
                Realign();
            }

            if (GUILayout.Button("Align with Rotation and Translation"))
            {
                ResetPointsA();

                var center = (from Transform point in pointsA select point.position.RemoveY())
                    .Aggregate((prev, curr) => prev + curr) / pointsA.childCount;
                var centerOffset = center - pointsA.position.RemoveY();
                var centerTransformed = center.Transform(_transformation);
                var newPosition = centerTransformed - centerOffset;

                Debug.DrawLine(centerTransformed, newPosition, Color.green, 2f);

                pointsA.position = newPosition.AddY();

                var rotation = _transformation.GetRotation();
                pointsA.RotateAround(centerTransformed.AddY(), Vector3.up, -Mathf.Rad2Deg * rotation);
            }
        }

        private void Realign()
        {
            var source = (from Transform point in pointsA select point.position.RemoveY()).ToList();
            var target = (from Transform point in pointsB select point.position.RemoveY()).ToList();

            var (transformationHistory, transformation, alignedPoints) =
                MathUtils.Icp(target, source, distanceThreshold: 3f, pointPairsThreshold: 0, verbose: true);

            _startPosition = pointsA.position;
            _startRotation = pointsA.rotation;

            _transformation = transformation;
        }

        private void ResetPointsA()
        {
            pointsA.SetPositionAndRotation(_startPosition, _startRotation);
        }
    }
}