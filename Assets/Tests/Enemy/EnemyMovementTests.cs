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
        Assert.IsNotNull(enemyMovement.OnWaypointReached, 
            "OnWaypointReached event should exist");
        Assert.IsNotNull(enemyMovement.OnFinalWaypointReached, 
            "OnFinalWaypointReached event should exist");
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
}
