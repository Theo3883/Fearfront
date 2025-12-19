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
    
    [SerializeField] private float detectionRadius = 25f;
    [SerializeField] private float attackRange = 3.5f;
    [SerializeField] private float maxPathDistance = 8f;  // If farther than this from waypoint, return to path
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackDamage = 10f;
    
    [SerializeField] private EnemyData enemyData;
    private float healthMax = 20f;
    private float currentHealth = 20f;
    
    // Hysteresis to prevent state oscillation around detection boundary
    private float detectionHysteresis = 3f;
    private bool playerWasInDetectionRange = false;
    
    public event Action<EnemyState> OnStateChanged;
    private EnemyState currentState = EnemyState.Moving;
    private float attackCooldownTimer = 0f;
    // ----------------------------

    private Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private EnemySpawner spawner;
    private Rigidbody rb;
    
    // --- NEW: Reference to Dismantle Script ---
    private SpiderDismantle spiderDismantle; 
    private bool isDead = false;

    // ------------------------------------------
    private NavMeshAgent agent;
    private bool isMoving = true;
    
    // NavMesh recovery tracking
    private int navMeshRecoveryAttempts = 0;
    private const int MAX_RECOVERY_ATTEMPTS = 5;
    private float pathCalculationTimer = 0f;
    private const float PATH_CALCULATION_TIMEOUT = 1f;
    
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
        
        // --- NEW: Grab the component ---
        spiderDismantle = GetComponent<SpiderDismantle>();
        // -------------------------------
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
        // Find player transform - prioritize Camera.main (actual player head position)
        if (Camera.main != null)
        {
            playerTransform = Camera.main.transform;
            if (debugEnable) Debug.Log($"[Enemy] {name} Found player via Camera.main");
        }
        else if (playerHealth != null)
        {
            playerTransform = playerHealth.transform;
            if (debugEnable) Debug.Log($"[Enemy] {name} Found player via PlayerHealth");
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

        // Search for renderer on root and all children
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                Material mat = new Material(renderer.material);
                mat.color = enemyData.TypeColor;
                renderer.material = mat;
            }
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
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Initialize(Transform[] path, EnemySpawner enemySpawner)
    {
        waypoints = path;
        spawner = enemySpawner;
        currentWaypointIndex = 0;
        navMeshRecoveryAttempts = 0; // Reset recovery tracking
        
        isDead = false;
        
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

            // Ensure the NavMeshAgent is placed on the NavMesh. If the sampled
            // startPosition wasn't on the NavMesh, try to find a nearby valid
            // position and warp the agent there. Only give up (and allow
            // ReachedEnd to handle destruction) if no NavMesh position is found.
            if (agent != null)
            {
                NavMeshHit agentHit;
                if (!agent.isOnNavMesh)
                {
                    if (NavMesh.SamplePosition(transform.position, out agentHit, 10.0f, NavMesh.AllAreas))
                    {
                        agent.Warp(agentHit.position);
                    }
                }

                if (agent.isOnNavMesh)
                {
                    if (waypoints.Length > 1)
                    {
                        Vector3 randomizedNextWaypoint = GetRandomizedWaypointPosition(waypoints[1].position);
                        Vector3 separatedDest = GetSeparatedNavMeshPosition(randomizedNextWaypoint);
                        agent.SetDestination(separatedDest);
                        currentWaypointIndex = 1;
                        if (debugEnable) Debug.Log($"[Enemy] {name} Initialized at {startPosition}, heading to waypoint 1");
                    }
                    else
                    {
                        agent.SetDestination(startPosition);
                        if (debugEnable) Debug.Log($"[Enemy] {name} Initialized at {startPosition} (only 1 waypoint)");
                    }
                }
                else
                {
                    Debug.LogWarning($"Enemy '{name}' spawned but could not be placed on NavMesh near {transform.position}. Will attempt again during Update.");
                }
            }
        }
    }

    private void Update()
    {
        // --- NEW: Guard Clause ---
        // If we are dead, do absolutely nothing. Don't move, don't rotate.
        if (isDead) return;
        // -------------------------

        // Check if spider is grabbed - stop movement
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

        // Only move along path if still in Moving state after detection check
        if (currentState == EnemyState.Moving)
        {
            MoveAlongPath();
        }
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
            // Player not available, clear detection state
            playerWasInDetectionRange = false;
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
        
        // Hysteresis logic: only transition if player crosses threshold + buffer
        float triggerDistance = playerWasInDetectionRange ? (detectionRadius + detectionHysteresis) : detectionRadius;
        
        if (distanceToPlayer <= triggerDistance)
        {
            if (!playerWasInDetectionRange)
            {
                if (debugEnable) Debug.Log($"[Enemy] {name} Player detected at {distanceToPlayer:F1}m (threshold {detectionRadius}m), transitioning to attack mode");
                playerWasInDetectionRange = true;
            }
            TransitionToAttacking();
        }
        else
        {
            if (playerWasInDetectionRange && debugEnable)
            {
                Debug.Log($"[Enemy] {name} Player out of detection range ({distanceToPlayer:F1}m > {triggerDistance:F1}m)");
            }
            playerWasInDetectionRange = false;
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

    // ... [MoveAlongPath, UpdateRotationTowardPath, GetLookaheadDirection, GetLookaheadPosition remain unchanged] ...

    private void MoveAlongPath()
    {
        // Validate waypoints
        if (waypoints == null || waypoints.Length == 0)
        {
            if (debugEnable) Debug.LogWarning($"Enemy '{name}' has invalid waypoints, despawning");
            ReachedEnd();
            return;
        }
        
        // If waypoints finished, mark as reached end
        if (currentWaypointIndex >= waypoints.Length)
        {
            if (debugEnable) Debug.Log($"Enemy '{name}' reached end of path (waypoint {currentWaypointIndex}/{waypoints.Length})");
            ReachedEnd();
            return;
        }

        // If agent is missing, consider this instance invalid and end it
        if (agent == null)
        {
            if (debugEnable) Debug.LogWarning($"Enemy '{name}' has no NavMeshAgent, despawning");
            ReachedEnd();
            return;
        }

        // Guard: do not try to access remainingDistance if agent is disabled
        if (!agent.enabled || !agent.isOnNavMesh)
        {
            // Try to get back on NavMesh
            if (!agent.enabled)
            {
                agent.enabled = true;
            }

            if (!agent.isOnNavMesh)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 10.0f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
                else
                {
                    if (debugEnable) Debug.LogWarning($"Enemy '{name}' not on NavMesh at {transform.position} and no nearby NavMesh found.");
                    return;
                }
            }
            return; // Skip this frame, will resume next frame when agent is ready
        }

        agent.speed = moveSpeed;

        // Only treat a waypoint as reached when we have a valid path and the path is complete.
        // This prevents cases where remainingDistance is 0 (no path) and the enemy immediately
        // increments waypoints and despawns.
        if (!agent.pathPending && agent.hasPath && agent.path != null && agent.path.status == NavMeshPathStatus.PathComplete)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                currentWaypointIndex++;

                if (currentWaypointIndex < waypoints.Length)
                {
                    Vector3 randomizedWaypoint = GetRandomizedWaypointPosition(waypoints[currentWaypointIndex].position);
                    Vector3 separatedDest = GetSeparatedNavMeshPosition(randomizedWaypoint);
                    agent.SetDestination(separatedDest);
                    if (debugEnable) Debug.Log($"Enemy '{name}' reached waypoint {currentWaypointIndex - 1}, moving to waypoint {currentWaypointIndex}");
                }
                else
                {
                    if (debugEnable) Debug.Log($"Enemy '{name}' reached final waypoint {currentWaypointIndex - 1}, despawning");
                    ReachedEnd();
                }
                return;
            }
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
    /// When player detected, move toward them but return to path if too far
    /// </summary>
    private void DetectAndAttackPlayer()
    {
        // Find player if not cached
        if (playerTransform == null)
        {
            playerTransform = FindPlayer();
        }
        
        // No player or player dead - resume path
        if (playerTransform == null || playerHealth == null || !playerHealth.IsAlive())
        {
            TransitionToMoving();
            return;
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Check if player left detection range (with hysteresis)
        float exitDistance = detectionRadius + detectionHysteresis;
        if (distanceToPlayer > exitDistance)
        {
            if (debugEnable) Debug.Log($"[Enemy] {name} Player lost (distance {distanceToPlayer:F1}m > {exitDistance:F1}m), resuming path");
            playerWasInDetectionRange = false;
            TransitionToMoving();
            return;
        }
        
        // Player is in detection range
        playerWasInDetectionRange = true;
        
        // Check if too far from current waypoint - if so, return to path
        float distanceToWaypoint = Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position);
        if (distanceToWaypoint > maxPathDistance)
        {
            if (debugEnable) Debug.Log($"[Enemy] {name} Too far from path (distance {distanceToWaypoint:F1}m > {maxPathDistance}m), returning to waypoint");
            TransitionToMoving();
            return;
        }
        
        // In attack range - attack the player
        if (distanceToPlayer <= attackRange)
        {
            if (debugEnable) Debug.Log($"[Enemy] {name} In attack range ({distanceToPlayer:F1}m), attacking");
            
            // Stop NavMeshAgent movement
            if (agent != null && agent.enabled)
            {
                agent.velocity = Vector3.zero;
            }
            
            // Rotate and attack
            RotateTowardsPlayer();
            
            if (attackCooldownTimer <= 0f)
            {
                ExecuteAttack();
                attackCooldownTimer = attackCooldown;
                if (debugEnable) Debug.Log($"[Enemy] {name} Hit player for {attackDamage} damage");
            }
        }
        else
        {
            // In detection range but not attack range - move toward player
            if (debugEnable) Debug.Log($"[Enemy] {name} Chasing player (distance {distanceToPlayer:F1}m)");
            
            // Enable NavMeshAgent for movement
            if (agent != null && !agent.enabled)
            {
                agent.enabled = true;
            }
            
            // Move toward player using NavMeshAgent
            if (agent != null && agent.enabled)
            {
                // Set destination to player position
                agent.SetDestination(playerTransform.position);
                agent.speed = moveSpeed;
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
        // Prefer PlayerHealth instance if available
        if (playerHealth != null)
            return playerHealth.transform;

        // First try to find by tag
        GameObject playerObject = null;
        try { playerObject = GameObject.FindWithTag("Player"); } catch { playerObject = null; }
        if (playerObject != null)
        {
            return playerObject.transform;
        }

        // Try to find a PlayerHealth MonoBehaviour in scene
        var ph = FindObjectOfType<PlayerHealth>();
        if (ph != null)
            return ph.transform;

        // Try common XR origin names used by starter assets
        string[] xrNames = new string[] { "XROrigin", "XR Origin", "XR Origin (XR Rig)", "XR Rig", "XROrigin" };
        foreach (var n in xrNames)
        {
            var go = GameObject.Find(n);
            if (go != null) return go.transform;
        }

        // As a last resort use the main camera (often parented to the rig)
        if (Camera.main != null)
            return Camera.main.transform;

        // Final fallback: search nearby colliders for a tagged player
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
        if (debugEnable) Debug.Log($"[Enemy] {name} Transitioned to Attacking state at {transform.position}");
        
        // Note: agent state is managed in DetectAndAttackPlayer based on distance to player
        // We don't pre-emptively disable it here, allowing chase logic to work
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
        playerWasInDetectionRange = false; // Reset hysteresis on state change
        
        if (debugEnable) Debug.Log($"[Enemy] {name} Transitioned to Moving state at {transform.position}, resuming path at waypoint {currentWaypointIndex}/{waypoints?.Length ?? 0}");
        
        // Enable NavMeshAgent when moving
        if (agent != null)
        {
            if (!agent.isOnNavMesh)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 10.0f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                    if (debugEnable) Debug.Log($"[Enemy] {name} Warped to NavMesh at {hit.position} for path resume");
                }
            }
            
            if (agent.isOnNavMesh && !agent.enabled)
            {
                agent.enabled = true;
                if (debugEnable) Debug.Log($"[Enemy] {name} Enabled agent for path following");
            }
            
            // Resume path to current waypoint
            if (agent.enabled && agent.isOnNavMesh && waypoints != null && currentWaypointIndex < waypoints.Length)
            {
                Vector3 randomizedWaypoint = GetRandomizedWaypointPosition(waypoints[currentWaypointIndex].position);
                Vector3 separatedDest = GetSeparatedNavMeshPosition(randomizedWaypoint);
                agent.SetDestination(separatedDest);
                if (debugEnable) Debug.Log($"[Enemy] {name} Set destination to waypoint {currentWaypointIndex}");
            }
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
    
    // --- ADD THIS NEW METHOD ---
    public void ActivateSelfDestruct(float delay)
    {
        // "Invoke" is a built-in Unity function that runs a method after a delay
        Invoke("Die", delay);
    }
    // ---------------------------
    
    // --- NEW: Health and Death Logic ---
    public void Die()
    {
        if (isDead) return;
        isDead = true;
        isMoving = false;

        // 1. Stop Physics immediately
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true; // Stop the root object from falling/moving
        }

        // 2. Trigger the Dismantle Effect
        if (spiderDismantle != null)
        {
            spiderDismantle.ActivateDismantle();
        }
        else
        {
            // Fallback if you forgot to add the script
            Destroy(gameObject);
        }

        // 3. Disable this script so Update() stops running entirely
        this.enabled = false;
    }
    // -----------------------------------

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