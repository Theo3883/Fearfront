using UnityEngine;
using System;

/// <summary>
/// Singleton system for managing player health.
/// Handles damage, death, respawn, and provides events for UI and other systems.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;
    
    // Immunity system
    [SerializeField] private float immunityDuration = 3f;
    private bool isImmune = false;
    private float immunityTimer = 0f;
    
    public event Action<float, float> OnHealthChanged; // (currentHealth, maxHealth)
    public event Action OnDeath;
    public event Action OnRespawn;
    public event Action OnImmunityStarted;
    public event Action OnImmunityEnded;
    public event Action<int> OnRespawnCountdown; // Fires with countdown: 3, 2, 1
    
    // Singleton instance
    private static PlayerHealth instance;
    public static PlayerHealth Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<PlayerHealth>();
                if (instance == null)
                {
                    Debug.LogError("PlayerHealth singleton not found in scene!");
                }
            }
            return instance;
        }
    }
    
    private Vector3 spawnPosition;
    private bool isDead = false;
    
    /// <summary>
    /// Public property to check if player is currently immune to damage
    /// </summary>
    public bool IsImmune => isImmune;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        
        currentHealth = maxHealth;
        spawnPosition = transform.position;
        isDead = false;
        isImmune = false;
    }
    
    private void Update()
    {
        // Handle immunity timer
        if (isImmune)
        {
            immunityTimer -= Time.deltaTime;
            if (immunityTimer <= 0f)
            {
                isImmune = false;
                OnImmunityEnded?.Invoke();
            }
        }
    }

    /// <summary>
    /// Get current health value
    /// </summary>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    /// <summary>
    /// Get maximum health value
    /// </summary>
    public float GetMaxHealth()
    {
        return maxHealth;
    }

    /// <summary>
    /// Set maximum health (resets current health to max)
    /// </summary>
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Apply damage to the player
    /// </summary>
    public void Damage(float amount)
    {
        // Don't take damage if dead or immune
        if (isDead || isImmune)
            return;

        currentHealth -= amount;
        if (currentHealth < 0f)
            currentHealth = 0f;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Heal the player
    /// </summary>
    public void Heal(float amount)
    {
        if (isDead)
            return;

        currentHealth += amount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Check if player is alive
    /// </summary>
    public bool IsAlive()
    {
        return !isDead && currentHealth > 0f;
    }

    /// <summary>
    /// Handle player death
    /// </summary>
    private void Die()
    {
        isDead = true;
        OnDeath?.Invoke();
        
        DisablePlayerMovement();
        DisablePlayerInteractions();
        StartCoroutine(RespawnCountdown());
    }
    
    /// <summary>
    /// Countdown coroutine for respawning the player
    /// </summary>
    private System.Collections.IEnumerator RespawnCountdown()
    {
        // Fire countdown events for UI
        OnRespawnCountdown?.Invoke(5);
        yield return new WaitForSeconds(1f);
        
        OnRespawnCountdown?.Invoke(4);
        yield return new WaitForSeconds(1f);
        
        OnRespawnCountdown?.Invoke(3);
        yield return new WaitForSeconds(1f);
        
        OnRespawnCountdown?.Invoke(2);
        yield return new WaitForSeconds(1f);
        
        OnRespawnCountdown?.Invoke(1);
        yield return new WaitForSeconds(1f);
        
        Respawn(spawnPosition);
        StartImmunityPeriod(immunityDuration);
    }
    
    /// <summary>
    /// Starts immunity period for specified duration
    /// </summary>
    private void StartImmunityPeriod(float duration)
    {
        isImmune = true;
        immunityTimer = duration;
        OnImmunityStarted?.Invoke();
    }

    /// <summary>
    /// Disable player movement systems
    /// </summary>
    private void DisablePlayerMovement()
    {
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }
        
        var locomotionProviders = GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider>();
        foreach (var provider in locomotionProviders)
        {
            provider.enabled = false;
        }
    }
    
    /// <summary>
    /// Disable player interactions while dead
    /// </summary>
    private void DisablePlayerInteractions()
    {
        var interactors = GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor>();
        foreach (var interactor in interactors)
        {
            if (interactor is MonoBehaviour mb)
            {
                mb.enabled = false;
            }
        }
    }

    /// <summary>
    /// Disable all spawned enemies
    /// </summary>
    private void DisableEnemies()
    {
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Debug.Log($"<color=yellow>PLAYER DIED: Destroying all {allEnemies.Length} enemies</color>");
        foreach (Enemy enemy in allEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
    }

    /// <summary>
    /// Reset game state on death
    /// </summary>
    private void ResetGameState()
    {
        // TODO: Could be extended for additional reset logic
        // For now, just notify systems through events
    }

    /// <summary>
    /// Respawn the player at a spawn position with full health
    /// </summary>
    public void Respawn(Vector3 respawnPosition)
    {
        currentHealth = maxHealth;
        isDead = false;
        
        transform.position = respawnPosition;
        spawnPosition = respawnPosition;
        
        EnablePlayerMovement();
        EnablePlayerInteractions();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnRespawn?.Invoke();
    }

    /// <summary>
    /// Enable player movement systems
    /// </summary>
    private void EnablePlayerMovement()
    {
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = true;
        }
        
        var locomotionProviders = GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider>();
        foreach (var provider in locomotionProviders)
        {
            provider.enabled = true;
        }
    }
    
    /// <summary>
    /// Enable player interactions after respawn
    /// </summary>
    private void EnablePlayerInteractions()
    {
        var interactors = GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor>();
        foreach (var interactor in interactors)
        {
            if (interactor is MonoBehaviour mb)
            {
                mb.enabled = true;
            }
        }
    }

    /// <summary>
    /// Get the spawn position for respawn
    /// </summary>
    public Vector3 GetSpawnPosition()
    {
        return spawnPosition;
    }
}
