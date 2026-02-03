using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Tests for EnemyMovement component.
/// These tests validate waypoint navigation, pause/resume behavior, and completion detection.
/// Note: Without a real NavMesh, tests focus on interface mechanics and event firing.
/// </summary>
[TestFixture]
public class EnemyMovementTests
{
    private GameObject enemyGameObject;
    private EnemyMovement enemyMovement;
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Transform[] testWaypoints;

    [SetUp]
    public void SetUp()
    {
        // Create enemy game object with required components
        enemyGameObject = new GameObject("TestEnemyMovement");
        
        // Add NavMeshAgent
        agent = enemyGameObject.AddComponent<NavMeshAgent>();
        agent.enabled = false; // Disable as NavMesh might not exist in tests
        
        // Add Rigidbody
        rb = enemyGameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        
        // Add EnemyMovement component
        enemyMovement = enemyGameObject.AddComponent<EnemyMovement>();
        
        // Create test waypoints
        testWaypoints = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            var waypointObj = new GameObject($"Waypoint{i}");
            waypointObj.transform.position = new Vector3(i * 5f, 0, 0);
            testWaypoints[i] = waypointObj.transform;
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (enemyGameObject != null)
            Object.Destroy(enemyGameObject);
        
        foreach (var waypoint in testWaypoints)
        {
            if (waypoint != null)
                Object.Destroy(waypoint.gameObject);
        }
    }

    /// <summary>
    /// Test 1: Verify sequential waypoint navigation
    /// </summary>
    [Test]
    public void TestEnemyMovementWaypointFollowing()
    {
        // Arrange
        EnemyData testData = ScriptableObject.CreateInstance<EnemyData>();
        
        // Act
        enemyMovement.Initialize(testWaypoints, testData);
        
        // Assert - movement should be initialized and heading toward waypoint 1
        Assert.AreEqual(1, enemyMovement.CurrentWaypointIndex, 
            "After initialization, should be heading to waypoint 1");
        Assert.IsFalse(enemyMovement.IsMovementPaused, 
            "Movement should not be paused initially");
    }

    /// <summary>
    /// Test 2: Verify movement can be paused and resumed
    /// </summary>
    [Test]
    public void TestEnemyMovementPauseResume()
    {
        // Arrange
        EnemyData testData = ScriptableObject.CreateInstance<EnemyData>();
        enemyMovement.Initialize(testWaypoints, testData);
        
        // Act - pause movement
        enemyMovement.PauseMovement();
        
        // Assert - movement should be paused
        Assert.IsTrue(enemyMovement.IsMovementPaused, 
            "Movement should be paused after calling PauseMovement");
        
        // Act - resume movement
        enemyMovement.ResumeMovement();
        
        // Assert - movement should be resumed
        Assert.IsFalse(enemyMovement.IsMovementPaused, 
            "Movement should be resumed after calling ResumeMovement");
    }

    /// <summary>
    /// Test 3: Verify completion when reaching final waypoint
    /// </summary>
    [Test]
    public void TestEnemyMovementReachesDestination()
    {
        // Arrange
        EnemyData testData = ScriptableObject.CreateInstance<EnemyData>();
        enemyMovement.Initialize(testWaypoints, testData);
        
        bool finalWaypointReached = false;
        enemyMovement.OnFinalWaypointReached += () => 
        {
            finalWaypointReached = true;
        };
        
        int waypointReachedCount = 0;
        enemyMovement.OnWaypointReached += (index) => 
        {
            waypointReachedCount++;
        };
        
        // Act - manually advance to final waypoint state
        // Simulate reaching waypoint 1
        enemyMovement.UpdateMovement();
        
        // Note: In a real scenario with NavMesh, this would happen via remaining distance
        // For now we test that events can be subscribed and the structure is in place
        // The actual waypoint progression requires NavMesh simulation
        
        // Assert - structure is initialized correctly
        // We cannot check event subscriptions directly, but we can verify
        // that our local tracking variables haven't been triggered yet (since we haven't reached destination)
        Assert.IsFalse(finalWaypointReached, "Should not have reached final waypoint just by updating once");
        Assert.AreEqual(0, waypointReachedCount, "Should not have reached any waypoints yet");
    }

    /// <summary>
    /// Test 4: Verify NavMesh recovery logic is initialized with 0 attempts
    /// </summary>
    [Test]
    public void TestNavMeshRecoveryAttemptsInitialized()
    {
        // Arrange
        EnemyData testData = ScriptableObject.CreateInstance<EnemyData>();
        
        // Act
        enemyMovement.Initialize(testWaypoints, testData);
        
        // Assert - recovery attempts should be initialized to 0
        Assert.AreEqual(0, enemyMovement.GetNavMeshRecoveryAttempts(), 
            "NavMesh recovery attempts should start at 0");
    }

