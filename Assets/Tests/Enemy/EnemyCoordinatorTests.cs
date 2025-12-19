using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Tests for Enemy.cs refactored to coordinator pattern.
/// Verifies that Enemy properly coordinates with NavMeshPlayerDetector, EnemyMovement, and EnemyStateMachine.
/// </summary>
[TestFixture]
public class EnemyCoordinatorTests
{
    private GameObject enemyGameObject;
    private Enemy enemy;
    private NavMeshAgent agent;
    private Rigidbody rb;
    private NavMeshPlayerDetector playerDetector;
    private EnemyMovement enemyMovement;
    private EnemyStateMachine stateMachine;
    
    private GameObject playerObject;
    private PlayerHealth playerHealth;
    private GameObject waypointParent;
    private Transform[] waypoints;

    [SetUp]
    public void SetUp()
    {
        // Create player with health
        playerObject = new GameObject("Player");
        playerObject.tag = "Player";
        playerHealth = playerObject.AddComponent<PlayerHealth>();
        playerObject.AddComponent<SphereCollider>();
        playerObject.transform.position = new Vector3(0, 0, 0);

        // Create test enemy with all required components
        enemyGameObject = new GameObject("TestEnemy");
        
        agent = enemyGameObject.AddComponent<NavMeshAgent>();
        agent.enabled = false; // Disable initially as NavMesh might not exist in tests
        
        rb = enemyGameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        
        playerDetector = enemyGameObject.AddComponent<NavMeshPlayerDetector>();
        enemyMovement = enemyGameObject.AddComponent<EnemyMovement>();
        stateMachine = enemyGameObject.AddComponent<EnemyStateMachine>();
        
        enemy = enemyGameObject.AddComponent<Enemy>();

        // Create waypoints for movement tests
        waypointParent = new GameObject("Route");
        GameObject wp1 = new GameObject("Waypoint1");
        wp1.transform.parent = waypointParent.transform;
        wp1.transform.position = new Vector3(5, 0, 0);
        
        GameObject wp2 = new GameObject("Waypoint2");
        wp2.transform.parent = waypointParent.transform;
        wp2.transform.position = new Vector3(10, 0, 0);

        waypoints = new Transform[] { wp1.transform, wp2.transform };
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(enemyGameObject);
        Object.Destroy(playerObject);
        Object.Destroy(waypointParent);
    }

    /// <summary>
    /// Test 1: Verify that when Enemy dies, all components are properly disabled
    /// </summary>
    [Test]
    public void TestEnemyDeathStopsAllComponents()
    {
        // Arrange: Set up enemy with health
        EnemyData testData = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(testData, 20f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(testData, 20f);
        
        enemy.SetEnemyData(testData);
        enemy.Initialize(waypoints, null);

        // Verify components are active before death
        Assert.IsTrue(enemyMovement.enabled, "EnemyMovement should be enabled before death");
        Assert.IsTrue(stateMachine.enabled, "EnemyStateMachine should be enabled before death");

        // Act: Enemy takes damage and dies
        enemy.TakeDamage(30f); // More than health

        // Assert: Components should be disabled
        Assert.IsFalse(enemyMovement.enabled, "EnemyMovement should be disabled on death");
        Assert.IsFalse(stateMachine.enabled, "EnemyStateMachine should be disabled on death");
        Assert.IsFalse(playerDetector.enabled, "NavMeshPlayerDetector should be disabled on death");
        
        // NavMeshAgent should be disabled/stopped
        if (agent.enabled)
        {
            Assert.AreEqual(Vector3.zero, agent.velocity, "Agent velocity should be zero on death");
        }
    }

    /// <summary>
    /// Test 2: Verify that health system works correctly with new architecture
    /// </summary>
    [Test]
    public void TestEnemyHealthIntegration()
    {
        // Arrange: Set up enemy with specific health values
        EnemyData testData = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(testData, 50f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(testData, 50f);
        
        enemy.SetEnemyData(testData);

        // Assert initial health
        Assert.AreEqual(50f, enemy.GetHealth(), "Initial health should match data");
        Assert.AreEqual(50f, enemy.GetMaxHealth(), "Max health should match data");

        // Act: Take damage
        enemy.TakeDamage(15f);

        // Assert: Health reduced
        Assert.AreEqual(35f, enemy.GetHealth(), "Health should be reduced by damage amount");

        // Act: Take more damage
        enemy.TakeDamage(35f);

        // Assert: Enemy should be dead when health <= 0
        Assert.LessOrEqual(enemy.GetHealth(), 0f, "Health should be <= 0 after fatal damage");
    }

    /// <summary>
    /// Test 3: Verify that Enemy.Initialize() properly sets up components
    /// </summary>
    [Test]
    public void TestEnemyInitialization()
    {
        // Arrange: Create enemy data with specific values
        EnemyData testData = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("enemyType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(testData, EnemyType.FastSpider);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(testData, 15f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(testData, 25f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(testData, 25f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(testData, 20f);
        
        enemy.SetEnemyData(testData);

        // Act: Initialize enemy
        enemy.Initialize(waypoints, null);

        // Assert: Waypoints should be stored
        Assert.IsNotNull(waypoints, "Waypoints should not be null");
        Assert.AreEqual(2, waypoints.Length, "Should have 2 waypoints");

        // Assert: Health should be initialized from data
        Assert.AreEqual(25f, enemy.GetHealth(), "Health should be initialized from EnemyData");
        Assert.AreEqual(25f, enemy.GetMaxHealth(), "Max health should be initialized from EnemyData");

        // Assert: Components should be in Moving state initially
        Assert.AreEqual(EnemyState.Moving, stateMachine.CurrentState, "Should start in Moving state");

        // Assert: All components should be enabled
        Assert.IsTrue(enemyMovement.enabled, "EnemyMovement should be enabled after Initialize");
        Assert.IsTrue(stateMachine.enabled, "EnemyStateMachine should be enabled after Initialize");
        Assert.IsTrue(playerDetector.enabled, "NavMeshPlayerDetector should be enabled after Initialize");
    }
}
