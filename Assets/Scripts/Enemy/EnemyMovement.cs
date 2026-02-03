using UnityEngine;
using UnityEngine.AI;
using System;

/// <summary>
/// EnemyMovement handles waypoint-based navigation for enemies.
/// Simple, robust implementation: enemies follow exact waypoints and never leave the path.
/// </summary>
public class EnemyMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform[] waypoints;
    private EnemyData enemyData;
    private int currentWaypointIndex = 0;
    private bool isMovementPaused = false;
    private bool isInitialized = false;

    // Configuration
    private float moveSpeed = 0f;
    private const float STOPPING_DISTANCE = 0.8f;
    private const float NAVMESH_SAMPLE_DISTANCE = 5f;

    // Events
    public event Action<int> OnWaypointReached;
    public event Action OnFinalWaypointReached;

    // Properties
    public int CurrentWaypointIndex => currentWaypointIndex;
    public bool IsMovementPaused => isMovementPaused;
    public bool IsInitialized => isInitialized;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError($"EnemyMovement on '{gameObject.name}' requires a NavMeshAgent component!");
        }
    }

    /// <summary>
    /// Initialize the movement system with waypoints and enemy data.
    /// Warps enemy to first waypoint and sets destination to second waypoint.
    /// </summary>
    public void Initialize(Transform[] waypointsArray, EnemyData data)
    {
        if (data == null)
        {
            Debug.LogError($"EnemyMovement on '{gameObject.name}' requires EnemyData!");
            return;
        }

        if (waypointsArray == null || waypointsArray.Length < 2)
        {
            Debug.LogError($"EnemyMovement on '{gameObject.name}' requires at least 2 waypoints!");
            return;
        }

        waypoints = waypointsArray;
        enemyData = data;
        isMovementPaused = false;
        isInitialized = true;

        // Load speed from EnemyData
        moveSpeed = data.MoveSpeed;

        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null) return;
        }

        // Configure NavMeshAgent for smooth path following
        ConfigureAgent();

        // Warp to first waypoint position on NavMesh
        WarpToWaypoint(0);

        // Set destination to second waypoint (index 1)
        currentWaypointIndex = 1;
        SetDestinationToCurrentWaypoint();
    }

    /// <summary>
    /// Configure NavMeshAgent for optimal path following and avoidance.
    /// </summary>
    private void ConfigureAgent()
    {
        if (agent == null)
            return;

        // Core settings
        agent.stoppingDistance = STOPPING_DISTANCE;
        agent.autoBraking = true;
        agent.speed = moveSpeed;

        // Improved obstacle avoidance to prevent enemies pushing each other
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.radius = 0.6f; // Slightly larger radius for better spacing
        
        // Priority: faster enemies have higher priority (lower number = higher priority)
        // Range: 30-50 (was 40-60, tightened range for more consistent behavior)
        float speedNormalized = Mathf.Clamp01(moveSpeed / 10f);
        agent.avoidancePriority = Mathf.RoundToInt(Mathf.Lerp(50, 30, speedNormalized));
    }

    /// <summary>
    /// Warps the agent to a waypoint position, ensuring it's on NavMesh.
    /// </summary>
    private void WarpToWaypoint(int waypointIndex)
    {
        if (waypoints == null || waypointIndex < 0 || waypointIndex >= waypoints.Length)
            return;

        if (agent == null || waypoints[waypointIndex] == null)
            return;

        Vector3 targetPosition = waypoints[waypointIndex].position;

        // Find valid NavMesh position near the waypoint
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, NAVMESH_SAMPLE_DISTANCE, NavMesh.AllAreas))
        {
            if (!agent.enabled)
                agent.enabled = true;

            agent.Warp(hit.position);
            transform.position = hit.position;
        }
        else
        {
            Debug.LogWarning($"EnemyMovement on '{gameObject.name}': Could not find NavMesh near waypoint {waypointIndex}");
            transform.position = targetPosition;
        }
    }

    /// <summary>
    /// Sets the agent destination to the current target waypoint.
    /// </summary>
    private void SetDestinationToCurrentWaypoint()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        if (waypoints == null || currentWaypointIndex >= waypoints.Length)
            return;

        if (waypoints[currentWaypointIndex] == null)
            return;

        Vector3 targetPosition = waypoints[currentWaypointIndex].position;

        // Sample position to ensure it's on NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, NAVMESH_SAMPLE_DISTANCE, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // Fallback: set destination directly
            agent.SetDestination(targetPosition);
        }
    }

    /// <summary>
    /// Update movement logic each frame. Call from Enemy.Update().
    /// </summary>
    public void UpdateMovement()
    {
        if (!isInitialized || waypoints == null || waypoints.Length == 0)
            return;

        if (isMovementPaused)
            return;

        if (agent == null || !agent.enabled)
            return;

        // Ensure agent is on NavMesh
        if (!agent.isOnNavMesh)
        {
            // Try to recover by warping to current waypoint
            if (currentWaypointIndex > 0 && currentWaypointIndex < waypoints.Length)
            {
                WarpToWaypoint(currentWaypointIndex - 1);
                SetDestinationToCurrentWaypoint();
            }
            return;
        }

        // Update speed in case it changed
        agent.speed = moveSpeed;

        // Check if reached current waypoint
        // Important: Check hasPath to ensure remainingDistance is valid
        if (agent.hasPath && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            // Double-check we're actually close to the waypoint position
            if (currentWaypointIndex < waypoints.Length && waypoints[currentWaypointIndex] != null)
            {
                float distanceToWaypoint = Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position);
                
                // Only advance if we're actually close to the waypoint (prevents teleporting)
                if (distanceToWaypoint <= agent.stoppingDistance + 1f)
                {
                    // Fire waypoint reached event
                    OnWaypointReached?.Invoke(currentWaypointIndex);

                    // Advance to next waypoint
                    currentWaypointIndex++;

                    if (currentWaypointIndex < waypoints.Length)
                    {
                        // Continue to next waypoint
                        SetDestinationToCurrentWaypoint();
                    }
                    else
                    {
                        // Reached final waypoint - notify and stop
                        OnFinalWaypointReached?.Invoke();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Pauses movement. Enemy stays in place but agent remains enabled.
    /// </summary>
    public void PauseMovement()
    {
        isMovementPaused = true;
        
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
        }
    }

    /// <summary>
    /// Resumes movement toward the current waypoint.
    /// </summary>
    public void ResumeMovement()
    {
        isMovementPaused = false;
        
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            SetDestinationToCurrentWaypoint();
        }
    }

    /// <summary>
    /// Gets the position of a specific waypoint.
    /// </summary>
    public Vector3 GetWaypointPosition(int index)
    {
        if (waypoints == null || index < 0 || index >= waypoints.Length)
            return transform.position;

        if (waypoints[index] == null)
            return transform.position;

        return waypoints[index].position;
    }

    /// <summary>
    /// Gets the total number of waypoints.
    /// </summary>
    public int GetWaypointCount()
    {
        return waypoints?.Length ?? 0;
    }

    /// <summary>
    /// Gets the position of the next waypoint (current target).
    /// </summary>
    public Vector3 GetCurrentTargetPosition()
    {
        return GetWaypointPosition(currentWaypointIndex);
    }

    // ===== Backward Compatibility Methods =====
    // These methods maintain API compatibility with existing code

    /// <summary>
    /// Find the nearest waypoint ahead. Returns current waypoint index.
    /// Simplified: just returns current target since we no longer need complex searching.
    /// </summary>
    public int FindNearestWaypoint(Vector3 position)
    {
        return currentWaypointIndex;
    }

    /// <summary>
    /// Resume from nearest waypoint. Simplified: just continues to current target.
    /// </summary>
    public void ResumeFromNearestWaypoint(Vector3 currentPosition)
    {
        ResumeMovement();
    }

    /// <summary>
    /// Get NavMesh recovery attempts (deprecated - always returns 0).
    /// </summary>
    public int GetNavMeshRecoveryAttempts()
    {
        return 0;
    }

    /// <summary>
    /// Get enemy layer mask (deprecated - returns 0).
    /// </summary>
    public int GetEnemyLayer()
    {
        return LayerMask.GetMask("Enemy");
    }

    /// <summary>
    /// Set enemy layer mask (deprecated - no-op).
    /// </summary>
    public void SetEnemyLayer(int layerMask)
    {
        // No-op: separation logic removed
    }

    /// <summary>
    /// Set separation parameters (deprecated - no-op).
    /// </summary>
    public void SetSeparationParameters(float radius, float mass, float force)
    {
        // No-op: separation logic removed, using NavMeshAgent native avoidance
    }
}