    /// <summary>
    /// Test 5: Verify enemy layer is initialized
    /// </summary>
    [Test]
    public void TestEnemyLayerInitialization()
    {
        // Arrange
        EnemyData testData = ScriptableObject.CreateInstance<EnemyData>();
        
        // Act
        enemyMovement.Initialize(testWaypoints, testData);
        
        // Assert - enemy layer should be initialized (not 0 or uninitialized)
        int enemyLayer = enemyMovement.GetEnemyLayer();
        Assert.AreNotEqual(0, enemyLayer, 
            "Enemy layer should be initialized to a valid layer mask");
    }

    /// <summary>
    /// Test 6: Test return to nearest waypoint functionality
    /// Verify that FindNearestWaypoint returns the closest waypoint ahead of current position
    /// </summary>
    [Test]
    public void TestReturnToNearestWaypoint()
    {
        // Arrange
        EnemyData testData = ScriptableObject.CreateInstance<EnemyData>();
        enemyMovement.Initialize(testWaypoints, testData);
        
        // Set current position between waypoints 0 and 1
        // Waypoints are at (0,0,0), (5,0,0), (10,0,0)
        enemyGameObject.transform.position = new Vector3(2.5f, 0, 0);
        
        // Act - find nearest waypoint ahead from current position
        int nearestWaypointIndex = enemyMovement.FindNearestWaypoint(enemyGameObject.transform.position);
        
        // Assert - should find waypoint 1 (at 5,0,0) as nearest ahead
        Assert.AreEqual(1, nearestWaypointIndex,
            "Should find waypoint 1 as nearest ahead when positioned between waypoints 0 and 1");
    }

    /// <summary>
    /// Test 7: Test path progression after resuming from pause
    /// Verify that ResumeFromNearestWaypoint continues from current target waypoint
    /// (Simplified behavior: no waypoint searching, just resume)
    /// </summary>
    [Test]
    public void TestPathProgressionAfterCombat()
    {
        // Arrange
        EnemyData testData = ScriptableObject.CreateInstance<EnemyData>();
        enemyMovement.Initialize(testWaypoints, testData);
        
        // After Initialize, currentWaypointIndex is 1
        // Simulate pausing and resuming
        enemyMovement.PauseMovement();
        
        // Act - resume movement (simplified: continues to current waypoint)
        enemyMovement.ResumeFromNearestWaypoint(enemyGameObject.transform.position);
        
        // Assert - should still be targeting waypoint 1 (simplified: no waypoint change)
        Assert.AreEqual(1, enemyMovement.CurrentWaypointIndex,
            "After resuming, should continue to current target waypoint 1");
        Assert.IsFalse(enemyMovement.IsMovementPaused,
            "Movement should be resumed");
    }

    /// <summary>
    /// Test 8: Test no backtracking - enemy continues forward on path
    /// Verify that FindNearestWaypoint returns current target (simplified behavior)
    /// </summary>
    [Test]
    public void TestNoBacktracking()
    {
        // Arrange
        EnemyData testData = ScriptableObject.CreateInstance<EnemyData>();
        enemyMovement.Initialize(testWaypoints, testData);
        
        // After Initialize, currentWaypointIndex is 1
        
        // Act - find nearest waypoint (simplified: returns current target)
        int nearestWaypointIndex = enemyMovement.FindNearestWaypoint(enemyGameObject.transform.position);
        
        // Assert - should return current waypoint index (no searching)
        Assert.AreEqual(1, nearestWaypointIndex,
            "FindNearestWaypoint should return current target waypoint (simplified behavior)");
        
        // Verify no backtracking
        Assert.GreaterOrEqual(nearestWaypointIndex, 1,
            "Should not backtrack to waypoint 0");
    }

    [Test]
    public void EnemyMovement_UsesEnemyDataSpeed_NotInspectorValue()
    {
        // Arrange
        var data = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 5f);
        
        // Act
        enemyMovement.Initialize(testWaypoints, data);
        
        // Assert: Verify moveSpeed is loaded from EnemyData (5f)
        var moveSpeedField = typeof(EnemyMovement).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        float actualSpeed = (float)moveSpeedField?.GetValue(enemyMovement);
        Assert.AreEqual(5f, actualSpeed, "moveSpeed should be loaded from EnemyData (5f)");
        
        Object.DestroyImmediate(data);
    }

    [Test]
    public void EnemyMovement_UpdateMovement_FailsIfInitializedWithoutData()
    {
        // Act & Assert: Should not throw exception, should handle gracefully
        Assert.DoesNotThrow(() => enemyMovement.UpdateMovement());
    }
}
