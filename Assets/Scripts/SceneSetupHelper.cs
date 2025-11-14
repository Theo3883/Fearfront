using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper script pentru setup rapid al scenei VR
/// Folosit pentru debugging și testing rapid
/// </summary>
public class SceneSetupHelper : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool autoSetupOnStart = false;
    
    [Header("References")]
    [SerializeField] private GameObject cubeObject;
    [SerializeField] private GameObject sphereObject;
    [SerializeField] private GameObject cubeZone;
    [SerializeField] private GameObject sphereZone;
    [SerializeField] private GameObject interactionManager;
    
    void Start()
    {
        if (autoSetupOnStart)
        {
            ValidateSetup();
        }
    }
    
    /// <summary>
    /// Validează că toate componentele necesare sunt prezente
    /// </summary>
    public void ValidateSetup()
    {
        Debug.Log("=== Validating VR Scene Setup ===");
        
        int errors = 0;
        int warnings = 0;
        
        // Check Cube
        if (cubeObject == null)
        {
            Debug.LogError("❌ Cube Object is not assigned!");
            errors++;
        }
        else
        {
            ValidateGrabbableObject(cubeObject, "Cube", ref errors, ref warnings);
        }
        
        // Check Sphere
        if (sphereObject == null)
        {
            Debug.LogError("❌ Sphere Object is not assigned!");
            errors++;
        }
        else
        {
            ValidateGrabbableObject(sphereObject, "Sphere", ref errors, ref warnings);
        }
        
        // Check Cube Zone
        if (cubeZone == null)
        {
            Debug.LogError("❌ Cube Zone is not assigned!");
            errors++;
        }
        else
        {
            ValidatePlacementZone(cubeZone, "Cube Zone", ref errors, ref warnings);
        }
        
        // Check Sphere Zone
        if (sphereZone == null)
        {
            Debug.LogError("❌ Sphere Zone is not assigned!");
            errors++;
        }
        else
        {
            ValidatePlacementZone(sphereZone, "Sphere Zone", ref errors, ref warnings);
        }
        
        // Check Interaction Manager
        if (interactionManager == null)
        {
            Debug.LogError("❌ Interaction Manager is not assigned!");
            errors++;
        }
        else
        {
            if (interactionManager.GetComponent<InteractionManager>() == null)
            {
                Debug.LogError("❌ Interaction Manager doesn't have InteractionManager component!");
                errors++;
            }
            else
            {
                Debug.Log("✓ Interaction Manager: OK");
            }
        }
        
        // Check XR Rig
        Unity.XR.CoreUtils.XROrigin xrOrigin = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("❌ No XR Origin found in scene!");
            errors++;
        }
        else
        {
            Debug.Log("✓ XR Origin: Found");
        }
        
        // Summary
        Debug.Log("=== Validation Complete ===");
        Debug.Log($"Errors: {errors}, Warnings: {warnings}");
        
        if (errors == 0 && warnings == 0)
        {
            Debug.Log("✓✓✓ Setup is perfect! Ready to test!");
        }
        else if (errors == 0)
        {
            Debug.Log("✓ Setup is functional but has warnings.");
        }
        else
        {
            Debug.LogWarning("⚠ Setup has errors. Please fix them before testing.");
        }
    }
    
    private void ValidateGrabbableObject(GameObject obj, string name, ref int errors, ref int warnings)
    {
        GrabbableObject grabbable = obj.GetComponent<GrabbableObject>();
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable xrGrab = obj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        Renderer renderer = obj.GetComponent<Renderer>();
        
        if (grabbable == null)
        {
            Debug.LogError($"❌ {name}: Missing GrabbableObject component!");
            errors++;
        }
        
        if (xrGrab == null)
        {
            Debug.LogError($"❌ {name}: Missing XRGrabInteractable component!");
            errors++;
        }
        
        if (rb == null)
        {
            Debug.LogError($"❌ {name}: Missing Rigidbody component!");
            errors++;
        }
        
        if (renderer == null)
        {
            Debug.LogWarning($"⚠ {name}: Missing Renderer - color feedback won't work!");
            warnings++;
        }
        
        if (grabbable != null && xrGrab != null && rb != null)
        {
            Debug.Log($"✓ {name}: All components present");
        }
    }
    
    private void ValidatePlacementZone(GameObject zone, string name, ref int errors, ref int warnings)
    {
        PlacementZone placementZone = zone.GetComponent<PlacementZone>();
        Collider collider = zone.GetComponent<Collider>();
        
        if (placementZone == null)
        {
            Debug.LogError($"❌ {name}: Missing PlacementZone component!");
            errors++;
        }
        
        if (collider == null)
        {
            Debug.LogError($"❌ {name}: Missing Collider component!");
            errors++;
        }
        else if (!collider.isTrigger)
        {
            Debug.LogError($"❌ {name}: Collider must be set as Trigger!");
            errors++;
        }
        
        if (placementZone != null && collider != null && collider.isTrigger)
        {
            Debug.Log($"✓ {name}: All components present and configured correctly");
        }
    }
    
    /// <summary>
    /// Aplică culori recomandare pentru graybox
    /// </summary>
    public void ApplyGrayboxColors()
    {
        Debug.Log("Applying graybox colors...");
        
        if (cubeObject != null)
        {
            SetObjectColor(cubeObject, new Color(0.53f, 0.81f, 0.92f)); // Sky Blue
        }
        
        if (sphereObject != null)
        {
            SetObjectColor(sphereObject, new Color(1f, 0.65f, 0f)); // Orange
        }
        
        if (cubeZone != null)
        {
            SetObjectColor(cubeZone, new Color(0.53f, 0.53f, 0.53f)); // Gray
        }
        
        if (sphereZone != null)
        {
            SetObjectColor(sphereZone, new Color(0.53f, 0.53f, 0.53f)); // Gray
        }
        
        Debug.Log("✓ Colors applied!");
    }
    
    private void SetObjectColor(GameObject obj, Color color)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }
    
    /// <summary>
    /// Print instructiuni in Console
    /// </summary>
    public void PrintInstructions()
    {
        Debug.Log("=== VR PROTOTYPE INSTRUCTIONS ===");
        Debug.Log("1. Grab Cube-ul cu controller-ul (Grip button)");
        Debug.Log("2. Plasează Cube-ul în Cube Zone (zona gri)");
        Debug.Log("3. Grab Sphere-ul cu controller-ul");
        Debug.Log("4. Plasează Sphere-ul în Sphere Zone");
        Debug.Log("5. Când ambele sunt plasate → SUCCESS!");
        Debug.Log("");
        Debug.Log("Visual Feedback:");
        Debug.Log("- Hover: Obiectul devine GALBEN");
        Debug.Log("- Grabbed: Obiectul devine VERDE");
        Debug.Log("- Zone Active: Zona devine CYAN");
        Debug.Log("- Placed: Zona devine VERDE ÎNCHIS");
        Debug.Log("=================================");
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SceneSetupHelper))]
public class SceneSetupHelperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        SceneSetupHelper helper = (SceneSetupHelper)target;
        
        GUILayout.Space(10);
        GUILayout.Label("Quick Actions", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Validate Setup", GUILayout.Height(30)))
        {
            helper.ValidateSetup();
        }
        
        if (GUILayout.Button("Apply Graybox Colors", GUILayout.Height(30)))
        {
            helper.ApplyGrayboxColors();
        }
        
        if (GUILayout.Button("Print Instructions", GUILayout.Height(30)))
        {
            helper.PrintInstructions();
        }
    }
}
#endif

