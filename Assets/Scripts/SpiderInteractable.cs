using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Script pentru interactiuni cu paianjenul (spider) in VR
/// Poate fi prins, aruncat, sau defeated prin diverse metode
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class SpiderInteractable : MonoBehaviour
{
    [Header("Spider Settings")]
    [SerializeField] private string spiderName = "Spider";
    [SerializeField] private float health = 1f; // Pentru feature "kill spider"
    
    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.red; // Rosu = pericol
    [SerializeField] private Color grabbedColor = Color.green;
    [SerializeField] private Color damagedColor = Color.yellow;
    
    [Header("Behavior")]
    [SerializeField] private bool destroyWhenThrown = true;
    [SerializeField] private float throwVelocityThreshold = 5f; // Viteza minima pentru "kill"
    [SerializeField] private bool canBeGrabbed = true;
    
    [Header("Effects")]
    [SerializeField] private GameObject destroyEffect; // Particule opționale
    [SerializeField] private AudioClip grabSound;
    [SerializeField] private AudioClip destroySound;
    
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private Renderer spiderRenderer;
    private Rigidbody rb;
    private bool isGrabbed = false;
    private bool isDestroyed = false;
    private AudioSource audioSource;
    
    void Awake()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        spiderRenderer = GetComponentInChildren<Renderer>(); // GetComponentInChildren pentru a prinde render-ul din spider model
        rb = GetComponent<Rigidbody>();
        
        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f; // 3D sound
        
        // Setup initial color
        if (spiderRenderer != null && spiderRenderer.material != null)
        {
            // Creaza o copie a materialului pentru a evita schimbarea tuturor spiderilor
            spiderRenderer.material = new Material(spiderRenderer.material);
            // Nu schimba culoarea initiala, pastreaza textura spider-ului
        }
        
        // Setup rigidbody
        if (rb != null)
        {
            rb.mass = 0.5f; // Spiderii sunt usori
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        
        // Enable/disable grab
        if (grabInteractable != null)
        {
            grabInteractable.enabled = canBeGrabbed;
        }
        else
        {
            Debug.LogError($"❌ SpiderInteractable on {gameObject.name}: XRGrabInteractable component is NULL!");
        }
    }
    
    void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
            grabInteractable.hoverEntered.AddListener(OnHoverEntered);
            grabInteractable.hoverExited.AddListener(OnHoverExited);
        }
    }
    
    void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
            grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
            grabInteractable.hoverExited.RemoveListener(OnHoverExited);
        }
    }
    
    private void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        
        // Visual feedback
        SetSpiderColor(grabbedColor);
        
        // Audio feedback
        PlaySound(grabSound);
    }
    
    private void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;
        
        // Check if thrown with enough force
        if (destroyWhenThrown && rb != null)
        {
            float velocity = rb.linearVelocity.magnitude;
            
            if (velocity > throwVelocityThreshold)
            {
                DestroySpider();
            }
            else
            {
                SetSpiderColor(normalColor);
            }
        }
        else
        {
            SetSpiderColor(normalColor);
        }
    }
    
    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        
        if (!isGrabbed)
        {
            SetSpiderColor(hoverColor);
        }
    }
    
    private void OnHoverExited(HoverExitEventArgs args)
    {
        
        if (!isGrabbed)
        {
            SetSpiderColor(normalColor);
        }
    }
    
    private void SetSpiderColor(Color color)
    {
        if (spiderRenderer != null && spiderRenderer.material != null)
        {
            // Optiune 1: Schimba culoarea (functioneaza cu Standard Shader)
            spiderRenderer.material.color = color;
            
            // Optiune 2: Schimba emission pentru highlight mai vizibil
            spiderRenderer.material.SetColor("_EmissionColor", color * 0.5f);
            spiderRenderer.material.EnableKeyword("_EMISSION");
        }
    }
    
    public void DamageSpider(float damage)
    {
        if (isDestroyed) return;
        
        health -= damage;
        
        if (health <= 0)
        {
            DestroySpider();
        }
        else
        {
            // Flash damaged color
            SetSpiderColor(damagedColor);
            Invoke(nameof(ResetColor), 0.2f);
        }
    }
    
    private void ResetColor()
    {
        SetSpiderColor(normalColor);
    }
    
    private void DestroySpider()
    {
        if (isDestroyed) return;
        
        isDestroyed = true;
        
        // Play destroy sound
        PlaySound(destroySound);
        
        // Spawn destroy effect
        if (destroyEffect != null)
        {
            Instantiate(destroyEffect, transform.position, Quaternion.identity);
        }
        
        // Destroy the spider
        Destroy(gameObject, 0.1f);
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // Collision detection (optional - pentru a detecta impact cu obiecte)
    private void OnCollisionEnter(Collision collision)
    {
        // Exemplu: Spider moare daca loveste un anumit obiect
        if (collision.relativeVelocity.magnitude > throwVelocityThreshold)
        {
            // Optional: Destroy on hard impact
            // DestroySpider();
        }
    }
    
    // Public methods pentru control extern
    public bool IsGrabbed()
    {
        return isGrabbed;
    }
    
    public bool IsDestroyed()
    {
        return isDestroyed;
    }
    
    public void SetGrabbable(bool canGrab)
    {
        canBeGrabbed = canGrab;
        if (grabInteractable != null)
        {
            grabInteractable.enabled = canGrab;
        }
    }
}

