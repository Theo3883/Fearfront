using UnityEngine;
using System;

/// <summary>
/// EnemyStateMachine manages state transitions between FollowingPath and AttackingPlayer.
/// Coordinates with NavMeshPlayerDetector to ensure player is on NavMesh before engaging.
/// </summary>
public class EnemyStateMachine : MonoBehaviour
{
    private NavMeshPlayerDetector playerDetector;
    private float detectionRange;
    private PlayerHealth playerHealth;
    
    private EnemyState currentState = EnemyState.Moving;

    // Events
    public event Action<EnemyState> OnStateChanged;
    public event Action OnEngagingPlayer;
    public event Action OnDisengagingPlayer;

    /// <summary>
    /// Current state of the enemy
    /// </summary>
    public EnemyState CurrentState => currentState;

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
    /// Checks if player should be engaged based on both distance and NavMesh status
    /// </summary>
    /// <param name="playerPosition">Current position of the player</param>
    /// <returns>True if player is in range AND on NavMesh</returns>
    public bool ShouldEngagePlayer(Vector3 playerPosition)
    {
        if (playerDetector == null || playerHealth == null)
        {
            return false;
        }

        // Check if player is dead
        if (playerHealth.GetCurrentHealth() <= 0)
        {
            return false;
        }

        // Calculate distance from enemy to player
        float distanceToPlayer = Vector3.Distance(transform.position, playerPosition);
        bool isInRange = distanceToPlayer <= detectionRange;

        // Check if player is on NavMesh
        bool isOnNavMesh = playerDetector.IsPlayerOnNavMesh();

        // Both conditions must be true
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
    /// Transitions to a new state and fires appropriate events
    /// </summary>
    private void TransitionToState(EnemyState newState)
    {
        // Handle exiting current state
        if (currentState == EnemyState.Attacking)
        {
            OnDisengagingPlayer?.Invoke();
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
