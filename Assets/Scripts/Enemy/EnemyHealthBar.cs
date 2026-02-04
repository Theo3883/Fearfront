using UnityEngine;

/// <summary>
/// Displays a floating health bar above the enemy when damaged.
/// Uses the shared HealthBarUI component.
/// </summary>
[RequireComponent(typeof(Enemy))]
public class EnemyHealthBar : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0); // Above head
    [SerializeField] private Vector3 scale = new Vector3(0.003f, 0.003f, 0.003f);
    [SerializeField] private float visibleDuration = 5f;
    [SerializeField] private float width = 400f;
    [SerializeField] private float height = 40f;

    private Enemy enemy;
    private HealthBarUI healthBar;
    private GameObject healthBarObj;
    private float hideTimer = 0f;
    private Camera mainCam;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        CreateHealthBar();
    }

    private void OnEnable()
    {
        if (enemy != null)
        {
            enemy.OnHealthChanged += OnHealthChanged;
            enemy.OnDeath += HandleDeath;
        }
        
        // Hide by default
        if (healthBar != null) healthBar.SetVisible(false);
    }

    private void OnDisable()
    {
        if (enemy != null)
        {
            enemy.OnHealthChanged -= OnHealthChanged;
            enemy.OnDeath -= HandleDeath;
        }
    }

    private void HandleDeath()
    {
        if (healthBar != null) healthBar.SetVisible(false);
        this.enabled = false;
    }

    private void Start()
    {
        mainCam = Camera.main;
    }

    private void Update()
    {
        if (EnemyVisualsConfig.Instance != null && healthBarObj != null)
        {
            healthBarObj.transform.localPosition = EnemyVisualsConfig.Instance.HealthBarOffset;
            healthBarObj.transform.localScale = EnemyVisualsConfig.Instance.HealthBarScale;
        }

        // Handle visibility timer
        if (hideTimer > 0)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0)
            {
                if (healthBar != null) healthBar.SetVisible(false);
            }
            else
            {
                // Billboard: Face the camera
                if (mainCam != null && healthBarObj != null)
                {
                    // Rotate to look at camera
                    healthBarObj.transform.LookAt(healthBarObj.transform.position + mainCam.transform.rotation * Vector3.forward,
                                             mainCam.transform.rotation * Vector3.up);
                }
            }
        }
    }

    private void OnHealthChanged(float current, float max)
    {
        if (healthBar == null) return;

        // Show bar
        healthBar.SetVisible(true);
        hideTimer = visibleDuration;

        // Update fill
        healthBar.UpdateHealth(current, max);
    }

    private void CreateHealthBar()
    {
        // Detect optimal height based on collider
        float dynamicHeight = offset.y;
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            float topY = col.bounds.max.y;
            float rootY = transform.position.y;
            
            float heightDiff = topY - rootY;
            if (heightDiff > 0.1f)
            {
                dynamicHeight = heightDiff + 0.5f; // reduced margin to 0.5
            }
        }
        
        Vector3 finalOffset = new Vector3(0, dynamicHeight, 0);

        // 1. Create a container object for offset/rotation
        healthBarObj = new GameObject("HealthBarContainer");
        healthBarObj.transform.SetParent(this.transform, false);
        healthBarObj.transform.localPosition = finalOffset;
        healthBarObj.transform.localScale = scale;
        
        // 2. Add HealthBarUI component
        healthBar = healthBarObj.AddComponent<HealthBarUI>();
        healthBar.Width = width;
        healthBar.Height = height;
        
        // Initialize explicitly to ensure everything is ready
        healthBar.Initialize();
        healthBar.SetVisible(false); // Start hidden
    }
}
