using System;
using QuestMarkerTracking.Tracking;
using UnityEngine;

namespace QuestMarkerTracking
{
    public class TableHeightCalibration : MonoBehaviour
    {
        public static TableHeightCalibration Instance { get; private set; }
        
        [SerializeField] private GameObject visualPlaneGameObject;
        
        private bool _isSettingFloorHeight;
        private OVRHand _leftHand;
        private OVRHand _rightHand;
        
        public float FloorHeight => transform.position.y;

        private void Awake()
        {
            if (Instance) 
            {
                Debug.LogError("Multiple instances of TableHeightCalibration detected. Destroying the new instance.");
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
        }

        private void Start()
        {
            _leftHand = TrackerSettings.Instance.LeftHand;
            _rightHand = TrackerSettings.Instance.RightHand;
            
            var newHeight = PlayerPrefs.GetFloat("TableHeight", transform.position.y);
            transform.position = new Vector3(transform.position.x, newHeight, transform.position.z);
        }

        private void Update()
        {
            if (_leftHand.IsPressed())
            {
                _isSettingFloorHeight = false;
                visualPlaneGameObject.SetActive(false);
                PlayerPrefs.SetFloat("TableHeight", transform.position.y);
            }

            if (_isSettingFloorHeight)
            {
                var newHeight = _rightHand.transform.position.y;
                transform.position = new Vector3(transform.position.x, newHeight, transform.position.z);
            }
        }

        public void SetTableHeight(bool value)
        {
            _isSettingFloorHeight = true;
            visualPlaneGameObject.SetActive(true);
        }
    }
}
