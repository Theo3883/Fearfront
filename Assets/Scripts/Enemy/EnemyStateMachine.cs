using UnityEngine;
using System;

/// <summary>
/// EnemyStateMachine manages state transitions between FollowingPath and AttackingPlayer.
/// Coordinates with NavMeshPlayerDetector to ensure player is on NavMesh before engaging.
/// </summary>
public class EnemyStateMachine : MonoBehaviour
{
    [SerializeField] private NavMeshPlayerDetector playerDetector;
    private float detectionRange = 0f;
    [SerializeField] private PlayerHealth playerHealth;
    
    private EnemyState currentState = EnemyState.Moving;
    private bool requirePlayerOnNavMesh = false; // Default: NavMesh check disabled, distance-only

    // Events
    public event Action<EnemyState> OnStateChanged;
    public event Action OnEngagingPlayer;
    public event Action OnDisengagingPlayer;
    public event Action<Vector3> OnResumePathMovement;

    /// <summary>
    /// Current state of the enemy
    /// </summary>
    public EnemyState CurrentState => currentState;

    private void Start()
    {
        // Auto-find player health if not assigned
        if (playerHealth == null)
        {
            AutoFindPlayerHealth();
        }
        
        // Auto-find player detector if not assigned
        if (playerDetector == null)
        {
            playerDetector = GetComponent<NavMeshPlayerDetector>();
        }
    }

    /// <summary>
    /// Auto-finds the PlayerHealth component by singleton or tag
    /// </summary>
    private void AutoFindPlayerHealth()
    {
        // Try PlayerHealth singleton first
        if (PlayerHealth.Instance != null)
        {
            playerHealth = PlayerHealth.Instance;
            return;
        }

        // Try finding by tag
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerHealth = playerObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                return;
        }

        // Fallback to FindFirstObjectByType
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        
        if (playerHealth == null)
        {
            Debug.LogWarning($"EnemyStateMachine on '{gameObject.name}' could not auto-find PlayerHealth. Assign manually or ensure player has 'Player' tag.");
        }
    }

    /// <summary>
    /// Initialize the state machine with dependencies
    /// </summary>
    public void Initialize(NavMeshPlayerDetector detector, float detectionRange, PlayerHealth playerHealthRef)
    {
        playerDetector = detector;
        this.detectionRange = detectionRange;
        playerHealth = playerHealthRef;
        
        // Start in Moving state
        currentState = EnemyState.Moving;
    }

    /// <summary>
    /// Sets whether the player must be on NavMesh for engagement
    /// </summary>
    /// <param name="require">If true, player must be on NavMesh; if false, distance-only check</param>
    public void SetRequirePlayerOnNavMesh(bool require)
    {
        requirePlayerOnNavMesh = require;
    }

    /// <summary>
    /// Checks if player should be engaged based on distance and optionally NavMesh status
    /// </summary>
    /// <param name="playerPosition">Current position of the player</param>
    /// <returns>True if player is in range; also requires NavMesh if requirePlayerOnNavMesh is true</returns>
    public bool ShouldEngagePlayer(Vector3 playerPosition)
    {
        if (playerDetector == null || playerHealth == null)
        {
            Debug.LogWarning($"[{gameObject.name}] ShouldEngagePlayer: playerDetector={playerDetector}, playerHealth={playerHealth}");
            return false;
        }

        // Check if player is dead
        if (playerHealth.GetCurrentHealth() <= 0)
        {
            Debug.Log($"[{gameObject.name}] Player is dead (health={playerHealth.GetCurrentHealth()})");
            return false;
        }

        // Calculate distance from enemy to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerPosition);
        bool isInRange = distanceToPlayer <= detectionRange;

        Debug.Log($"[{gameObject.name}] Distance to player: {distanceToPlayer:F1}m, Detection range: {detectionRange}m, In range: {isInRange}");

        // If NavMesh check is disabled, only check distance
        if (!requirePlayerOnNavMesh)
        {
            return isInRange;
        }

        // If NavMesh check is enabled, check both conditions
        bool isOnNavMesh = playerDetector.IsPlayerOnNavMesh();
        Debug.Log($"[{gameObject.name}] NavMesh check enabled: isInRange={isInRange}, isOnNavMesh={isOnNavMesh}");
        return isInRange && isOnNavMesh;
    }

    /// <summary>
    /// Updates state machine, evaluating transitions each frame
    /// </summary>
    /// <param name="playerPosition">Current position of the player</param>
    public void UpdateState(Vector3 playerPosition)
    {
        EnemyState newState = currentState;

        switch (currentState)
        {
            case EnemyState.Moving:
                // Transition to Attacking if player should be engaged
                if (ShouldEngagePlayer(playerPosition))
                {
                    newState = EnemyState.Attacking;
                }
                break;

            case EnemyState.Attacking:
                // Transition back to Moving if player should not be engaged
                if (!ShouldEngagePlayer(playerPosition))
                {
                    newState = EnemyState.Moving;
                }
                break;
        }

        // Handle state change
        if (newState != currentState)
        {
            TransitionToState(newState);
        }
    }

    /// <summary>
    /// Forces a state change (used for backward compatibility and testing)
    /// </summary>
    public void ForceStateChange(EnemyState newState)
    {
        TransitionToState(newState);
    }

    /// <summary>
    /// Transitions to a new state and fires appropriate events
    /// </summary>
    private void TransitionToState(EnemyState newState)
    {
        // Handle exiting current state
        if (currentState == EnemyState.Attacking)
        {
            OnDisengagingPlayer?.Invoke();
            
            // When transitioning from Attacking back to Moving, signal to resume path movement
            if (newState == EnemyState.Moving)
            {
                OnResumePathMovement?.Invoke(transform.position);
            }
        }

        // Update state
        currentState = newState;

        // Handle entering new state
        if (newState == EnemyState.Attacking)
        {
            OnEngagingPlayer?.Invoke();
        }

        // Fire state changed event
        OnStateChanged?.Invoke(newState);
    }
}
