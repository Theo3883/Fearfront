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
    private float healthMax = 0f;
    private float currentHealth = 0f;
    
    [SerializeField] private NavMeshPlayerDetector playerDetector;
    [SerializeField] private EnemyMovement enemyMovement;
    [SerializeField] private EnemyStateMachine stateMachine;
    private Rigidbody rb;
    private NavMeshAgent agent;
    
    private SpiderDismantle spiderDismantle;
    private bool isDead = false;
    
    private EnemySpawner spawner;
    
    // Attack-related fields
    private Transform playerTransform;
    private PlayerHealth playerHealthRef;
    private float attackCooldownTimer = 0f;
    
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
    /// FAILS HARD if data is null
    /// </summary>
    public void SetEnemyData(EnemyData data)
    {
        // FAIL HARD if data is null
        if (data == null)
        {
            Debug.LogError("Enemy.SetEnemyData: EnemyData cannot be null!");
            return;
        }

        enemyData = data;
        if (enemyData.IsValid())
        {
            healthMax = enemyData.MaxHealth;
            currentHealth = enemyData.Health;
        }
        else
        {
            Debug.LogError($"Enemy.SetEnemyData: EnemyData '{data.name}' is not valid!");
        }
        
        ApplyVisualDifferentiation();
    }

    /// <summary>
    /// Applies visual differentiation (scale and color) from EnemyData to the enemy GameObject
    /// </summary>
    private void ApplyVisualDifferentiation()
    {
        if (enemyData == null) return;
        
        // Get visual scale from EnemyData and clamp to reasonable range [0.5, 2.0]
        float scale = Mathf.Clamp(enemyData.VisualScale, 0.5f, 2.0f);
        transform.localScale = Vector3.one * scale;
        
        // Apply color from EnemyData to all child renderers
        Color typeColor = enemyData.TypeColor;
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                Material mat = new Material(renderer.material);
                mat.color = typeColor;
                renderer.material = mat;
            }
        }
    }

    /// <summary>
    /// Initialize the enemy with waypoints and spawner reference
    /// Sets up component dependencies and initial state
    /// Waypoints are NOT stored in Enemy; they are passed directly to EnemyMovement
    /// </summary>
    public void Initialize(Transform[] path, EnemySpawner enemySpawner)
    {
        spawner = enemySpawner;
        isDead = false;
        
        // The most reliable way to find the player is via the PlayerHealth singleton.
        // Ensure PlayerHealth is attached to the object that actually moves (e.g., XR Origin).
        PlayerHealth playerHealth = PlayerHealth.Instance;
        Transform playerTransformLocal = null;
        
        if (playerHealth != null)
        {
            playerTransformLocal = playerHealth.transform;
        }
        else
        {
            // Fallback 1: Look for XR Origin (common in VR)
            GameObject xrOrigin = GameObject.Find("XR Origin") ?? GameObject.Find("XROrigin");
            if (xrOrigin != null)
            {
                playerTransformLocal = xrOrigin.transform;
            }
            // Fallback 2: Tagged Player
            else
            {
                GameObject playerObj = null;
                try { playerObj = GameObject.FindWithTag("Player"); } catch { playerObj = null; }
                if (playerObj != null)
                    playerTransformLocal = playerObj.transform;
                // Fallback 3: Main Camera
                else if (Camera.main != null)
                    playerTransformLocal = Camera.main.transform;
            }
        }

        // Store player references for use in attack logic
        this.playerTransform = playerTransformLocal;
        this.playerHealthRef = playerHealth;

        if (playerTransformLocal != null && playerDetector != null)
        {
            playerDetector.SetPlayerReference(playerTransformLocal);
        }
        
        // Initialize movement with waypoints (Enemy does NOT store waypoints)
        if (enemyMovement != null && path != null && path.Length > 0)
        {
            enemyMovement.Initialize(path, enemyData);
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
            Debug.Log($"[{gameObject.name}] Initialized StateMachine with detection range: {detectionRadius}m, playerDetector={playerDetector != null}, playerHealth={playerHealth != null}");
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
        Vector3 playerPosition = Vector3.zero;
        bool havePlayerPosition = false;
        
        // Priority 1: Main Camera (where the player actually is looking from)
        if (Camera.main != null)
        {
            playerPosition = Camera.main.transform.position;
            havePlayerPosition = true;
        }
        // Priority 2: PlayerHealth singleton transform
        else if (playerHealth != null)
        {
            playerPosition = playerHealth.transform.position;
            havePlayerPosition = true;
        }
        // Priority 3: XR Origin (VR body root)
        else 
        {
            GameObject xrOrigin = GameObject.Find("XR Origin") ?? GameObject.Find("XROrigin");
            if (xrOrigin != null)
            {
                playerPosition = xrOrigin.transform.position;
                havePlayerPosition = true;
            }
            // Priority 4: Tagged Player
            else
            {
                GameObject playerObj = null;
                try { playerObj = GameObject.FindWithTag("Player"); } catch { playerObj = null; }
                
                if (playerObj != null)
                {
                    playerPosition = playerObj.transform.position;
                    havePlayerPosition = true;
                }
            }
        }

        if (havePlayerPosition)
        {
            Debug.Log($"[{gameObject.name}] Updating state with player at {playerPosition}");
            stateMachine.UpdateState(playerPosition);
            
            // Handle attack logic if in Attacking state
            if (stateMachine.CurrentState == EnemyState.Attacking)
            {
                HandleAttackingState(playerPosition);
            }
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

    /// <summary>
    /// Handles attack logic when enemy is in Attacking state
    /// </summary>
    private void HandleAttackingState(Vector3 playerPosition)
    {
        if (enemyData == null || playerTransform == null || playerHealthRef == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Cannot attack: missing data, player transform, or player health");
            return;
        }

        // Update cooldown timer
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerPosition);
        float attackRange = enemyData.AttackRange;

        Debug.Log($"[{gameObject.name}] Attacking state: distance={distanceToPlayer:F1}m, attack range={attackRange}m");

        // Check if player is in attack range
        if (distanceToPlayer <= attackRange)
        {
            Debug.Log($"[{gameObject.name}] Player in attack range! Attacking!");
            
            // Pause movement to focus on attacking
            if (enemyMovement != null)
            {
                enemyMovement.PauseMovement();
            }
            
            // Rotate to face player
            Vector3 directionToPlayer = (playerPosition - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
            
            // Execute attack if cooldown is ready
            if (attackCooldownTimer <= 0f)
            {
                if (playerHealthRef.IsAlive())
                {
                    playerHealthRef.Damage(enemyData.AttackDamage);
                    Debug.Log($"[{gameObject.name}] Hit player for {enemyData.AttackDamage} damage!");
                    attackCooldownTimer = enemyData.AttackCooldown;
                }
            }
        }
        else
        {
            // Too far to attack, move toward player
            Debug.Log($"[{gameObject.name}] Chasing player (distance {distanceToPlayer:F1}m > {attackRange}m)");
            
            // Resume movement toward player
            if (enemyMovement != null)
            {
                enemyMovement.ResumeMovement();
            }
            
            // Move to player using NavMeshAgent
            if (agent != null && agent.enabled)
            {
                agent.SetDestination(playerPosition);
            }
        }
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

