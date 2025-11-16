using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Automatically fixes XR Ray Interactor settings
/// - Increases Max Raycast Distance to 100 units
/// - Sets Hit Detection Type to Sphere Cast (can find interactables through geometry)
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor))]
public class FixRayDistance : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxRaycastDistance = 100f;
    [SerializeField] private bool useSphereCast = true;
    [SerializeField] private float sphereCastRadius = 0.1f;
    
    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rayInteractor;
    
    void Awake()
    {
        rayInteractor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
        
        if (rayInteractor != null)
        {
            float oldDistance = rayInteractor.maxRaycastDistance;
            rayInteractor.maxRaycastDistance = maxRaycastDistance;
            Debug.Log($"✓ Fixed Ray Distance on {gameObject.name}: {oldDistance} → {maxRaycastDistance}");
            
            if (useSphereCast)
            {
                // Set Hit Detection Type to Sphere Cast (2)
                // This allows finding interactables even when geometry is in the way
                rayInteractor.hitDetectionType = UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor.HitDetectionType.SphereCast;
                rayInteractor.sphereCastRadius = sphereCastRadius;
                Debug.Log($"✓ Set Hit Detection Type to Sphere Cast (radius: {sphereCastRadius})");
            }
        }
    }
}

