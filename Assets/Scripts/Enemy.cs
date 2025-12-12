using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float stoppingDistance = 1.0f;
    [SerializeField] private float lookaheadDistance = 3f;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    [SerializeField] private float waypointSpreadRadius = 2f;
    
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackDamage = 10f;
    
    [SerializeField] private EnemyData enemyData;
    private float healthMax = 20f;
    private float currentHealth = 20f;
    
    public event Action<EnemyState> OnStateChanged;
    private EnemyState currentState = EnemyState.Moving;
    private float attackCooldownTimer = 0f;
    
    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private EnemySpawner spawner;
    private Rigidbody rb;
    private NavMeshAgent agent;
    private bool isMoving = true;
    
    private Transform playerTransform;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        
        if (agent == null)
        {
            Debug.LogError($"Enemy prefab '{gameObject.name}' requires a NavMeshAgent component!");
        }
        else
        {
            agent.radius = 0.5f;
            agent.stoppingDistance = stoppingDistance;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.avoidancePriority = UnityEngine.Random.Range(40, 60);
        }
        
        playerHealth = PlayerHealth.Instance;
        
        LoadStatsFromData();
    }

    /// <summary>
    /// Loads enemy stats from EnemyData ScriptableObject
    /// Falls back to serialized values if no data is assigned
    /// </summary>
    private void LoadStatsFromData()
    {
        if (enemyData != null && enemyData.IsValid())
        {
            moveSpeed = enemyData.MoveSpeed;
            healthMax = enemyData.MaxHealth;
            currentHealth = enemyData.Health;
            attackDamage = enemyData.AttackDamage;
            attackRange = enemyData.AttackRange;
            attackCooldown = enemyData.AttackCooldown;
            detectionRadius = enemyData.DetectionRadius;
            
            // Apply visual differentiation
            ApplyVisualDifferentiation();
        }
        else
        {
            // Use default values if no data provided
            healthMax = 20f;
            currentHealth = 20f;
        }
    }

    /// <summary>
    /// Applies visual differentiation based on enemy type (color and scale)
    /// </summary>
    private void ApplyVisualDifferentiation()
    {
        if (enemyData == null)
            return;

        float scale = enemyData.VisualScale;
        transform.localScale = Vector3.one * scale;

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(renderer.material);
            mat.color = enemyData.TypeColor;
            renderer.material = mat;
        }
    }

    /// <summary>
    /// Sets EnemyData for this enemy instance
    /// Useful for dynamically changing enemy type
    /// </summary>
    public void SetEnemyData(EnemyData data)
    {
        enemyData = data;
        LoadStatsFromData();
    }

    /// <summary>
    /// Gets a randomized position near a waypoint for organic spreading
    /// </summary>
    private Vector3 GetRandomizedWaypointPosition(Vector3 basePosition)
    {
        Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * waypointSpreadRadius;
        randomOffset.y = 0;
        
        Vector3 randomizedPosition = basePosition + randomOffset;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomizedPosition, out hit, 10.0f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        
        return basePosition;
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
    /// Damages the enemy
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Enemy dies
    /// </summary>
    private void Die()
    {
        ReachedEnd();
    }

    public void Initialize(Transform[] path, EnemySpawner enemySpawner)
    {
        waypoints = path;
        spawner = enemySpawner;
        currentWaypointIndex = 0;
        
        if (waypoints.Length > 0)
        {
            NavMeshHit hit;
            Vector3 startPosition = waypoints[0].position;
            if (NavMesh.SamplePosition(waypoints[0].position, out hit, 10.0f, NavMesh.AllAreas))
            {
                startPosition = hit.position;
            }
            else
            {
                Debug.LogWarning($"Waypoint 0 not on NavMesh. Enemy may not navigate correctly.");
            }
            
            transform.position = startPosition;
            
            if (agent != null && agent.isOnNavMesh)
            {
                if (waypoints.Length > 1)
                {
                    Vector3 randomizedNextWaypoint = GetRandomizedWaypointPosition(waypoints[1].position);
                    agent.SetDestination(randomizedNextWaypoint);
                    currentWaypointIndex = 1;
                }
                else
                {
                    agent.SetDestination(startPosition);
                }
            }
            
            UpdateRotationTowardPath();
        }
        
    }

    private void Update()
    {
        SpiderInteractable spiderInteractable = GetComponent<SpiderInteractable>();
        if (spiderInteractable != null)
        {
            if (spiderInteractable.IsGrabbed())
            {
                if (currentState != EnemyState.Idle && currentState != EnemyState.Stunned)
                {
                    TransitionToIdle();
                }
                return;
            }
            else if (currentState == EnemyState.Idle)
            {
                TransitionToMoving();
            }
        }
        
        UpdateState();
    }

    /// <summary>
    /// Updates state-specific behavior each frame
    /// </summary>
    private void UpdateState()
    {
        switch (currentState)
        {
            case EnemyState.Moving:
                UpdateMovingState();
                break;
            case EnemyState.Attacking:
                UpdateAttackingState();
                break;
            case EnemyState.Idle:
                // Idle state - no action needed
                break;
            case EnemyState.Stunned:
                // Stunned state - no action needed
                break;
        }
    }

    /// <summary>
    /// Handles behavior in Moving state
    /// </summary>
    private void UpdateMovingState()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;
        
        DetectPlayerInRange();

        MoveAlongPath();
    }

    /// <summary>
    /// Detect if player enters detection range while in Moving state
    /// </summary>
    private void DetectPlayerInRange()
    {
        if (playerTransform == null)
        {
            playerTransform = FindPlayer();
        }
        
        if (playerTransform == null || playerHealth == null || !playerHealth.IsAlive())
        {
            return;
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        if (distanceToPlayer <= detectionRadius)
        {
            TransitionToAttacking();
        }
    }

    /// <summary>
    /// Handles behavior in Attacking state
    /// </summary>
    private void UpdateAttackingState()
    {
        DetectAndAttackPlayer();
        if (attackCooldownTimer > 0)
        {
            attackCooldownTimer -= Time.deltaTime;
        }
    }

    private void MoveAlongPath()
    {
        if (currentWaypointIndex >= waypoints.Length || agent == null || !agent.isOnNavMesh)
        {
            ReachedEnd();
            return;
        }

        agent.speed = moveSpeed;
        if (agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
        {
            currentWaypointIndex++;
            
             if (currentWaypointIndex < waypoints.Length)
            {
                Vector3 randomizedWaypoint = GetRandomizedWaypointPosition(waypoints[currentWaypointIndex].position);
                agent.SetDestination(randomizedWaypoint);
            }
            else
            {
                // Reached end of path
                ReachedEnd();
            }
            return;
        }

        UpdateRotationTowardPath();
    }

    private void UpdateRotationTowardPath()
    {
        Vector3 lookDirection = GetLookaheadDirection();

        if (lookDirection.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            targetRotation *= Quaternion.Euler(rotationOffset);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private Vector3 GetLookaheadDirection()
    {
        Vector3 lookaheadPos = GetLookaheadPosition();
        return (lookaheadPos - transform.position).normalized;
    }

    private Vector3 GetLookaheadPosition()
    {
        if (currentWaypointIndex >= waypoints.Length)
            return waypoints[waypoints.Length - 1].position;

        Vector3 currentPos = transform.position;
        Vector3 currentTarget = waypoints[currentWaypointIndex].position;
        
        float distToCurrentTarget = Vector3.Distance(currentPos, currentTarget);

        if (distToCurrentTarget > lookaheadDistance)
        {
            return currentPos + (currentTarget - currentPos).normalized * lookaheadDistance;
        }

        if (currentWaypointIndex + 1 < waypoints.Length)
        {
            Vector3 nextTarget = waypoints[currentWaypointIndex + 1].position;
            float remainingDist = lookaheadDistance - distToCurrentTarget;
            return currentTarget + (nextTarget - currentTarget).normalized * remainingDist;
        }

        return currentTarget;
    }

    /// <summary>
    /// Detects player within range and attacks if in attack range
    /// </summary>
    private void DetectAndAttackPlayer()
    {
        // Try to find player if not cached
        if (playerTransform == null)
        {
            playerTransform = FindPlayer();
        }
        
        if (playerTransform == null || playerHealth == null || !playerHealth.IsAlive())
        {
            // Player not found or dead - return to moving
            if (currentState == EnemyState.Attacking)
            {
                TransitionToMoving();
            }
            return;
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Check if player is still in detection range
        if (distanceToPlayer > detectionRadius)
        {
            // Player left detection range - return to moving
            TransitionToMoving();
            return;
        }
        
        // Check if player is in attack range
        if (distanceToPlayer <= attackRange)
        {
            // Rotate towards player
            RotateTowardsPlayer();
            
            // Attack if cooldown is ready
            if (attackCooldownTimer <= 0f)
            {
                ExecuteAttack();
                attackCooldownTimer = attackCooldown;
            }
        }
    }

    /// <summary>
    /// Find the player in the scene
    /// </summary>
    private Transform FindPlayer()
    {
        // First try to find by tag
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            return playerObject.transform;
        }
        
        // Fallback: search for XROrigin by name
        Transform xrOrigin = GameObject.Find("XROrigin")?.transform;
        if (xrOrigin != null)
        {
            return xrOrigin;
        }
        
        // Final fallback: use OverlapSphere to detect any collider at player position
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Player"))
            {
                return col.transform;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Rotate enemy to face the player
    /// </summary>
    private void RotateTowardsPlayer()
    {
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
        targetRotation *= Quaternion.Euler(rotationOffset);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Execute attack on player
    /// </summary>
    private void ExecuteAttack()
    {
        if (playerHealth != null && playerHealth.IsAlive())
        {
            playerHealth.Damage(attackDamage);
        }
    }

    #region State Transitions

    /// <summary>
    /// Gets the current state of the enemy
    /// </summary>
    public EnemyState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// Transitions the enemy to Attacking state
    /// </summary>
    public void TransitionToAttacking()
    {
        if (currentState == EnemyState.Attacking)
            return;

        SetState(EnemyState.Attacking);
        
        // Disable NavMeshAgent when attacking
        if (agent != null && agent.isOnNavMesh)
        {
            agent.velocity = Vector3.zero;
            agent.enabled = false;
        }
        
        // Reset attack cooldown
        attackCooldownTimer = attackCooldown;
    }

    /// <summary>
    /// Transitions the enemy to Idle state (e.g., when grabbed)
    /// </summary>
    public void TransitionToIdle()
    {
        if (currentState == EnemyState.Idle)
            return;

        SetState(EnemyState.Idle);
        isMoving = false;
        
        // Disable NavMeshAgent in idle state
        if (agent != null && agent.isOnNavMesh)
        {
            agent.velocity = Vector3.zero;
            agent.enabled = false;
        }
        
        // Clear Rigidbody velocity
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Transitions the enemy to Moving state
    /// </summary>
    public void TransitionToMoving()
    {
        if (currentState == EnemyState.Moving)
            return;

        SetState(EnemyState.Moving);
        
        // Enable NavMeshAgent when moving
        if (agent != null && !agent.enabled && agent.isOnNavMesh)
        {
            agent.enabled = true;
        }
    }

    /// <summary>
    /// Transitions the enemy to Stunned state (complete disable)
    /// </summary>
    public void TransitionToStunned()
    {
        if (currentState == EnemyState.Stunned)
            return;

        SetState(EnemyState.Stunned);
        isMoving = false;
        
        // Disable everything
        if (agent != null && agent.isOnNavMesh)
        {
            agent.velocity = Vector3.zero;
            agent.enabled = false;
        }
        
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Recovers the enemy from stunned state
    /// </summary>
    public void ResumeFromStun()
    {
        if (currentState != EnemyState.Stunned)
            return;

        TransitionToMoving();
    }

    /// <summary>
    /// Internal method to set state and fire event
    /// </summary>
    private void SetState(EnemyState newState)
    {
        currentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    #endregion

    private void ReachedEnd()
    {
        isMoving = false;
        
        // Stop the NavMeshAgent
        if (agent != null && agent.isOnNavMesh)
        {
            agent.velocity = Vector3.zero;
            agent.enabled = false;
        }
        
        // Clear Rigidbody velocity
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
        
        spawner.OnEnemyReachedEnd(this);
        Destroy(gameObject);
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    public void StopMoving()
    {
        // StopMoving should transition based on context
        // For compatibility, transition to Idle (grab scenario)
        // or Attacking (combat scenario - Phase 3)
        TransitionToIdle();
    }

    public void ResumeMoving()
    {
    }
}
