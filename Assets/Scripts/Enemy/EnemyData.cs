using UnityEngine;

/// <summary>
/// ScriptableObject that defines the stats for a specific enemy type variant.
/// Supports data-driven enemy configuration for different speeds, health, damage, and attack ranges.
/// </summary>
public class EnemyData : ScriptableObject
{
    [SerializeField] private string enemyName = "Enemy";
    [SerializeField] private EnemyType enemyType = EnemyType.FastSpider;
    
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Combat")]
    [SerializeField] private float health = 20f;
    [SerializeField] private float maxHealth = 20f;
    [SerializeField] private float attackDamage = 8f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float detectionRadius = 15f;
    
    [Header("Visual")]
    [SerializeField] private Color typeColor = Color.white;
    [SerializeField] private float visualScale = 1f;

    /// <summary>
    /// Gets the name of this enemy variant
    /// </summary>
    public string EnemyName => enemyName;

    /// <summary>
    /// Gets the type of this enemy
    /// </summary>
    public EnemyType Type => enemyType;

    /// <summary>
    /// Gets the movement speed in meters per second
    /// </summary>
    public float MoveSpeed => moveSpeed;

    /// <summary>
    /// Gets the health value
    /// </summary>
    public float Health => health;

    /// <summary>
    /// Gets the max health value
    /// </summary>
    public float MaxHealth => maxHealth;

    /// <summary>
    /// Gets the attack damage value
    /// </summary>
    public float AttackDamage => attackDamage;

    /// <summary>
    /// Gets the attack range in meters
    /// </summary>
    public float AttackRange => attackRange;

    /// <summary>
    /// Gets the attack cooldown in seconds
    /// </summary>
    public float AttackCooldown => attackCooldown;

    /// <summary>
    /// Gets the detection radius in meters
    /// </summary>
    public float DetectionRadius => detectionRadius;

    /// <summary>
    /// Gets the color for visual differentiation
    /// </summary>
    public Color TypeColor => typeColor;

    /// <summary>
    /// Gets the visual scale multiplier (0.7 to 1.3)
    /// </summary>
    public float VisualScale => visualScale;

    /// <summary>
    /// Validates that all values are in reasonable ranges
    /// </summary>
    public bool IsValid()
    {
        if (moveSpeed <= 0)
        {
            Debug.LogWarning($"EnemyData '{enemyName}': moveSpeed must be > 0");
            return false;
        }

        if (health <= 0)
        {
            Debug.LogWarning($"EnemyData '{enemyName}': health must be > 0");
            return false;
        }

        if (maxHealth <= 0)
        {
            Debug.LogWarning($"EnemyData '{enemyName}': maxHealth must be > 0");
            return false;
        }

        if (health > maxHealth)
        {
            Debug.LogWarning($"EnemyData '{enemyName}': health cannot exceed maxHealth");
            return false;
        }

        if (attackDamage <= 0)
        {
            Debug.LogWarning($"EnemyData '{enemyName}': attackDamage must be > 0");
            return false;
        }

        if (attackRange <= 0)
        {
            Debug.LogWarning($"EnemyData '{enemyName}': attackRange must be > 0");
            return false;
        }

        if (attackCooldown <= 0)
        {
            Debug.LogWarning($"EnemyData '{enemyName}': attackCooldown must be > 0");
            return false;
        }

        if (detectionRadius <= 0)
        {
            Debug.LogWarning($"EnemyData '{enemyName}': detectionRadius must be > 0");
            return false;
        }

        if (visualScale < 0.1f || visualScale > 2f)
        {
            Debug.LogWarning($"EnemyData '{enemyName}': visualScale should be between 0.1 and 2.0");
            return false;
        }

        return true;
    }
}
