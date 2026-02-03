using UnityEngine;
using TMPro;

/// <summary>
/// Displays player health as a number on the right hand controller.
/// Health value follows hand movement for quick status checks.
/// </summary>
public class HealthDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI healthText;
    
    private PlayerHealth playerHealth;
    
    private void Start()
    {
        playerHealth = PlayerHealth.Instance;
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthDisplay;
            playerHealth.OnRespawn += OnRespawn;
            UpdateHealthDisplay(playerHealth.GetCurrentHealth(), playerHealth.GetMaxHealth());
        }
        else
        {
            Debug.LogError("HealthDisplay: PlayerHealth.Instance not found!");
        }
    }
    
    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthDisplay;
            playerHealth.OnRespawn -= OnRespawn;
        }
    }
    
    /// <summary>
    /// Updates the health display text.
    /// </summary>
    private void UpdateHealthDisplay(float currentHealth, float maxHealth)
    {
        if (healthText != null)
        {
            int healthValue = Mathf.RoundToInt(currentHealth);
            healthText.text = healthValue.ToString();
            
            if (healthValue <= 0)
            {
                healthText.color = Color.red;
            }
            else if (healthValue < maxHealth * 0.3f)
            {
                healthText.color = Color.yellow;
            }
            else
            {
                healthText.color = Color.white;
            }
        }
    }
    
    /// <summary>
    /// Called when player respawns.
    /// </summary>
    private void OnRespawn()
    {
        if (playerHealth != null)
        {
            UpdateHealthDisplay(playerHealth.GetCurrentHealth(), playerHealth.GetMaxHealth());
        }
    }
}
