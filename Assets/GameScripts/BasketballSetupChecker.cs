using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

/// <summary>
/// Helper script to verify basketball scoring setup is correct.
/// Attach this to any GameObject and check the Console for setup validation messages.
/// </summary>
public class BasketballSetupChecker : MonoBehaviour
{
    [Header("Auto-Check on Start")]
    [Tooltip("Automatically check setup when the scene starts")]
    public bool checkOnStart = true;
    
    [Header("Manual Check")]
    [Tooltip("Assign specific objects to check (optional)")]
    public GameObject basketballRing;
    public BBasketball basketball;
    public KeyboardThrowController throwController;
    
    private void Start()
    {
        if (checkOnStart)
        {
            CheckSetup();
        }
    }
    
    [ContextMenu("Check Basketball Setup")]
    public void CheckSetup()
    {
        Debug.Log("========================================");
        Debug.Log("BASKETBALL SETUP CHECKER - Starting validation...");
        Debug.Log("========================================");
        
        bool allGood = true;
        
        // Check 1: Find all basketballs
        allGood &= CheckBasketballs();
        
        // Check 2: Find score zones
        allGood &= CheckScoreZones();
        
        // Check 3: Check keyboard throw controller
        allGood &= CheckThrowController();
        
        // Check 4: Check UI elements
        allGood &= CheckUI();
        
        // Check 5: Check XR components
        allGood &= CheckXRComponents();
        
        Debug.Log("========================================");
        if (allGood)
        {
            Debug.Log("✅ SETUP CHECK COMPLETE - Everything looks good!");
        }
        else
        {
            Debug.LogWarning("⚠️ SETUP CHECK COMPLETE - Some issues found. See messages above.");
        }
        Debug.Log("========================================");
    }
    
    private bool CheckBasketballs()
    {
        Debug.Log("\n--- Checking Basketballs ---");
        
        BBasketball[] basketballs = FindObjectsOfType<BBasketball>();
        if (basketballs.Length == 0)
        {
            Debug.LogError("❌ No basketballs found! Make sure you have objects with BBasketball script in the scene.");
            return false;
        }
        
        Debug.Log($"✅ Found {basketballs.Length} basketball(s) with BBasketball script");
        
        bool allValid = true;
        for (int i = 0; i < basketballs.Length; i++)
        {
            BBasketball ball = basketballs[i];
            Debug.Log($"\n  Basketball {i + 1}: {ball.gameObject.name}");
            
            // Check tag
            if (!ball.CompareTag("BBasketball"))
            {
                Debug.LogWarning($"  ⚠️ Basketball '{ball.gameObject.name}' doesn't have tag 'BBasketball'! Set Tag to 'BBasketball' in Inspector.");
                allValid = false;
            }
            else
            {
                Debug.Log("  ✅ Tag is correct ('BBasketball')");
            }
            
            // Check for Rigidbody
            Rigidbody[] rbs = ball.GetComponentsInChildren<Rigidbody>();
            if (rbs.Length == 0)
            {
                Debug.LogError($"  ❌ No Rigidbody found on '{ball.gameObject.name}' or its children!");
                allValid = false;
            }
            else
            {
                Debug.Log($"  ✅ Has {rbs.Length} Rigidbody component(s)");
            }
            
            // Check for XRGrabInteractable
            XRGrabInteractable[] grabs = ball.GetComponentsInChildren<XRGrabInteractable>();
            if (grabs.Length == 0)
            {
                Debug.LogWarning($"  ⚠️ No XRGrabInteractable found on '{ball.gameObject.name}'. VR grabbing won't work.");
            }
            else
            {
                Debug.Log($"  ✅ Has {grabs.Length} XRGrabInteractable component(s)");
            }
        }
        
        return allValid;
    }
    
    private bool CheckScoreZones()
    {
        Debug.Log("\n--- Checking Score Zones ---");
        
        ScoreZone[] scoreZones = FindObjectsOfType<ScoreZone>();
        if (scoreZones.Length == 0)
        {
            Debug.LogError("❌ No ScoreZone components found in the scene!");
            Debug.LogError("   Add a child object to your basketball ring with:");
            Debug.LogError("   1. A collider (Sphere or Cylinder)");
            Debug.LogError("   2. 'Is Trigger' checked");
            Debug.LogError("   3. ScoreZone script component");
            return false;
        }
        
        Debug.Log($"✅ Found {scoreZones.Length} ScoreZone(s)");
        
        bool allValid = true;
        for (int i = 0; i < scoreZones.Length; i++)
        {
            ScoreZone zone = scoreZones[i];
            Debug.Log($"\n  ScoreZone {i + 1}: {zone.gameObject.name}");
            
            // Check for trigger collider
            Collider[] colliders = zone.GetComponents<Collider>();
            bool hasTrigger = false;
            foreach (Collider col in colliders)
            {
                if (col.isTrigger)
                {
                    hasTrigger = true;
                    Debug.Log($"  ✅ Has trigger collider ({col.GetType().Name})");
                    break;
                }
            }
            
            if (!hasTrigger)
            {
                Debug.LogError($"  ❌ No trigger collider found on '{zone.gameObject.name}'! Add a collider and check 'Is Trigger'.");
                allValid = false;
            }
            
            // Check UI references
            if (zone.scoreText == null)
            {
                Debug.LogWarning($"  ⚠️ Score Text not assigned. Score won't display on UI.");
            }
            else
            {
                Debug.Log($"  ✅ Score Text assigned: {zone.scoreText.gameObject.name}");
            }
            
            if (zone.pointsText == null)
            {
                Debug.LogWarning($"  ⚠️ Points Text not assigned. Points won't display on UI.");
            }
            else
            {
                Debug.Log($"  ✅ Points Text assigned: {zone.pointsText.gameObject.name}");
            }
        }
        
        return allValid;
    }
    
