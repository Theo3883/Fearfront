using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Displays a brief red flash overlay when player takes damage,
/// and a pulsing green overlay when healing.
/// </summary>
public class DamageFlashOverlay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject flashPanel;
    [SerializeField] private Image damageOverlayImage;
    [Tooltip("Assign a green/healing overlay image here")]
    [SerializeField] private Image healingOverlayImage;
    
    [Header("Damage Flash Settings")]
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private float flashAlpha = 0.2f;
    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [Header("Healing Pulse Settings")]
    [SerializeField] private float healingPulseSpeed = 2f;
    [SerializeField] private float healingMinAlpha = 0.1f;
    [SerializeField] private float healingMaxAlpha = 0.3f;
    
    [Header("Post Processing")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private float vignetteIntensity = 0.25f;
    [SerializeField] private float vignetteSmoothSpeed = 8f;
    
    private PlayerHealth playerHealth;
    private Coroutine currentFlashCoroutine;
    private Coroutine currentVignetteCoroutine;
    private Vignette vignette;
    
    private float lastHealth;
    private float lastHealTime;
    private bool isHealing = false;

    private void Start()
    {
        playerHealth = PlayerHealth.Instance;
        if (playerHealth != null)
        {
            lastHealth = playerHealth.GetCurrentHealth();
            playerHealth.OnHealthChanged += OnHealthChanged;
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
        
        // Initialize panel as hidden
        if (flashPanel != null)
        {
            flashPanel.SetActive(false);
        }
        
        // Reset alphas
        if (damageOverlayImage != null) ResetImageAlpha(damageOverlayImage);
        if (healingOverlayImage != null) ResetImageAlpha(healingOverlayImage);
    }
    
    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= OnHealthChanged;
        }
        
        StopAllCoroutines();
        
        if (vignette != null)
        {
            vignette.intensity.value = 0f;
        }
    }
    
    private void Update()
    {
        HandleHealingPulse();
    }

    private void HandleHealingPulse()
    {
        if (healingOverlayImage == null) return;
        
        bool currentlyHealing = (Time.time - lastHealTime < 0.2f);
        
        if (currentlyHealing && flashPanel != null && !flashPanel.activeSelf)
        {
            flashPanel.SetActive(true);
        }

        if (currentlyHealing)
        {
            float pulse = (Mathf.Sin(Time.time * healingPulseSpeed) + 1f) * 0.5f;
            float targetAlpha = Mathf.Lerp(healingMinAlpha, healingMaxAlpha, pulse);
            
            SetImageAlpha(healingOverlayImage, targetAlpha);
            isHealing = true;
        }
        else if (isHealing)
        {
            Color c = healingOverlayImage.color;
            c.a = Mathf.MoveTowards(c.a, 0f, Time.deltaTime * 0.5f); 
            
            healingOverlayImage.color = c;
            
            if (c.a <= 0f)
            {
                SetImageAlpha(healingOverlayImage, 0f);
                isHealing = false;
                HideFlashPanel();
            }
        }
    }

    private void ResetImageAlpha(Image img)
    {
        SetImageAlpha(img, 0f);
    }
    
    private void SetImageAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    /// <summary>
    /// Triggered when player health changes.
    /// Determines if damage or healing occurred.
    /// </summary>
    private void OnHealthChanged(float currentHealth, float maxHealth)
    {
        if (lastHealth == 0 && currentHealth > 0)
        {
            lastHealth = currentHealth;
            return;
        }
        
        if (Time.timeSinceLevelLoad < 0.5f)
        {
            lastHealth = currentHealth;
            return;
        }

        // Calculate delta
        float delta = currentHealth - lastHealth;
        lastHealth = currentHealth;

        if (delta < 0)
        {
            // DAMAGE taken
            if (currentHealth > 0) // Don't flash if dead
            {
                TriggerFlash(damageOverlayImage, true);
                
                if (healingOverlayImage != null)
                {
                    ResetImageAlpha(healingOverlayImage);
                    isHealing = false;
                }
            }
        }
        else if (delta > 0)
        {
            // HEALING received
            if (currentHealth < maxHealth)
            {
                 lastHealTime = Time.time;
            }
            else
            {
                lastHealTime = 0f;
            }
        }
    }
    
    /// <summary>
    /// Triggers the flash effect for the specified image.
    /// </summary>
    private void TriggerFlash(Image targetImage, bool useVignette)
    {
        if (flashPanel != null)
        {
            flashPanel.SetActive(true);
        }
        
        if (targetImage == null)
            return;
        
        if (currentFlashCoroutine != null) StopCoroutine(currentFlashCoroutine);
        if (currentVignetteCoroutine != null) StopCoroutine(currentVignetteCoroutine);
        
        if (damageOverlayImage != null && damageOverlayImage != targetImage) ResetImageAlpha(damageOverlayImage);
        
        currentFlashCoroutine = StartCoroutine(FlashImage(targetImage));
        
        if (useVignette && vignette != null)
        {
            currentVignetteCoroutine = StartCoroutine(FlashVignette());
        }
    }
    
    private IEnumerator FlashImage(Image targetImage)
    {
        float elapsed = 0f;
        Color c = targetImage.color;
        
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;
            c.a = flashCurve.Evaluate(t) * flashAlpha;
            targetImage.color = c;
            
            yield return null;
        }
        
        c.a = 0f;
        targetImage.color = c;
        currentFlashCoroutine = null;
        
        HideFlashPanel();
    }
    
    private IEnumerator FlashVignette()
    {
        float time = 0f;
        while (time < 1f)
        {
            time += Time.deltaTime * vignetteSmoothSpeed;
            vignette.intensity.value = Mathf.Lerp(0f, vignetteIntensity, time);
            yield return null;
        }
        
        time = 0f;
        while (time < 1f)
        {
            time += Time.deltaTime * vignetteSmoothSpeed;
            vignette.intensity.value = Mathf.Lerp(vignetteIntensity, 0f, time);
            yield return null;
        }
        
        vignette.intensity.value = 0f;
        currentVignetteCoroutine = null;
        
        HideFlashPanel();
    }
    
    private void HideFlashPanel()
    {
        if (currentFlashCoroutine == null && currentVignetteCoroutine == null && !isHealing)
        {
            if (flashPanel != null)
            {
                flashPanel.SetActive(false);
            }
        }
    }
}
