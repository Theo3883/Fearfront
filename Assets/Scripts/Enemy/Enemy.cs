using UnityEngine;
using UnityEngine.AI;
using System;

/// <summary>
/// Enemy coordinator that manages health, death, and components (EnemyMovement, EnemyStateMachine, NavMeshPlayerDetector).
/// This is a simplified refactoring that delegates movement and state logic to specialized components.
/// </summary>
public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private float healthMax = 20f;
    [SerializeField] private float currentHealth = 20f;
    
    [SerializeField] private NavMeshPlayerDetector playerDetector;
    [SerializeField] private EnemyMovement enemyMovement;
    [SerializeField] private EnemyStateMachine stateMachine;
    private Rigidbody rb;
    private NavMeshAgent agent;
    
    private SpiderDismantle spiderDismantle;
    private bool isDead = false;
    
    private EnemySpawner spawner;
    private Transform[] waypoints;
    
    // ===== Events =====
    public event Action<EnemyState> OnStateChanged;

    private void Awake()
    {
        // Get references to components
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        spiderDismantle = GetComponent<SpiderDismantle>();
        
        // Get component references for coordination
        playerDetector = GetComponent<NavMeshPlayerDetector>();
        enemyMovement = GetComponent<EnemyMovement>();
        stateMachine = GetComponent<EnemyStateMachine>();
        
        if (playerDetector == null)
            Debug.LogError($"Enemy '{gameObject.name}' missing NavMeshPlayerDetector component!");
        if (enemyMovement == null)
            Debug.LogError($"Enemy '{gameObject.name}' missing EnemyMovement component!");
        if (stateMachine == null)
            Debug.LogError($"Enemy '{gameObject.name}' missing EnemyStateMachine component!");
        
        // Wire up state change event forwarding from stateMachine to Enemy
        if (stateMachine != null)
        {
            stateMachine.OnStateChanged += (newState) => OnStateChanged?.Invoke(newState);
        }
    }

    /// <summary>
    /// Sets EnemyData for this enemy instance
    /// Loads health and other stats from data
    /// </summary>
    public void SetEnemyData(EnemyData data)
    {
        enemyData = data;
        if (enemyData != null && enemyData.IsValid())
        {
            healthMax = enemyData.MaxHealth;
            currentHealth = enemyData.Health;
        }
        else
        {
            healthMax = 20f;
            currentHealth = 20f;
        }
    }

    /// <summary>
    /// Initialize the enemy with waypoints and spawner reference
    /// Sets up component dependencies and initial state
    /// </summary>
    public void Initialize(Transform[] path, EnemySpawner enemySpawner)
    {
        waypoints = path;
        spawner = enemySpawner;
        isDead = false;
        
        // Set player reference for detection
        PlayerHealth playerHealth = PlayerHealth.Instance;
        if (playerHealth != null && playerDetector != null)
        {
            Transform playerTransform = playerHealth.transform;
            if (Camera.main != null)
                playerTransform = Camera.main.transform;
            playerDetector.SetPlayerReference(playerTransform);
        }
        
        // Initialize movement with waypoints
        if (enemyMovement != null && waypoints != null && waypoints.Length > 0)
        {
            enemyMovement.Initialize(waypoints, enemyData);
        }
        
        // Initialize state machine with detection range from enemy data or default
        float detectionRadius = 25f;
        if (enemyData != null && enemyData.IsValid())
        {
            detectionRadius = enemyData.DetectionRadius;
        }
        
        if (stateMachine != null && playerDetector != null)
        {
            stateMachine.Initialize(playerDetector, detectionRadius, playerHealth);
        }
    }

    private void Update()
    {
        if (isDead) return;
        
        if (stateMachine == null || playerDetector == null || enemyMovement == null)
        {
        Debug.LogWarning($"Enemy '{gameObject.name}' missing required components");
            return;
        }
        
        enemyMovement.UpdateMovement();
        
        PlayerHealth playerHealth = PlayerHealth.Instance;
        if (playerHealth != null)
        {
            stateMachine.UpdateState(playerHealth.transform.position);
        }
    }

    /// <summary>
    /// Gets the current health of this enemy
    /// </summary>
    public float GetHealth()
    {
        return currentHealth;
    }

    /// <summary>
    /// Gets the max health of this enemy
    /// </summary>
    public float GetMaxHealth()
    {
        return healthMax;
    }

    /// <summary>
    /// Damages the enemy and triggers death if health <= 0
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Kills the enemy, disables all components, and triggers dismantle effect
    /// </summary>
    public void Die()
    {
        if (isDead) return;
        isDead = true;

        DisableAllComponents();

        if (spiderDismantle != null)
        {
            spiderDismantle.ActivateDismantle();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Disables all movement and combat components
    /// </summary>
    private void DisableAllComponents()
    {
        if (enemyMovement != null)
            enemyMovement.enabled = false;
        
        if (stateMachine != null)
            stateMachine.enabled = false;
        
        if (playerDetector != null)
            playerDetector.enabled = false;
        
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.velocity = Vector3.zero;
            agent.enabled = false;
        }
    }

    /// <summary>
    /// Re-enables all components after death (useful for respawning scenarios)
    /// </summary>
    public void ResumeAllComponents()
    {
        if (isDead) return;
        
        if (enemyMovement != null)
            enemyMovement.enabled = true;
        
        if (stateMachine != null)
            stateMachine.enabled = true;
        
        if (playerDetector != null)
            playerDetector.enabled = true;
        
        if (rb != null)
            rb.isKinematic = false;
    }

    // ===== BACKWARD COMPATIBILITY: State transition methods =====
    // These methods are kept for backward compatibility with existing code
    // They delegate to the EnemyStateMachine where actual state logic lives

    /// <summary>
    /// Gets the current state of the enemy (from EnemyStateMachine)
    /// </summary>
    public EnemyState GetCurrentState()
    {
        if (stateMachine != null)
            return stateMachine.CurrentState;
        return EnemyState.Moving;
    }

    /// <summary>
    /// Transitions the enemy to Attacking state
    /// </summary>
    public void TransitionToAttacking()
    {
        if (stateMachine != null)
        {
            stateMachine.ForceStateChange(EnemyState.Attacking);
        }
        else if (enemyMovement != null)
        {
            enemyMovement.PauseMovement();
        }
    }

    /// <summary>
    /// Transitions the enemy to Idle state
    /// </summary>
    public void TransitionToIdle()
    {
        if (stateMachine != null)
        {
            stateMachine.ForceStateChange(EnemyState.Idle);
        }
        else if (enemyMovement != null)
        {
            enemyMovement.PauseMovement();
        }
    }

    /// <summary>
    /// Transitions the enemy to Moving state
    /// </summary>
    public void TransitionToMoving()
    {
        if (stateMachine != null)
        {
            stateMachine.ForceStateChange(EnemyState.Moving);
        }
        else if (enemyMovement != null)
        {
            enemyMovement.ResumeMovement();
        }
    }

    /// <summary>
    /// Transitions the enemy to Stunned state
    /// </summary>
    public void TransitionToStunned()
    {
        if (stateMachine != null)
        {
            stateMachine.ForceStateChange(EnemyState.Stunned);
        }
        else if (enemyMovement != null)
        {
            enemyMovement.PauseMovement();
        }
    }

    /// <summary>
    /// Recovers the enemy from stunned state
    /// </summary>
    public void ResumeFromStun()
    {
        if (stateMachine != null)
        {
            stateMachine.ForceStateChange(EnemyState.Moving);
        }
        else if (enemyMovement != null)
        {
            enemyMovement.ResumeMovement();
        }
    }

    /// <summary>
    /// Sets move speed (for compatibility, delegates to EnemyMovement if possible)
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        // Speed management is now in EnemyMovement
    }

    /// <summary>
    /// Stops movement (for compatibility)
    /// </summary>
    public void StopMoving()
    {
        if (enemyMovement != null)
            enemyMovement.PauseMovement();
    }

    /// <summary>
    /// Resumes movement (for compatibility)
    /// </summary>
    public void ResumeMoving()
    {
        if (enemyMovement != null)
            enemyMovement.ResumeMovement();
    }
}

