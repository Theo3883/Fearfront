using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections.Generic;

/// <summary>
/// Centralized VR Highlight Manager - auto-highlights ANY interactable when hovered.
/// Put on one empty GameObject, configure settings, done.
/// </summary>
public class VRHighlightManager : MonoBehaviour
{
    [Header("Highlight Settings")]
    [SerializeField] private Color highlightColor = new Color(0.3f, 0.7f, 1f, 1f);
    [SerializeField] private float emissionIntensity = 0.5f;
    [SerializeField] private float colorTintStrength = 0.4f;
    
    [Header("Outline Settings")]
    [SerializeField] private bool useOutline = true;
    [SerializeField] private float outlineWidth = 0.03f;
    
    [Header("Auto-Detection")]
    [SerializeField] private bool autoFindInteractors = true;
    [SerializeField] private List<XRBaseInteractor> manualInteractors = new List<XRBaseInteractor>();
    
    [Header("Debug")]
    [SerializeField] private bool debugLog = false;
    
    private class HighlightData
    {
        public Renderer[] renderers;
        public Material[][] originalMaterials;
        public List<GameObject> outlineObjects = new List<GameObject>();
        public Color effectiveColor;
        public Material outlineMaterial;
    }
    
    private Dictionary<GameObject, HighlightData> activeHighlights = new Dictionary<GameObject, HighlightData>();
    private List<XRBaseInteractor> allInteractors = new List<XRBaseInteractor>();
    
    private void Start()
    {
        if (autoFindInteractors)
        {
            FindAllInteractors();
        }
        else
        {
            allInteractors.AddRange(manualInteractors);
        }
        
        SubscribeToInteractors();
        
        if (debugLog)
        {
            Debug.Log($"[VRHighlightManager] Initialized with {allInteractors.Count} interactors");
        }
    }
    
    private void FindAllInteractors()
    {
        var rayInteractors = FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var directInteractors = FindObjectsByType<XRDirectInteractor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        
        foreach (var r in rayInteractors) allInteractors.Add(r);
        foreach (var d in directInteractors) allInteractors.Add(d);
        
        if (debugLog)
        {
            Debug.Log($"[VRHighlightManager] Found {rayInteractors.Length} ray + {directInteractors.Length} direct interactors");
        }
    }
    
    private void SubscribeToInteractors()
    {
        foreach (var interactor in allInteractors)
        {
            if (interactor == null) continue;
            interactor.hoverEntered.AddListener(OnHoverEntered);
            interactor.hoverExited.AddListener(OnHoverExited);
        }
    }
    
    private void OnDestroy()
    {
        foreach (var interactor in allInteractors)
        {
            if (interactor == null) continue;
            interactor.hoverEntered.RemoveListener(OnHoverEntered);
            interactor.hoverExited.RemoveListener(OnHoverExited);
        }
        
        foreach (var kvp in activeHighlights)
        {
            RemoveHighlightVisual(kvp.Key, kvp.Value);
        }
        activeHighlights.Clear();
    }
    
    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (args.interactableObject == null) return;
        
        GameObject target = args.interactableObject.transform.gameObject;
        
        if (debugLog)
        {
            Debug.Log($"[VRHighlightManager] Hover ENTERED: {target.name}");
        }
        
