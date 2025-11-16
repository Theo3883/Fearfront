using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Automatically sets up spider interaction when spiders are spawned at runtime
/// </summary>
public class RuntimeSpiderSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool setupOnAwake = true;
    
    [Header("Spider Settings")]
    [SerializeField] private bool makeGrabbable = true;
    [SerializeField] private bool destroyWhenThrown = true;
    [SerializeField] private float throwVelocityThreshold = 5f;
    
    [Header("Colors")]
    [SerializeField] private Color hoverColor = new Color(1f, 0f, 0f, 1f); // Red
    [SerializeField] private Color grabbedColor = new Color(0f, 1f, 0f, 1f); // Green
    
    private bool isSetup = false;
    
    void Awake()
    {
        if (setupOnAwake && !isSetup)
        {
            SetupThisSpider();
        }
    }
    
    public void SetupThisSpider()
    {
        if (isSetup)
        {
            Debug.Log($"Spider {gameObject.name} already setup, skipping...");
            return;
        }
        
        Debug.Log($"🕷️ Setting up spider at runtime: {gameObject.name}");
        
        // 1. Add or configure Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.mass = 0.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        // 2. Verify Collider exists and is NOT a trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = GetComponentInChildren<Collider>();
            
            if (col == null)
            {
                // Add capsule collider as fallback
                CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
                capsule.center = Vector3.zero;
                capsule.radius = 0.3f;
                capsule.height = 0.5f;
                capsule.isTrigger = false; // CRITICAL: Must NOT be trigger for XR interactions
                Debug.Log($"  ✓ Added CapsuleCollider to {gameObject.name}");
            }
        }
        
        // CRITICAL: Ensure collider is NOT a trigger
        if (col != null)
        {
            if (col.isTrigger)
            {
                Debug.LogWarning($"  ⚠️ Collider on {gameObject.name} was a TRIGGER! Setting to non-trigger for XR interaction");
                col.isTrigger = false;
            }
            
            // CRITICAL FIX: MeshColliders on dynamic rigidbodies MUST be convex
            if (col is MeshCollider meshCol)
            {
                if (!meshCol.convex)
                {
                    Debug.LogWarning($"  ⚠️ MeshCollider on {gameObject.name} was NOT CONVEX! Setting to convex for dynamic rigidbody");
                    meshCol.convex = true;
                }
                Debug.Log($"  ✓ MeshCollider verified: isTrigger = {col.isTrigger}, convex = {meshCol.convex}");
            }
            else
            {
                Debug.Log($"  ✓ Collider verified: isTrigger = {col.isTrigger} (should be false)");
            }
        }
        
        // 3. Add XR Grab Interactable
        var grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable == null && makeGrabbable)
        {
            grabInteractable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            grabInteractable.throwOnDetach = true;
            grabInteractable.throwSmoothingDuration = 0.25f;
            grabInteractable.throwVelocityScale = 1.5f;
            
            // CRITICAL: Set Interaction Layers to Everything so ray interactor can detect it
            grabInteractable.interactionLayers = -1; // -1 means "Everything" (all bits set)
            
            Debug.Log($"  ✓ Added XRGrabInteractable to {gameObject.name}");
        }
        else if (grabInteractable != null)
        {
            // Ensure Interaction Layers are set correctly even if component already exists
            grabInteractable.interactionLayers = -1; // -1 means "Everything" (all bits set)
            Debug.Log($"  ✓ XRGrabInteractable already exists on {gameObject.name}, set layers to Everything");
        }
        
        // 4. Add SpiderInteractable script
        SpiderInteractable spiderScript = GetComponent<SpiderInteractable>();
        if (spiderScript == null)
        {
            spiderScript = gameObject.AddComponent<SpiderInteractable>();
            Debug.Log($"  ✓ Added SpiderInteractable script to {gameObject.name}");
            
            // Note: We can't set private serialized fields at runtime easily
            // So SpiderInteractable will use its default values
            // Or we can make a version that accepts parameters
        }
        else
        {
            Debug.Log($"  ✓ SpiderInteractable already exists on {gameObject.name}");
        }
        
        isSetup = true;
        Debug.Log($"  Runtime setup complete for {gameObject.name}!");
    }
}

