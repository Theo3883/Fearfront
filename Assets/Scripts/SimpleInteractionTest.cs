using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// Simple test to check if XR interactions are working
/// Add this to any GameObject in the scene and press 'T' key to test
/// </summary>
public class SimpleInteractionTest : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            Debug.Log("========== INTERACTION TEST START ==========");
            
            // 1. Check for XR Interaction Manager
            var interactionManager = FindAnyObjectByType<XRInteractionManager>();
            if (interactionManager == null)
            {
                Debug.LogError("❌ NO XR INTERACTION MANAGER FOUND!");
                Debug.LogError("   → Add an XR Interaction Manager to your scene (GameObject > XR > Interaction Manager)");
            }
            else
            {
                Debug.Log($"✓ XR Interaction Manager found: {interactionManager.gameObject.name}");
            }
            
            // 2. Check for Ray Interactors
            var rayInteractors = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>(FindObjectsSortMode.None);
            Debug.Log($"✓ Found {rayInteractors.Length} Ray Interactor(s)");
            
            foreach (var ray in rayInteractors)
            {
                Debug.Log($"   Ray Interactor: {ray.gameObject.name}");
                Debug.Log($"     - Enabled: {ray.enabled}");
                Debug.Log($"     - GameObject Active: {ray.gameObject.activeInHierarchy}");
                Debug.Log($"     - Max Distance: {ray.maxRaycastDistance}");
                Debug.Log($"     - Interaction Layers: {ray.interactionLayers.value} (binary: {Convert.ToString(ray.interactionLayers.value, 2).PadLeft(32, '0')})");
                Debug.Log($"     - Raycast Mask: {ray.raycastMask.value} (Unity physics layers)");
                Debug.Log($"     - Hit Detection Type: {ray.hitDetectionType}");
                Debug.Log($"     - Has Line Visual: {ray.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>() != null}");
                Debug.Log($"     - Select Action Reference: {(ray.selectInput != null ? "SET" : "NULL")}");
                
                // Check if hovering anything
                if (ray.hasHover)
                {
                    Debug.Log($"     ✓ Currently HOVERING: {ray.interactablesHovered[0].transform.name}");
                }
                else
                {
                    Debug.Log($"     - Not hovering anything");
                }
                
                // Check if selecting anything
                if (ray.hasSelection)
                {
                    Debug.Log($"     ✓ Currently SELECTING: {ray.interactablesSelected[0].transform.name}");
                }
                else
                {
                    Debug.Log($"     - Not selecting anything");
                }
            }
            
            // 3. Check for Spiders with XRGrabInteractable
            var grabInteractables = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(FindObjectsSortMode.None);
            Debug.Log($"✓ Found {grabInteractables.Length} Grab Interactable(s)");
            
            int spiderCount = 0;
            foreach (var grab in grabInteractables)
            {
                if (grab.name.ToLower().Contains("spider"))
                {
                    spiderCount++;
                    Debug.Log($"   Spider: {grab.gameObject.name}");
                    Debug.Log($"     - Enabled: {grab.enabled}");
                    Debug.Log($"     - GameObject Active: {grab.gameObject.activeInHierarchy}");
                    Debug.Log($"     - Interaction Layers: {grab.interactionLayers.value} (binary: {Convert.ToString(grab.interactionLayers.value, 2).PadLeft(32, '0')})");
                    Collider spiderCol = grab.GetComponent<Collider>();
                    Debug.Log($"     - Has Collider: {spiderCol != null}");
                    if (spiderCol != null)
                    {
                        Debug.Log($"       - Collider Type: {spiderCol.GetType().Name}");
                        Debug.Log($"       - Is Trigger: {spiderCol.isTrigger} (MUST be false!)");
                        Debug.Log($"       - Enabled: {spiderCol.enabled}");
                        
                        // Check if MeshCollider and if it's convex
                        if (spiderCol is MeshCollider meshCol)
                        {
                            Debug.Log($"       - MeshCollider Convex: {meshCol.convex} (MUST be true for dynamic rigidbodies!)");
                            if (!meshCol.convex)
                            {
                                Debug.LogError($"       ❌ MeshCollider is NOT CONVEX! This will prevent interactions!");
                            }
                        }
                    }
                    Debug.Log($"     - GameObject Layer: {grab.gameObject.layer} ({LayerMask.LayerToName(grab.gameObject.layer)})");
                    Debug.Log($"     - Has Rigidbody: {grab.GetComponent<Rigidbody>() != null}");
                    Debug.Log($"     - Is Hovered: {grab.isHovered}");
                    Debug.Log($"     - Is Selected: {grab.isSelected}");
                    
                    // Check if registered with manager
                    if (interactionManager != null)
                    {
                        bool isRegistered = interactionManager.IsRegistered((UnityEngine.XR.Interaction.Toolkit.Interactables.IXRInteractable)grab);
                        Debug.Log($"     - Registered with Manager: {isRegistered}");
                        
                        if (!isRegistered)
                        {
                            Debug.LogWarning($"     ⚠️ Spider {grab.name} is NOT REGISTERED with XR Interaction Manager!");
                        }
                    }
                }
            }
            
            if (spiderCount == 0)
            {
                Debug.LogWarning("⚠️ No spiders found with XRGrabInteractable!");
            }
            
            Debug.Log("========== INTERACTION TEST END ==========");
        }
    }
}

