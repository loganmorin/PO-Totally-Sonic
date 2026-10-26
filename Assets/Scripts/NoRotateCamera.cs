using UnityEngine;

public class LockCameraRotation2D : MonoBehaviour
{
    private Quaternion initialRotation;

    void Start()
    {
        initialRotation = transform.rotation;
    }

    void LateUpdate()
    {
        // Forces the camera to maintain its starting rotation regardless of parent rotation
        transform.rotation = initialRotation;
    }
}