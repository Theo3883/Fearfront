using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Component that makes the player damageable.
/// Attached to XROrigin (player) and synced with PlayerHealth.
/// Provides damage feedback and tracks damage history for debugging.
/// </summary>
public class PlayerDamage : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private List<DamageEvent> damageHistory;
    
    [Header("Damage Feedback")]
    [SerializeField] private bool enableVisualFeedback = true;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private Color damageFlashColor = Color.red;
    
    private Renderer playerRenderer;
    private Material damageMaterial;
    private Color originalColor;
    private float flashTimer = 0f;

    public struct DamageEvent
    {
        public float damage;
        public float timestamp;
        public Vector3 source;
    }

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        
        if (playerHealth == null)
        {
            Debug.LogError("PlayerDamage requires PlayerHealth component on the same object!");
            enabled = false;
            return;
        }
        
        damageHistory = new List<DamageEvent>();
        
        // Try to get renderer for visual feedback
        if (enableVisualFeedback)
        {
            playerRenderer = GetComponentInChildren<Renderer>();
            if (playerRenderer != null)
            {
                damageMaterial = playerRenderer.material;
                originalColor = damageMaterial.color;
            }
        }
    }

    private void Update()
    {
        // Update visual feedback flash
        if (enableVisualFeedback && flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            
            if (playerRenderer != null && damageMaterial != null)
            {
                // Lerp back to original color
                float progress = 1f - (flashTimer / flashDuration);
                damageMaterial.color = Color.Lerp(damageFlashColor, originalColor, progress);
            }
        }
    }

    /// <summary>
    /// Apply damage to the player
    /// </summary>
    public void TakeDamage(float damageAmount)
    {
        if (!playerHealth.IsAlive())
            return;

        // Apply damage through health system
        playerHealth.Damage(damageAmount);
        
        // Record damage history
        damageHistory.Add(new DamageEvent
        {
            damage = damageAmount,
            timestamp = Time.time,
            source = transform.position
        });
        
        // Visual feedback
        if (enableVisualFeedback)
        {
            PlayDamageFlash();
        }
    }

    /// <summary>
    /// Apply damage with source location (for debugging)
    /// </summary>
    public void TakeDamage(float damageAmount, Vector3 sourcePosition)
    {
        TakeDamage(damageAmount);
        
        // Update last damage event source
        if (damageHistory.Count > 0)
        {
            DamageEvent lastEvent = damageHistory[damageHistory.Count - 1];
            lastEvent.source = sourcePosition;
            damageHistory[damageHistory.Count - 1] = lastEvent;
        }
    }

    /// <summary>
    /// Play visual damage feedback (flash effect)
    /// </summary>
    private void PlayDamageFlash()
    {
        if (playerRenderer != null && damageMaterial != null)
        {
            flashTimer = flashDuration;
            damageMaterial.color = damageFlashColor;
        }
    }

    /// <summary>
    /// Get the PlayerHealth reference
    /// </summary>
    public PlayerHealth GetPlayerHealth()
    {
        return playerHealth;
    }

    /// <summary>
    /// Get damage history for debugging
    /// </summary>
    public List<DamageEvent> GetDamageHistory()
    {
        return new List<DamageEvent>(damageHistory);
    }

    /// <summary>
    /// Clear damage history
    /// </summary>
    public void ClearDamageHistory()
    {
        damageHistory.Clear();
    }

    /// <summary>
    /// Get total damage taken
    /// </summary>
    public float GetTotalDamageTaken()
    {
        float total = 0f;
        foreach (DamageEvent evt in damageHistory)
        {
            total += evt.damage;
        }
        return total;
    }

    /// <summary>
    /// Log damage history for debugging
    /// </summary>
    public void LogDamageHistory()
    {
        Debug.Log($"=== Damage History ({damageHistory.Count} events) ===");
        foreach (DamageEvent evt in damageHistory)
        {
            Debug.Log($"Time: {evt.timestamp:F2}s | Damage: {evt.damage}f | Source: {evt.source}");
        }
        Debug.Log($"Total Damage Taken: {GetTotalDamageTaken()}f");
    }
}
