using UnityEngine;

public class CameraPan : MonoBehaviour
{
    [SerializeField] private float maxPanLimit = 5f;
    
    private Quaternion _initialRotation;

    private void Start()
    {
        _initialRotation = transform.localRotation;
    }

    private void LateUpdate()
    {
        var mousePosition = Input.mousePosition / new Vector2(Screen.width, Screen.height) * 2f - Vector2.one;
        transform.localRotation = _initialRotation * Quaternion.Euler(-mousePosition.y * maxPanLimit, mousePosition.x * maxPanLimit, 0f);
    }
}
