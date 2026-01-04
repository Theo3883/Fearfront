using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Manages the death screen overlay with red transparent background and countdown
/// </summary>
public class DeathOverlay : MonoBehaviour
{
    [SerializeField] private GameObject overlayPanel;
    [SerializeField] private Image redOverlayImage;
    [SerializeField] private TextMeshProUGUI countdownText;
    
    [Header("Post Processing")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private float vignetteIntensity = 0.55f;
    [SerializeField] private float vignetteSmoothSpeed = 2f;
    [SerializeField] private float pulseFrequency = 1f;
    [SerializeField] private float pulseAmplitude = 0.1f;
    
    private PlayerHealth playerHealth;
    private Vignette vignette;
    
    private void Start()
    {
        playerHealth = PlayerHealth.Instance;
        if (playerHealth != null)
        {
            playerHealth.OnDeath += ShowDeathOverlay;
            playerHealth.OnRespawn += HideDeathOverlay;
            playerHealth.OnRespawnCountdown += UpdateCountdown;
        }
        else
        {
            Debug.LogError("DeathOverlay: PlayerHealth.Instance not found!");
        }
        
        if (postProcessVolume != null)
        {
            if (postProcessVolume.profile.TryGet(out vignette))
            {
                vignette.active = true;
                vignette.intensity.value = 0f;
            }
        }
        
        HideDeathOverlay();
        ConfigureCanvas();
    }
    
    /// <summary>
    /// Configures the Canvas for proper VR visibility
    /// </summary>
    private void ConfigureCanvas()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            
            if (canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
            }
            
            if (canvas.gameObject.GetComponent<VRHeadFollower>() == null)
            {
                canvas.gameObject.AddComponent<VRHeadFollower>();
            }
            
            // UIAlwaysOnTop logic removed per user request
            
            canvas.sortingOrder = 32767;
            
            if (canvas.transform.localScale == Vector3.one)
            {
                canvas.transform.localScale = Vector3.one * 0.001f;
            }
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDeath -= ShowDeathOverlay;
            playerHealth.OnRespawn -= HideDeathOverlay;
            playerHealth.OnRespawnCountdown -= UpdateCountdown;
        }
    }
    
    /// <summary>
    /// Shows the death overlay when player dies
    /// </summary>
    private void ShowDeathOverlay()
    {
        Debug.Log("DEATH OVERLAY: SHOWING PANEL");
        if (overlayPanel != null)
        {
            overlayPanel.SetActive(true);
        }
        
        if (vignette != null)
        {
            StopAllCoroutines();
            StartCoroutine(PulseVignette());
        }
        
        // Ensure Red Overlay isn't too opaque (blocks vignette)
        if (redOverlayImage != null)
        {
            Color c = redOverlayImage.color;
            c.a = 0.3f; // Reduced from default to let Vignette show through
            redOverlayImage.color = c;
        }
    }
    
    /// <summary>
    /// Hides the death overlay when player respawns
    /// </summary>
    private void HideDeathOverlay()
    {
        // Only disable the panel, NOT the entire script object
        if (overlayPanel != null)
        {
            overlayPanel.SetActive(false);
        }
        
        if (countdownText != null)
        {
            countdownText.text = "";
        }
        
        // Only run coroutine if we are still active
        // If the user put this script ON the overlayPanel, disabling the panel disables this script,
        // causing StartCoroutine to fail. We check first.
        if (vignette != null)
        {
            StopAllCoroutines();
            if (this.isActiveAndEnabled)
            {
                StartCoroutine(FadeOutVignette());
            }
            else
            {
                // If inactive, just reset immediately to avoid error
                vignette.intensity.value = 0f;
            }
        }
    }
    
    /// <summary>
    /// Fades in then pulses the vignette intensity
    /// </summary>
    private IEnumerator PulseVignette()
    {
        // 1. Fade In
        float time = 0f;
        while (vignette.intensity.value < vignetteIntensity)
        {
            time += Time.deltaTime * vignetteSmoothSpeed;
            vignette.intensity.value = Mathf.Lerp(0f, vignetteIntensity, time);
            yield return null;
        }
        
        // 2. Pulse Loop
        float pulsingTime = 0f;
        while (true)
        {
            pulsingTime += Time.deltaTime * pulseFrequency;
            // Sine wave between (Base - Amplitude) and (Base)
            // Mathf.Sin goes -1 to 1. We map it to [0, 1] then scale
            float wave = (Mathf.Sin(pulsingTime) + 1f) * 0.5f; 
            vignette.intensity.value = vignetteIntensity + (wave * pulseAmplitude);
            yield return null;
        }
    }

    /// <summary>
    /// Smoothly fades out vignette
    /// </summary>
    private IEnumerator FadeOutVignette()
    {
        float startIntensity = vignette.intensity.value;
        float time = 0f;
        
        while (time < 1f)
        {
            time += Time.deltaTime * vignetteSmoothSpeed;
            vignette.intensity.value = Mathf.Lerp(startIntensity, 0f, time);
            yield return null;
        }
        
        vignette.intensity.value = 0f;
    }
    
    /// <summary>
    /// Updates the countdown text display
    /// </summary>
    /// <param name="count">Current countdown number (5, 4, 3, 2, or 1)</param>
    private void UpdateCountdown(int count)
    {
        if (countdownText != null)
        {
            countdownText.text = count.ToString();
        }
    }
}
