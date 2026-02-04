using UnityEngine;

/// <summary>
/// Displays player health using the shared HealthBarUI component.
/// Attached to the hand controller to provide visual feedback.
/// </summary>
public class HealthDisplay : MonoBehaviour
{
    [Header("Health Bar")]
    [SerializeField] private HealthBarUI healthBar;
    
    [Header("Auto-setup Settings")]
    [SerializeField] private float width = 200f;
    [SerializeField] private float height = 20f;
    [SerializeField] private Vector3 scale = new Vector3(0.001f, 0.001f, 0.001f);
    
    [SerializeField] private Vector3 offset = new Vector3(0, 0.05f, 0);

    private PlayerHealth playerHealth;
    
    private void Start()
    {
        InitializeHealthBar();
        
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
    
    private void InitializeHealthBar()
    {
        if (healthBar == null)
        {
             healthBar = GetComponent<HealthBarUI>();
             if (healthBar == null)
             {
                 GameObject hbObj = new GameObject("HealthBar");
                 hbObj.transform.SetParent(transform, false);
                 hbObj.transform.localPosition = offset;
                 hbObj.transform.localRotation = Quaternion.identity;
                 hbObj.transform.localScale = scale;
                 
                 healthBar = hbObj.AddComponent<HealthBarUI>();
                 healthBar.Width = width;
                 healthBar.Height = height;
             }
        }
        
        if (healthBar != null)
        {
            healthBar.Initialize();
        }
    }
    
    private void UpdateHealthDisplay(float currentHealth, float maxHealth)
    {
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }
    }
    
    private void Update()
    {
        // Continuously apply settings to support runtime adjustment
        if (healthBar != null)
        {
            // Only update scaling/size if changed (optimization could be added, but simple for now)
            healthBar.Width = width;
            healthBar.Height = height;
            
            // Adjust local scale
            if (healthBar.transform.localScale != scale)
            {
                healthBar.transform.localScale = scale;
            }
            
            // Adjust offset (position)
            if (healthBar.transform.localPosition != offset)
            {
                healthBar.transform.localPosition = offset;
            }
        }
    }
    
    private void OnRespawn()
    {
        if (playerHealth != null)
        {
            UpdateHealthDisplay(playerHealth.GetCurrentHealth(), playerHealth.GetMaxHealth());
        }
    }
}
