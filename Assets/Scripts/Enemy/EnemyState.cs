/// <summary>
/// Enum for all possible enemy states
/// Used by the state machine for pathfinding and AI behavior
/// </summary>
public enum EnemyState
{
    Moving,      // Following waypoints
    Attacking,   // In combat with player
    Idle,        // Waiting/idle
    Stunned      // Disabled
}
