using UnityEngine;

public class DeleteMe : MonoBehaviour
{
    public Transform referenceObject; // The "parent" object
    public Vector3 initialPositionOffset; // Used if calculateInitialOffset is false
    public Vector3 initialRotationOffset; // Euler angles, used if calculateInitialOffset is false
    public bool calculateInitialOffset = true; // Whether to calculate offset based on initial positions

    private Vector3 positionOffset;
    private Quaternion rotationOffset;

    void Start()
    {
        if (calculateInitialOffset)
        {
            // Calculate initial position offset
            positionOffset = Quaternion.Inverse(referenceObject.rotation) * (transform.position - referenceObject.position);

            // Calculate initial rotation offset
            rotationOffset = Quaternion.Inverse(referenceObject.rotation) * transform.rotation;
        }
        else
        {
            // Use the offsets provided in the inspector
            positionOffset = initialPositionOffset;
            rotationOffset = Quaternion.Euler(initialRotationOffset);
        }
    }

    void Update()
    {
        // Apply position offset
        Vector3 worldPositionOffset = referenceObject.rotation * positionOffset;
        transform.position = referenceObject.position + worldPositionOffset;

        // Apply rotation offset
        transform.rotation = referenceObject.rotation * rotationOffset;
    }
}