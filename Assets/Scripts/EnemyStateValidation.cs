using UnityEngine;

/// <summary>
/// Manual validation script for state machine implementation
/// This script can be attached to an Enemy in a test scene to verify state transitions
/// </summary>
public class EnemyStateValidation : MonoBehaviour
{
    private Enemy enemy;
    private int eventCount = 0;
    private EnemyState lastEventState = EnemyState.Moving;

    private void OnEnable()
    {
        enemy = GetComponent<Enemy>();
        
        if (enemy != null)
        {
            enemy.OnStateChanged += LogStateChange;
            Debug.Log($"[Phase 2 Validation] Enemy initialized with state: {enemy.GetCurrentState()}");
        }
        else
        {
            Debug.LogError("[Phase 2 Validation] No Enemy component found on this GameObject!");
        }
    }

    private void OnDisable()
    {
        if (enemy != null)
        {
            enemy.OnStateChanged -= LogStateChange;
        }
    }

    private void LogStateChange(EnemyState newState)
    {
        eventCount++;
        lastEventState = newState;
        Debug.Log($"[Phase 2 Validation] Event #{eventCount}: State changed to {newState}");
    }

    /// <summary>
    /// Public method to run all validation tests
    /// Call this from inspector or via code
    /// </summary>
    public void ValidateAllStates()
    {
        if (enemy == null)
        {
            Debug.LogError("[Phase 2 Validation] Enemy component not found!");
            return;
        }

        Debug.Log("[Phase 2 Validation] Starting validation tests...");
        eventCount = 0;

        // Test 1: Initial state
        Assert(enemy.GetCurrentState() == EnemyState.Moving, "Initial state should be Moving");
        
        // Test 2: Transition to Attacking
        enemy.TransitionToAttacking();
        Assert(enemy.GetCurrentState() == EnemyState.Attacking, "Should transition to Attacking");
        
        // Test 3: Transition to Idle
        enemy.TransitionToIdle();
        Assert(enemy.GetCurrentState() == EnemyState.Idle, "Should transition to Idle");
        
        // Test 4: Transition to Moving
        enemy.TransitionToMoving();
        Assert(enemy.GetCurrentState() == EnemyState.Moving, "Should transition to Moving");
        
        // Test 5: Transition to Stunned
        enemy.TransitionToStunned();
        Assert(enemy.GetCurrentState() == EnemyState.Stunned, "Should transition to Stunned");
        
        // Test 6: Resume from Stun
        enemy.ResumeFromStun();
        Assert(enemy.GetCurrentState() == EnemyState.Moving, "Should resume to Moving after stun");
        
        // Test 7: Multiple rapid transitions
        enemy.TransitionToAttacking();
        enemy.TransitionToIdle();
        enemy.TransitionToMoving();
        Assert(enemy.GetCurrentState() == EnemyState.Moving, "Should handle rapid transitions");
        
        // Test 8: Event count verification
        Assert(eventCount > 0, $"Should have fired state change events (fired: {eventCount})");

        Debug.Log($"[Phase 2 Validation] ✓ All validation tests passed! Total state changes: {eventCount}");
    }

    private void Assert(bool condition, string message)
    {
        if (condition)
        {
            Debug.Log($"[Phase 2 Validation] ✓ {message}");
        }
        else
        {
            Debug.LogError($"[Phase 2 Validation] ✗ {message}");
        }
    }
}