        ApplyHighlight(target);
    }
    
    private void OnHoverExited(HoverExitEventArgs args)
    {
        if (args.interactableObject == null) return;
        
        GameObject target = args.interactableObject.transform.gameObject;
        
        if (debugLog)
        {
            Debug.Log($"[VRHighlightManager] Hover EXITED: {target.name}");
        }
        
        RemoveHighlight(target);
    }
    
    private void ApplyHighlight(GameObject target)
    {
        if (target == null) return;
        if (activeHighlights.ContainsKey(target)) return;
        
        var data = new HighlightData();
        data.renderers = target.GetComponentsInChildren<Renderer>(true);
        data.originalMaterials = new Material[data.renderers.Length][];
        
        // Check for custom highlight color provider (e.g., enemies use red)
        var colorProvider = target.GetComponent<IHighlightColorProvider>();
        data.effectiveColor = colorProvider != null ? colorProvider.GetHighlightColor() : highlightColor;
        
        if (debugLog)
        {
            Debug.Log($"[VRHighlightManager] Found {data.renderers.Length} renderers on {target.name}, color: {data.effectiveColor}");
        }
        
        for (int i = 0; i < data.renderers.Length; i++)
        {
            if (data.renderers[i] == null) continue;
            if (data.renderers[i] is ParticleSystemRenderer) continue;
            if (data.renderers[i].GetComponent<CanvasRenderer>() != null) continue;
            
            Material[] mats = data.renderers[i].materials;
            data.originalMaterials[i] = new Material[mats.Length];
            
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j] == null) continue;
                
                data.originalMaterials[i][j] = new Material(mats[j]);
                
                Material mat = mats[j];
                
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", data.effectiveColor * emissionIntensity);
                }
                
                if (mat.HasProperty("_BaseColor"))
                {
                    Color orig = mat.GetColor("_BaseColor");
                    mat.SetColor("_BaseColor", Color.Lerp(orig, data.effectiveColor, colorTintStrength));
                }
                else if (mat.HasProperty("_Color"))
                {
                    Color orig = mat.GetColor("_Color");
                    mat.SetColor("_Color", Color.Lerp(orig, data.effectiveColor, colorTintStrength));
                }
            }
            
            data.renderers[i].materials = mats;
        }
        
        if (useOutline)
        {
            CreateOutlineMeshes(target, data);
        }
        
        activeHighlights[target] = data;
    }
    
    private void RemoveHighlight(GameObject target)
    {
        if (target == null) return;
        if (!activeHighlights.TryGetValue(target, out var data)) return;
        
        RemoveHighlightVisual(target, data);
        activeHighlights.Remove(target);
    }
    
    private void RemoveHighlightVisual(GameObject target, HighlightData data)
    {
        for (int i = 0; i < data.renderers.Length; i++)
        {
            if (data.renderers[i] == null) continue;
            if (data.originalMaterials[i] == null) continue;
            
            data.renderers[i].materials = data.originalMaterials[i];
        }
        
        foreach (var outlineObj in data.outlineObjects)
        {
            if (outlineObj != null)
            {
                Destroy(outlineObj);
            }
        }
        data.outlineObjects.Clear();
        
        // Destroy per-object outline material
        if (data.outlineMaterial != null)
        {
            Destroy(data.outlineMaterial);
            data.outlineMaterial = null;
        }
    }
    
    private void CreateOutlineMeshes(GameObject target, HighlightData data)
    {
        Shader outlineShader = Shader.Find("Custom/OutlineHighlight");
        if (outlineShader == null) return;
        
        // Create per-highlight material so different objects can have different colors
        data.outlineMaterial = new Material(outlineShader);
        data.outlineMaterial.SetColor("_OutlineColor", data.effectiveColor);
        data.outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
        
        foreach (var renderer in data.renderers)
        {
            if (renderer == null) continue;
            if (renderer is ParticleSystemRenderer) continue;
            if (renderer.GetComponent<CanvasRenderer>() != null) continue;
            
            // Handle SkinnedMeshRenderer (Animated Enemies)
            if (renderer is SkinnedMeshRenderer smr && smr.sharedMesh != null)
            {
                // Create a "ghost" SMR that shares the same bones to animate with the original
                GameObject outlineChild = new GameObject(renderer.name + "_Outline");
                outlineChild.transform.SetParent(renderer.transform, false);
                outlineChild.transform.localPosition = Vector3.zero;
                outlineChild.transform.localRotation = Quaternion.identity;
                outlineChild.transform.localScale = Vector3.one;
                
                SkinnedMeshRenderer newSmr = outlineChild.AddComponent<SkinnedMeshRenderer>();
                newSmr.sharedMesh = smr.sharedMesh;
                newSmr.rootBone = smr.rootBone;
                newSmr.bones = smr.bones; // Critical: Share the exact same bone transforms
                newSmr.material = data.outlineMaterial;
                
                // Disable shadows for the outline
                newSmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                newSmr.receiveShadows = false;
                
                data.outlineObjects.Add(outlineChild);
            }
            // Handle MeshRenderer (Static Objects)
            // Skip static meshes for enemies to avoid outlining debug spheres/capsules
            else if (renderer is MeshRenderer)
            {
                bool isEnemy = target.GetComponent<EnemyInteractable>() != null;
                if (isEnemy) continue;

                MeshFilter mf = renderer.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                
                GameObject outlineChild = new GameObject(renderer.name + "_Outline");
                outlineChild.transform.SetParent(renderer.transform, false);
                outlineChild.transform.localPosition = Vector3.zero;
                outlineChild.transform.localRotation = Quaternion.identity;
                outlineChild.transform.localScale = Vector3.one;
                
                MeshFilter newMf = outlineChild.AddComponent<MeshFilter>();
                newMf.sharedMesh = mf.sharedMesh;
                
                MeshRenderer newMr = outlineChild.AddComponent<MeshRenderer>();
                newMr.material = data.outlineMaterial;
                newMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                newMr.receiveShadows = false;
                
                data.outlineObjects.Add(outlineChild);
            }
        }
    }
    
    public void ForceHighlight(GameObject target)
    {
        ApplyHighlight(target);
    }
    
    public void ForceRemoveHighlight(GameObject target)
    {
        RemoveHighlight(target);
    }
}
