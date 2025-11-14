using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Script simplu pentru obiecte clickable (activate)
/// Demonstreaza interactiune de tip "click" sau "activate" in VR
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class SimpleClickable : MonoBehaviour
{
    [Header("Click Settings")]
    [SerializeField] private string buttonName = "Button";
    [SerializeField] private Color normalColor = Color.blue;
    [SerializeField] private Color clickedColor = Color.red;
    [SerializeField] private float clickDuration = 0.2f;
    
    [Header("Action")]
    [SerializeField] private bool toggleAction = false;
    [SerializeField] private GameObject targetObject;
    
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private Renderer buttonRenderer;
    private bool isToggled = false;
    private float clickTimer = 0f;
    
    void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        buttonRenderer = GetComponent<Renderer>();
        
        if (buttonRenderer != null)
        {
            buttonRenderer.material.color = normalColor;
        }
    }
    
    void OnEnable()
    {
        interactable.selectEntered.AddListener(OnClick);
    }
    
    void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnClick);
    }
    
    void Update()
    {
        // Timer pentru feedback vizual dupa click
        if (clickTimer > 0)
        {
            clickTimer -= Time.deltaTime;
            if (clickTimer <= 0 && buttonRenderer != null && !isToggled)
            {
                buttonRenderer.material.color = normalColor;
            }
        }
    }
    
    private void OnClick(SelectEnterEventArgs args)
    {
        Debug.Log($"{buttonName} clicked!");
        
        if (toggleAction)
        {
            // Toggle on/off
            isToggled = !isToggled;
            
            if (buttonRenderer != null)
            {
                buttonRenderer.material.color = isToggled ? clickedColor : normalColor;
            }
            
            if (targetObject != null)
            {
                targetObject.SetActive(isToggled);
            }
            
            Debug.Log($"{buttonName} toggled: {isToggled}");
        }
        else
        {
            // Click momentan
            if (buttonRenderer != null)
            {
                buttonRenderer.material.color = clickedColor;
                clickTimer = clickDuration;
            }
            
            // Actiune custom
            PerformAction();
        }
    }
    
    private void PerformAction()
    {
        // Custom action - exemplu: reset game
        InteractionManager manager = FindAnyObjectByType<InteractionManager>();
        if (manager != null && buttonName.Contains("Reset"))
        {
            manager.ResetGame();
        }
        
        // Sau trigger alte actiuni custom
        Debug.Log($"Action performed by {buttonName}");
    }
}

