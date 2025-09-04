using System;
using QuestMarkerTracking.Tracking;
using UnityEngine;

namespace QuestMarkerTracking.Mapping
{
    public class QuestTrackerMapping : MonoBehaviour
    {
        private const int MAX_COLLIDERS = 5;

        [SerializeField] private TrackerMapping trackerMapping;
        [SerializeField] private float pinchSelectionRadius = 0.05f;
        [SerializeField] private LayerMask selectionLayerMask;

        private OVRHand _leftHand;
        private OVRHand _rightHand;
        
        private bool _wasLeftHandPressed;
        private bool _wasRightHandPressed;

        private void Start()
        {
            _leftHand = TrackerSettings.Instance.LeftHand;
            _rightHand = TrackerSettings.Instance.RightHand;
        }

        private void Update()
        {
            if (_leftHand.IsPressed() && !_wasLeftHandPressed)
            {
                OnHandPinch(_leftHand);
                _wasLeftHandPressed = true;
            }
            
            if (_leftHand.IsReleased())
            {
                _wasLeftHandPressed = false;
            }
            
            if (_rightHand.IsPressed() && !_wasRightHandPressed)
            {
                OnHandPinch(_rightHand);
                _wasRightHandPressed = true;
            }
            
            if (_rightHand.IsReleased())
            {
                _wasRightHandPressed = false;
            }
        }

        private void OnHandPinch(OVRHand hand)
        {
            Debug.Log("OnHandPinch with: " + hand.name);

            var pinchPosition = hand.PointerPose.position;

            var results = new Collider[MAX_COLLIDERS];
            var size = Physics.OverlapSphereNonAlloc(pinchPosition, pinchSelectionRadius, results, selectionLayerMask);

            Debug.Log("Found " + size + " colliders within pinch radius.");

            Collider closestCollider = null;
            var closestDistance = float.MaxValue;
            for (var i = 0; i < size; i++)
            {
                var result = results[i];

                var distance = Vector3.Distance(pinchPosition, result.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestCollider = result;
                }
            }

            if (closestCollider)
            {
                if (closestCollider.TryGetComponent(out CameraVirtualObject virtualObject))
                {
                    Debug.Log("Selecting virtual object: " + virtualObject.name);

                    trackerMapping.SelectVirtualObject(virtualObject);
                }
            }
        }
    }
}