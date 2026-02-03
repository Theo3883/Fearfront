using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Displays a brief red flash overlay when player takes damage.
/// Uses panel show/hide pattern like DeathOverlay for consistency.
/// </summary>
public class DamageFlashOverlay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject flashPanel;
    [SerializeField] private Image redOverlayImage;
    
    [Header("Flash Settings")]
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private float flashAlpha = 0.2f;
    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [Header("Post Processing")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private float vignetteIntensity = 0.25f;
    [SerializeField] private float vignetteSmoothSpeed = 8f;
    
    private PlayerHealth playerHealth;
    private Coroutine currentFlashCoroutine;
    private Coroutine currentVignetteCoroutine;
    private Vignette vignette;
    
    private void Start()
    {
        playerHealth = PlayerHealth.Instance;
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += OnPlayerDamaged;
        }
        else
        {
            Debug.LogError("DamageFlashOverlay: PlayerHealth.Instance not found!");
        }
        
        if (postProcessVolume != null)
        {
            if (postProcessVolume.profile.TryGet(out vignette))
            {
                vignette.active = true;
                vignette.intensity.value = 0f;
            }
        }
        
        // Initialize panel as hidden (like DeathOverlay pattern)
        if (flashPanel != null)
        {
            flashPanel.SetActive(false);
        }
        
        if (redOverlayImage != null)
        {
            Color c = redOverlayImage.color;
            c.a = 0f;
            redOverlayImage.color = c;
        }
    }
    
    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= OnPlayerDamaged;
        }
        
        StopAllCoroutines();
        
        if (vignette != null)
        {
            vignette.intensity.value = 0f;
        }
    }
    
    /// <summary>
    /// Triggered when player health changes.
    /// </summary>
    private void OnPlayerDamaged(float currentHealth, float maxHealth)
    {
        if (currentHealth > 0f && currentHealth < maxHealth)
        {
            TriggerFlash();
        }
    }
    
    /// <summary>
    /// Triggers the damage flash effect.
    /// </summary>
    private void TriggerFlash()
    {
        // Show panel (like DeathOverlay shows overlayPanel)
        if (flashPanel != null)
        {
            flashPanel.SetActive(true);
        }
        
        if (redOverlayImage == null)
            return;
        
        if (currentFlashCoroutine != null)
        {
            StopCoroutine(currentFlashCoroutine);
        }
        
        if (currentVignetteCoroutine != null)
        {
            StopCoroutine(currentVignetteCoroutine);
        }
        
        currentFlashCoroutine = StartCoroutine(FlashRed());
        
        if (vignette != null)
        {
            currentVignetteCoroutine = StartCoroutine(FlashVignette());
        }
    }
    
    /// <summary>
    /// Flash red overlay: fade in quickly, fade out smoothly.
    /// </summary>
    private IEnumerator FlashRed()
    {
        float elapsed = 0f;
        Color c = redOverlayImage.color;
        
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;
            c.a = flashCurve.Evaluate(t) * flashAlpha;
            redOverlayImage.color = c;
            
            yield return null;
        }
        
        c.a = 0f;
        redOverlayImage.color = c;
        currentFlashCoroutine = null;
        
        // Hide panel after flash completes
        HideFlashPanel();
    }
    
    /// <summary>
    /// Flash vignette: fade in quickly, fade out quickly.
    /// Smaller intensity and faster than death overlay.
    /// </summary>
    private IEnumerator FlashVignette()
    {
        // Fade in
        float time = 0f;
        while (time < 1f)
        {
            time += Time.deltaTime * vignetteSmoothSpeed;
            vignette.intensity.value = Mathf.Lerp(0f, vignetteIntensity, time);
            yield return null;
        }
        
        // Fade out
        time = 0f;
        while (time < 1f)
        {
            time += Time.deltaTime * vignetteSmoothSpeed;
            vignette.intensity.value = Mathf.Lerp(vignetteIntensity, 0f, time);
            yield return null;
        }
        
        vignette.intensity.value = 0f;
        currentVignetteCoroutine = null;
        
        // Panel hiding is coordinated by both coroutines
        HideFlashPanel();
    }
    
    /// <summary>
    /// Hides the flash panel when both flash effects are complete.
    /// </summary>
    private void HideFlashPanel()
    {
        // Only hide if both coroutines are done
        if (currentFlashCoroutine == null && currentVignetteCoroutine == null)
        {
            if (flashPanel != null)
            {
                flashPanel.SetActive(false);
            }
        }
    }
}
