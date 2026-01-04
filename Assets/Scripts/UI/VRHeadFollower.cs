using UnityEngine;

/// <summary>
/// Makes the object follow the player's head (camera) with smooth interpolation (soft follow).
/// This reduces motion sickness and jitter compared to hard-parenting.
/// </summary>
public class VRHeadFollower : MonoBehaviour
{
    [Tooltip("Distance from the camera/target to place the object")]
    [SerializeField] private float distance = 0.5f;

    [Tooltip("Optional: specific object to follow. If empty, defaults to Main Camera")]
    [SerializeField] private Transform targetToFollow;
    
    [Tooltip("Smoothing speed for position (higher = faster response)")]
    [SerializeField] private float positionSmoothSpeed = 10f;
    
    [Tooltip("Smoothing speed for rotation (higher = faster response)")]
    [SerializeField] private float rotationSmoothSpeed = 5f;

    private Transform followTarget;

    private void Start()
    {
        // Use override target if assigned, otherwise find Main Camera
        if (targetToFollow != null)
        {
            followTarget = targetToFollow;
        }
        else if (Camera.main != null)
        {
            followTarget = Camera.main.transform;
            
            // Force layer to Default to avoid "UI" layer culling issues in VR cameras
            SetLayerRecursive(gameObject, 0); // 0 is Default layer
        }
        else
        {
            Debug.LogError("VRHeadFollower: No target assigned and no Main Camera found!");
            enabled = false;
        }
    }
    
    // Recursively set layer for all children
    private void SetLayerRecursive(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, newLayer);
        }
    }

    private void LateUpdate()
    {
        if (followTarget == null) return;

        // Calculate target position
        Vector3 targetPosition = followTarget.position + (followTarget.forward * distance);

        // Smoothly interpolate position
        transform.position = Vector3.Lerp(transform.position, targetPosition, positionSmoothSpeed * Time.deltaTime);

        // Smoothly interpolate rotation to face the camera
        Quaternion targetRotation = Quaternion.LookRotation(transform.position - followTarget.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
    }
}
