using UnityEngine;

/// <summary>
/// ScriptableObject that defines the stats for a specific enemy type variant.
/// Supports data-driven enemy configuration for different speeds, health, damage, and attack ranges.
/// </summary>
public class EnemyData : ScriptableObject
{
    [SerializeField] private string enemyName = "Enemy";
    
    [Header("Classification")]
    [SerializeField] private EnemyFamily family = EnemyFamily.Spider;
    [SerializeField] private EnemyVariantType variantType = EnemyVariantType.Normal;
    
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
    [SerializeField] private float visualScaleMultiplier = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip spawnSound;
    [Range(0f, 1f)] [SerializeField] private float spawnVolume = 1f;

    [SerializeField] private AudioClip ambientSound;
    [Range(0f, 1f)] [SerializeField] private float ambientVolume = 0.5f; // Lower default for ambient loop

    [SerializeField] private AudioClip attackSound;
    [Range(0f, 1f)] [SerializeField] private float attackVolume = 1f;

    [SerializeField] private AudioClip deathSound;
    [Range(0f, 1f)] [SerializeField] private float deathVolume = 1f;

    /// <summary>
    /// Gets the name of this enemy variant
    /// </summary>
    public string EnemyName => enemyName;

    /// <summary>
    /// Gets the family of this enemy (Spider, Ghost, etc)
    /// </summary>
    public EnemyFamily Family => family;

    /// <summary>
    /// Gets the generic variant type (Fast, Tank, etc)
    /// </summary>
    public EnemyVariantType VariantType => variantType;

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
    /// Gets the visual scale multiplier (0.1 to 3.0 relative to prefab base)
    /// </summary>
    public float VisualScaleMultiplier => visualScaleMultiplier;

    /// <summary>
    /// Gets the spawn sound clip
    /// </summary>
    public AudioClip SpawnSound => spawnSound;

    /// <summary>
    /// Gets the volume for spawn sound (0-1)
    /// </summary>
    public float SpawnVolume => spawnVolume;

    /// <summary>
    /// Gets the ambient loop sound clip
    /// </summary>
    public AudioClip AmbientSound => ambientSound;

    /// <summary>
    /// Gets the volume for ambient sound (0-1)
    /// </summary>
    public float AmbientVolume => ambientVolume;

    /// <summary>
    /// Gets the attack sound clip
    /// </summary>
    public AudioClip AttackSound => attackSound;

    /// <summary>
    /// Gets the volume for attack sound (0-1)
    /// </summary>
    public float AttackVolume => attackVolume;

    /// <summary>
    /// Gets the death sound clip
    /// </summary>
    public AudioClip DeathSound => deathSound;

    /// <summary>
    /// Gets the volume for death sound (0-1)
    /// </summary>
    public float DeathVolume => deathVolume;

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

        if (visualScaleMultiplier < 0.1f || visualScaleMultiplier > 5f)
        {
            Debug.LogWarning($"EnemyData '{enemyName}': visualScaleMultiplier should be between 0.1 and 5.0");
            return false;
        }

        return true;
    }
}
