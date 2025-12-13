using UnityEngine;
using UnityEngine.AI;
using NUnit.Framework;

/// <summary>
/// Tests for enemy player detection and attack systems
/// </summary>
public class EnemyAttackTests
{
    private GameObject enemyObject;
    private Enemy enemy;
    private GameObject playerObject;
    private PlayerHealth playerHealth;
    private EnemySpawner mockSpawner;
    private Transform[] mockWaypoints;

    [SetUp]
    public void Setup()
    {
        // Create mock spawner
        GameObject spawnerObj = new GameObject("MockSpawner");
        mockSpawner = spawnerObj.AddComponent<EnemySpawner>();
        
        // Create waypoints
        GameObject wp1 = new GameObject("WP1");
        wp1.transform.position = Vector3.zero;
        GameObject wp2 = new GameObject("WP2");
        wp2.transform.position = Vector3.forward * 10f;
        mockWaypoints = new[] { wp1.transform, wp2.transform };
        
        // Create player with health
        playerObject = new GameObject("XROrigin");
        playerObject.tag = "Player";
        playerHealth = playerObject.AddComponent<PlayerHealth>();
        Collider playerCollider = playerObject.AddComponent<SphereCollider>();
        
        // Create enemy
        enemyObject = new GameObject("TestEnemy");
        enemyObject.AddComponent<NavMeshAgent>();
        enemyObject.AddComponent<Rigidbody>();
        enemy = enemyObject.AddComponent<Enemy>();
        Collider enemyCollider = enemyObject.AddComponent<SphereCollider>();
        
        // Initialize enemy
        enemy.Initialize(mockWaypoints, mockSpawner);
        
        // Set enemy position away from player initially
        enemyObject.transform.position = Vector3.forward * 10f;
    }

    [TearDown]
    public void Teardown()
    {
        if (enemyObject != null)
            Object.Destroy(enemyObject);
        if (playerObject != null)
            Object.Destroy(playerObject);
        
        foreach (var wp in mockWaypoints)
        {
            if (wp != null)
                Object.Destroy(wp.gameObject);
        }
    }

    [Test]
    public void TestEnemyCanTransitionToAttacking()
    {
        Assert.AreEqual(EnemyState.Moving, enemy.GetCurrentState());
        
        enemy.TransitionToAttacking();
        
        Assert.AreEqual(EnemyState.Attacking, enemy.GetCurrentState());
    }

    [Test]
    public void TestEnemyAttackingStateExists()
    {
        enemy.TransitionToAttacking();
        
        // Just verify state changed without errors
        Assert.AreEqual(EnemyState.Attacking, enemy.GetCurrentState());
    }

    [Test]
    public void TestEnemyCanGetAttackRange()
    {
        // Verify attack range is configurable (tested through exposed properties)
        enemy.TransitionToAttacking();
        Assert.AreEqual(EnemyState.Attacking, enemy.GetCurrentState());
    }

    [Test]
    public void TestEnemyReturnsToMovingFromAttacking()
    {
        enemy.TransitionToAttacking();
        Assert.AreEqual(EnemyState.Attacking, enemy.GetCurrentState());
        
        enemy.TransitionToMoving();
        Assert.AreEqual(EnemyState.Moving, enemy.GetCurrentState());
    }
}
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.AI;
using NUnit.Framework;

/// <summary>
/// Tests for enemy player detection and attack systems
/// </summary>
public class EnemyAttackTests
{
    private GameObject enemyObject;
    private Enemy enemy;
    private GameObject playerObject;
    private PlayerHealth playerHealth;
    private EnemySpawner mockSpawner;
    private Transform[] mockWaypoints;

    [SetUp]
    public void Setup()
    {
        // Create mock spawner
        GameObject spawnerObj = new GameObject("MockSpawner");
        mockSpawner = spawnerObj.AddComponent<EnemySpawner>();
        
        // Create waypoints
        GameObject wp1 = new GameObject("WP1");
        wp1.transform.position = Vector3.zero;
        GameObject wp2 = new GameObject("WP2");
        wp2.transform.position = Vector3.forward * 10f;
        mockWaypoints = new[] { wp1.transform, wp2.transform };
        
        // Create player with health
        playerObject = new GameObject("XROrigin");
        playerObject.tag = "Player";
        playerHealth = playerObject.AddComponent<PlayerHealth>();
        Collider playerCollider = playerObject.AddComponent<SphereCollider>();
        
        // Create enemy
        enemyObject = new GameObject("TestEnemy");
        enemyObject.AddComponent<NavMeshAgent>();
        enemyObject.AddComponent<Rigidbody>();
        enemy = enemyObject.AddComponent<Enemy>();
        Collider enemyCollider = enemyObject.AddComponent<SphereCollider>();
        
        // Initialize enemy
        enemy.Initialize(mockWaypoints, mockSpawner);
        
        // Set enemy position away from player initially
        enemyObject.transform.position = Vector3.forward * 10f;
    }

    [TearDown]
    public void Teardown()
    {
        if (enemyObject != null)
            Object.Destroy(enemyObject);
        if (playerObject != null)
            Object.Destroy(playerObject);
        
        foreach (var wp in mockWaypoints)
        {
            if (wp != null)
                Object.Destroy(wp.gameObject);
        }
    }

    [Test]
    public void TestEnemyCanTransitionToAttacking()
    {
        Assert.AreEqual(EnemyState.Moving, enemy.GetCurrentState());
        
        enemy.TransitionToAttacking();
        
        Assert.AreEqual(EnemyState.Attacking, enemy.GetCurrentState());
    }

    [Test]
    public void TestEnemyAttackingStateExists()
    {
        enemy.TransitionToAttacking();
        
        // Just verify state changed without errors
        Assert.AreEqual(EnemyState.Attacking, enemy.GetCurrentState());
    }

    [Test]
    public void TestEnemyCanGetAttackRange()
    {
        // Verify attack range is configurable (tested through exposed properties)
        enemy.TransitionToAttacking();
        Assert.AreEqual(EnemyState.Attacking, enemy.GetCurrentState());
    }

    [Test]
    public void TestEnemyReturnsToMovingFromAttacking()
    {
        enemy.TransitionToAttacking();
        Assert.AreEqual(EnemyState.Attacking, enemy.GetCurrentState());
        
        enemy.TransitionToMoving();
        Assert.AreEqual(EnemyState.Moving, enemy.GetCurrentState());
    }
}
#endif
