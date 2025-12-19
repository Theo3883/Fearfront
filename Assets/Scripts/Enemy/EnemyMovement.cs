using UnityEngine;
using UnityEngine.AI;
using System;

/// <summary>
/// EnemyMovement handles waypoint-based navigation for enemies.
/// Manages NavMeshAgent setup, waypoint following, pause/resume logic, and movement events.
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform[] waypoints;
    private EnemyData enemyData;
    private int currentWaypointIndex = 0;
    private bool isMovementPaused = false;

    // NavMesh recovery tracking
    private int navMeshRecoveryAttempts = 0;
    private const int MAX_RECOVERY_ATTEMPTS = 5;

    // Configuration values
    private float moveSpeed = 12f;
    private float stoppingDistance = 2.5f;
    private float waypointSpreadRadius = 5f;
    private float separationRadius = 1.5f;
    private float separationWeight = 1.5f;
    private LayerMask enemyLayer;

    // Events
    public event Action<int> OnWaypointReached;
    public event Action OnFinalWaypointReached;

    // Properties
    public int CurrentWaypointIndex => currentWaypointIndex;
    public bool IsMovementPaused => isMovementPaused;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError($"EnemyMovement on '{gameObject.name}' requires a NavMeshAgent component!");
        }
    }

    /// <summary>
    /// Initialize the movement system with waypoints and enemy data
    /// </summary>
    public void Initialize(Transform[] waypointsArray, EnemyData data)
    {
        waypoints = waypointsArray;
        enemyData = data;
        currentWaypointIndex = 0;
        isMovementPaused = false;
        navMeshRecoveryAttempts = 0;

        if (agent == null)
            return;

        // Warn if enemy data is null
        if (data == null)
        {
            Debug.LogWarning("EnemyMovement initialized with null EnemyData, using default movement speed");
        }
        else
        {
            moveSpeed = data.MoveSpeed;
        }

        // Setup NavMeshAgent parameters
        if (agent.enabled)
        {
            agent.updateRotation = true;
            agent.angularSpeed = 150f;
            agent.autoBraking = false;
            agent.acceleration = UnityEngine.Random.Range(6f, 10f);
            agent.stoppingDistance = stoppingDistance;
            agent.radius = 0.7f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;

            // Configure avoidance based on speed
            ConfigureAvoidanceBasedOnSpeed();

            // Initialize enemy layer for separation logic
            enemyLayer = LayerMask.GetMask("Enemy");
            if (enemyLayer == 0)
            {
                Debug.LogWarning("'Enemy' layer not found. Agent separation may not work correctly. Consider calling SetEnemyLayer() with a valid layer.");
            }

            // Place agent on NavMesh if needed
            if (!agent.isOnNavMesh && waypoints.Length > 0)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(waypoints[0].position, out hit, 10.0f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                    transform.position = hit.position;
                }
            }

            // Set initial destination
            if (agent.isOnNavMesh && waypoints.Length > 1)
            {
                Vector3 randomizedNextWaypoint = GetRandomizedWaypointPosition(waypoints[1].position);
                Vector3 separatedDest = GetSeparatedNavMeshPosition(randomizedNextWaypoint);
                agent.SetDestination(separatedDest);
                currentWaypointIndex = 1;
            }
            else if (agent.isOnNavMesh && waypoints.Length == 1)
            {
                agent.SetDestination(waypoints[0].position);
            }
        }
        else
        {
            // Agent disabled (e.g., in tests without NavMesh)
            // Still set up waypoint index for movement tracking
            if (waypoints.Length > 1)
            {
                currentWaypointIndex = 1;
            }

            // Initialize enemy layer even if agent is disabled
            enemyLayer = LayerMask.GetMask("Enemy");
        }
    }

    /// <summary>
    /// Update movement logic each frame
    /// </summary>
    public void UpdateMovement()
    {
        if (waypoints == null || waypoints.Length == 0 || agent == null)
            return;

        if (isMovementPaused)
            return;

        // Ensure agent is enabled and on NavMesh
        if (!agent.enabled || !agent.isOnNavMesh)
        {
            if (!agent.enabled)
                agent.enabled = true;
            
            if (!agent.isOnNavMesh)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 10.0f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                    navMeshRecoveryAttempts++;
                    
                    if (navMeshRecoveryAttempts >= MAX_RECOVERY_ATTEMPTS)
                    {
                        Debug.LogWarning($"EnemyMovement on '{gameObject.name}' reached max NavMesh recovery attempts ({MAX_RECOVERY_ATTEMPTS}). Triggering final waypoint reached.");
                        OnFinalWaypointReached?.Invoke();
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                // Successfully recovered, reset counter
                navMeshRecoveryAttempts = 0;
            }
        }

        agent.speed = moveSpeed;

        // Check if current waypoint has been reached
        if (!agent.pathPending && agent.hasPath && agent.path != null && 
            agent.path.status == NavMeshPathStatus.PathComplete)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                // Fire waypoint reached event
                OnWaypointReached?.Invoke(currentWaypointIndex);

                currentWaypointIndex++;

                if (currentWaypointIndex < waypoints.Length)
                {
                    // Move to next waypoint
                    Vector3 randomizedWaypoint = GetRandomizedWaypointPosition(waypoints[currentWaypointIndex].position);
                    Vector3 separatedDest = GetSeparatedNavMeshPosition(randomizedWaypoint);
                    agent.SetDestination(separatedDest);
                }
                else
                {
                    // All waypoints completed
                    OnFinalWaypointReached?.Invoke();
                }
            }
        }
    }

    /// <summary>
    /// Pause movement without disabling the NavMeshAgent
    /// </summary>
    public void PauseMovement()
    {
        isMovementPaused = true;
        if (agent != null && agent.enabled)
        {
            agent.velocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Resume movement from paused state
    /// </summary>
    public void ResumeMovement()
    {
        isMovementPaused = false;
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
    /// Returns a NavMesh-valid position near desiredPosition with local separation from nearby agents
    /// </summary>
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
    /// Configure NavMeshAgent avoidance priority based on speed
    /// </summary>
    private void ConfigureAvoidanceBasedOnSpeed()
    {
        if (agent == null) return;
        
        float speedFactor = moveSpeed / 12f;
        agent.avoidancePriority = Mathf.RoundToInt(Mathf.Lerp(70, 30, speedFactor - 0.6f));
        agent.radius = Mathf.Lerp(0.8f, 0.6f, Mathf.Clamp01(speedFactor - 0.6f));
    }

    /// <summary>
    /// Get the current NavMesh recovery attempt count
    /// </summary>
    public int GetNavMeshRecoveryAttempts()
    {
        return navMeshRecoveryAttempts;
    }

    /// <summary>
    /// Get the enemy layer mask used for separation logic
    /// </summary>
    public int GetEnemyLayer()
    {
        return enemyLayer;
    }

    /// <summary>
    /// Set the enemy layer mask for separation logic
    /// </summary>
    public void SetEnemyLayer(int layerMask)
    {
        enemyLayer = layerMask;
    }

    /// <summary>
    /// Set separation behavior parameters at runtime
    /// </summary>
    public void SetSeparationParameters(float radius, float mass, float force)
    {
        separationRadius = radius;
        separationWeight = mass;
        // Note: mass and force parameters stored for future use
    }