    private bool CheckThrowController()
    {
        Debug.Log("\n--- Checking Keyboard Throw Controller ---");
        
        KeyboardThrowController[] controllers = FindObjectsOfType<KeyboardThrowController>();
        if (controllers.Length == 0)
        {
            Debug.LogWarning("⚠️ No KeyboardThrowController found in the scene.");
            Debug.LogWarning("   Add KeyboardThrowController component to an object to enable keyboard throwing with 'T' key.");
            return true; // Not critical
        }
        
        Debug.Log($"✅ Found {controllers.Length} KeyboardThrowController(s)");
        
        bool allValid = true;
        for (int i = 0; i < controllers.Length; i++)
        {
            KeyboardThrowController controller = controllers[i];
            Debug.Log($"\n  Controller {i + 1}: {controller.gameObject.name}");
            
            // NOTE: KeyboardThrowController no longer needs a specific basketball assigned!
            // It auto-detects ANY grabbed basketball from the hand controllers.
            Debug.Log($"  ℹ️ Basketball assignment: Not needed (auto-detects grabbed balls)");
            
            if (controller.throwOrigin == null)
            {
                Debug.LogWarning($"  ⚠️ Throw Origin not set. Will try to find Main Camera automatically.");
            }
            else
            {
                Debug.Log($"  ✅ Throw Origin assigned: {controller.throwOrigin.gameObject.name}");
            }
            
            // Check if hand controllers are set or can be found
            if (controller.leftHandController == null && controller.rightHandController == null)
            {
                Debug.LogWarning($"  ⚠️ No hand controllers assigned. Will try to auto-find them.");
            }
            else
            {
                if (controller.leftHandController != null)
                    Debug.Log($"  ✅ Left Hand Controller: {controller.leftHandController.gameObject.name}");
                if (controller.rightHandController != null)
                    Debug.Log($"  ✅ Right Hand Controller: {controller.rightHandController.gameObject.name}");
            }
        }
        
        return allValid;
    }
    
    private bool CheckUI()
    {
        Debug.Log("\n--- Checking UI Elements ---");
        
        TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>();
        Debug.Log($"Found {texts.Length} TextMeshProUGUI element(s) in scene");
        
        bool foundScore = false;
        bool foundPoints = false;
        
        foreach (var text in texts)
        {
            if (text.gameObject.name.ToLower().Contains("hoop") || 
                text.gameObject.name.ToLower().Contains("score"))
            {
                foundScore = true;
                Debug.Log($"  ✅ Potential score text: {text.gameObject.name}");
            }
            if (text.gameObject.name.ToLower().Contains("point"))
            {
                foundPoints = true;
                Debug.Log($"  ✅ Potential points text: {text.gameObject.name}");
            }
        }
        
        if (!foundScore && !foundPoints)
        {
            Debug.LogWarning("  ⚠️ No score UI text elements found. You might want to create Canvas with TextMeshProUGUI elements.");
        }
        
        return true; // Not critical
    }
    
    private bool CheckXRComponents()
    {
        Debug.Log("\n--- Checking XR Components ---");
        
        // Check for XR Origin
        var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogWarning("⚠️ No XR Origin found. VR functionality may not work.");
        }
        else
        {
            Debug.Log($"✅ XR Origin found: {xrOrigin.gameObject.name}");
        }
        
        // Check for XR Device Simulator (for testing)
        var simulator = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator>();
        if (simulator == null)
        {
            Debug.LogWarning("⚠️ No XR Device Simulator found. For testing without VR headset, add the XR Device Simulator prefab.");
        }
        else
        {
            Debug.Log($"✅ XR Device Simulator found: {simulator.gameObject.name}");
        }
        
        // Check for Main Camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("❌ No Main Camera found! Make sure your camera has the 'MainCamera' tag.");
            return false;
        }
        else
        {
            Debug.Log($"✅ Main Camera found: {mainCam.gameObject.name}");
        }
        
        return true;
    }
}

