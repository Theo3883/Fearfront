using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Script pentru obiecte care pot fi prinse (grab) in VR
/// Attach la Cube si Sphere pentru functionalitate de grab
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class GrabbableObject : MonoBehaviour
{
    [Header("Object Settings")]
    [SerializeField] private string objectName = "Object";
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color grabbedColor = Color.green;
    
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private Renderer objectRenderer;
    private Color originalColor;
    private bool isGrabbed = false;
    
    void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        objectRenderer = GetComponent<Renderer>();
        
        if (objectRenderer != null)
        {
            originalColor = normalColor;
            objectRenderer.material.color = normalColor;
        }
    }
    
    void OnEnable()
    {
        grabInteractable.selectEntered.AddListener(OnGrab);
        grabInteractable.selectExited.AddListener(OnRelease);
        grabInteractable.hoverEntered.AddListener(OnHoverEnter);
        grabInteractable.hoverExited.AddListener(OnHoverExit);
    }
    
    void OnDisable()
    {
        grabInteractable.selectEntered.RemoveListener(OnGrab);
        grabInteractable.selectExited.RemoveListener(OnRelease);
        grabInteractable.hoverEntered.RemoveListener(OnHoverEnter);
        grabInteractable.hoverExited.RemoveListener(OnHoverExit);
    }
    
    private void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        if (objectRenderer != null)
        {
            objectRenderer.material.color = grabbedColor;
        }
        Debug.Log($"{objectName} grabbed!");
    }
    
    private void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;
        if (objectRenderer != null)
        {
            objectRenderer.material.color = normalColor;
        }
        Debug.Log($"{objectName} released!");
    }
    
    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (!isGrabbed && objectRenderer != null)
        {
            objectRenderer.material.color = hoverColor;
        }
    }
    
    private void OnHoverExit(HoverExitEventArgs args)
    {
        if (!isGrabbed && objectRenderer != null)
        {
            objectRenderer.material.color = normalColor;
        }
    }
    
    public bool IsGrabbed()
    {
        return isGrabbed;
    }
}

