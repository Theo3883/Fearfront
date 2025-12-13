using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float stoppingDistance = 2.5f;
    [SerializeField] private float lookaheadDistance = 3f;
    [SerializeField] private Vector3 rotationOffset = new Vector3(0, 90, 0);
    [SerializeField] private float waypointSpreadRadius = 5f;
    
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
    [SerializeField] private Transform modelRoot;
    private Animator animator;
    [SerializeField] private float animationSpeedBaseline = 12f;
    [SerializeField] private float separationRadius = 1.5f;
    [SerializeField] private float separationWeight = 1.5f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private bool debugEnable = true;
    
    private Vector3 modelInitialLocalEuler;

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
            agent.updateRotation = true;
            agent.angularSpeed = 150f;
            agent.autoBraking = false;
            agent.acceleration = UnityEngine.Random.Range(6f, 10f);
            agent.stoppingDistance = stoppingDistance;
            agent.radius = 0.7f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }
        
        playerHealth = PlayerHealth.Instance;
        // Cache player transform if PlayerHealth exists
        if (playerHealth != null)
        {
            playerTransform = playerHealth.transform;
        }
        LoadStatsFromData();
        ConfigureAvoidanceBasedOnSpeed();

        // If no model root specified, use first child (visual model)
        if (modelRoot == null && transform.childCount > 0)
        {
            modelRoot = transform.GetChild(0);
        }
        if (modelRoot != null)
        {
            modelInitialLocalEuler = modelRoot.localEulerAngles;
        }
        // Setup animator random start and speed if available
        SetupAnimation();
    }

    private void LateUpdate()
    {
        // Keep NavMeshAgent controlling transform rotation, but apply rotation offset to visual model
        if (modelRoot != null)
        {
            // Preserve the model's original pitch/roll and apply yaw from agent + Y offset
            float yaw = transform.eulerAngles.y + rotationOffset.y;
            modelRoot.rotation = Quaternion.Euler(modelInitialLocalEuler.x, yaw, modelInitialLocalEuler.z);
        }
    }

    private void SetupAnimation()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        // Start the animation at a random normalized time so multiple spiders are unsynced
        float normalized = UnityEngine.Random.value;
        animator.Play(0, -1, normalized);

        // Match animation speed to movement speed relative to baseline
        animator.speed = moveSpeed / Mathf.Max(0.01f, animationSpeedBaseline);
    }

    /// <summary>
    /// Loads enemy stats from EnemyData ScriptableObject
    /// Falls back to serialized values if no data is assigned
    /// </summary>
    private void ConfigureAvoidanceBasedOnSpeed()
    {
        if (agent == null) return;
        
        float speedFactor = moveSpeed / 12f;
        agent.avoidancePriority = Mathf.RoundToInt(Mathf.Lerp(70, 30, speedFactor - 0.6f));
        agent.radius = Mathf.Lerp(0.8f, 0.6f, Mathf.Clamp01(speedFactor - 0.6f));
    }

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

    // Returns a NavMesh-valid position near desiredPosition, after applying local separation from nearby agents
    private Vector3 GetSeparatedNavMeshPosition(Vector3 desiredPosition)
    {
        // Compute separation offset from nearby enemies
        Vector3 separation = Vector3.zero;
        Collider[] hits = Physics.OverlapSphere(transform.position, separationRadius, enemyLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform == this.transform) continue;
            Vector3 toMe = transform.position - hits[i].transform.position;
            float dist = toMe.magnitude;
            if (dist > 0.001f)
            {
                separation += toMe.normalized / dist; // stronger when closer
            }
        }
        if (separation != Vector3.zero)
        {
            separation = separation.normalized * separationWeight;
            separation.y = 0;
        }

        Vector3 candidate = desiredPosition + separation;

        // Sample candidate to NavMesh so it's valid
        NavMeshHit hit;
        if (NavMesh.SamplePosition(candidate, out hit, 10.0f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        // Fallback: try sampling the original desiredPosition
        if (NavMesh.SamplePosition(desiredPosition, out hit, 10.0f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return transform.position;
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
                    Vector3 separatedDest = GetSeparatedNavMeshPosition(randomizedNextWaypoint);
                    agent.SetDestination(separatedDest);
                    currentWaypointIndex = 1;
                }
                else
                {
                    agent.SetDestination(startPosition);
                }
            }
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
            // If playerHealth exists but transform not found, try to use its transform
            if (playerHealth != null && playerTransform == null)
            {
                playerTransform = playerHealth.transform;
            }
            if (playerTransform == null)
            {
                return;
            }
            return;
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (debugEnable)
        {
            Debug.Log($"[Enemy] {name} playerTransform={(playerTransform!=null)} distance={distanceToPlayer:F2} detectionRadius={detectionRadius:F2}");
        }
        if (distanceToPlayer <= detectionRadius)
        {
            if (debugEnable) Debug.Log($"[Enemy] {name} TransitionToAttacking triggered (distance {distanceToPlayer:F2})");
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
                Vector3 separatedDest = GetSeparatedNavMeshPosition(randomizedWaypoint);
                agent.SetDestination(separatedDest);
            }
            else
            {
                // Reached end of path
                ReachedEnd();
            }
            return;
        }
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
        if (debugEnable)
        {
            Debug.Log($"[Enemy] {name} AttackingCheck distance={distanceToPlayer:F2} detection={detectionRadius:F2} attackRange={attackRange:F2} cooldown={attackCooldownTimer:F2}");
        }
        // Check if player is still in detection range
        if (distanceToPlayer > detectionRadius)
        {
            if (debugEnable) Debug.Log($"[Enemy] {name} Player left detection range");
            TransitionToMoving();
            return;
        }

        // Check if player is in attack range
        if (distanceToPlayer <= attackRange)
        {
            if (debugEnable) Debug.Log($"[Enemy] {name} Player in attack range - rotating/attacking");
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

    private void OnDrawGizmosSelected()
    {
        if (!debugEnable) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, separationRadius);
        if (playerTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, playerTransform.position);
            Gizmos.DrawSphere(playerTransform.position, 0.15f);
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
