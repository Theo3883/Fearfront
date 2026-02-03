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
    }
    
    private Dictionary<GameObject, HighlightData> activeHighlights = new Dictionary<GameObject, HighlightData>();
    private List<XRBaseInteractor> allInteractors = new List<XRBaseInteractor>();
    private Material outlineMaterial;
    
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
        
        if (outlineMaterial != null)
        {
            Destroy(outlineMaterial);
        }
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
        
        if (debugLog)
        {
            Debug.Log($"[VRHighlightManager] Found {data.renderers.Length} renderers on {target.name}");
        }
        
        for (int i = 0; i < data.renderers.Length; i++)
        {
            if (data.renderers[i] == null) continue;
            if (data.renderers[i] is ParticleSystemRenderer) continue;
            if (data.renderers[i] is CanvasRenderer) continue;
            
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
                    mat.SetColor("_EmissionColor", highlightColor * emissionIntensity);
                }
                
                if (mat.HasProperty("_BaseColor"))
                {
                    Color orig = mat.GetColor("_BaseColor");
                    mat.SetColor("_BaseColor", Color.Lerp(orig, highlightColor, colorTintStrength));
                }
                else if (mat.HasProperty("_Color"))
                {
                    Color orig = mat.GetColor("_Color");
                    mat.SetColor("_Color", Color.Lerp(orig, highlightColor, colorTintStrength));
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
    }
    
    private void CreateOutlineMeshes(GameObject target, HighlightData data)
    {
        if (outlineMaterial == null)
        {
            Shader outlineShader = Shader.Find("Custom/OutlineHighlight");
            if (outlineShader == null)
            {
                if (debugLog) Debug.LogWarning("[VRHighlightManager] Outline shader not found");
                return;
            }
            outlineMaterial = new Material(outlineShader);
        }
        
        outlineMaterial.SetColor("_OutlineColor", highlightColor);
        outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
        
        int outlineCount = 0;
        
        foreach (var renderer in data.renderers)
        {
            if (renderer == null) continue;
            if (renderer is ParticleSystemRenderer) continue;
            if (renderer is CanvasRenderer) continue;
            
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
            newMr.material = outlineMaterial;
            
            data.outlineObjects.Add(outlineChild);
            outlineCount++;
        }
        
        if (debugLog)
        {
            Debug.Log($"[VRHighlightManager] Created {outlineCount} outline meshes for {target.name}");
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
