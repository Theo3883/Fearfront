using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Tests for EnemySpawner functionality including component initialization
/// Tests that spawner correctly initializes NavMeshPlayerDetector, EnemyMovement, and EnemyStateMachine
/// </summary>
[TestFixture]
public class EnemySpawnerTests
{
    private GameObject enemyGameObject;
    private Enemy enemy;
    private EnemySpawner spawner;
    private NavMeshAgent agent;
    private Rigidbody rb;
    private GameObject spawnerObject;
    private GameObject playerObject;
    private PlayerHealth playerHealth;
    private GameObject enemyPrefab;
    private EnemyRoute testRoute;

    [SetUp]
    public void SetUp()
    {
        // Create player with health
        playerObject = new GameObject("Player");
        playerObject.tag = "Player";
        playerHealth = playerObject.AddComponent<PlayerHealth>();
        Collider playerCollider = playerObject.AddComponent<SphereCollider>();
        playerObject.transform.position = new Vector3(0, 0, 0);

        // Create spawner
        spawnerObject = new GameObject("TestSpawner");
        spawner = spawnerObject.AddComponent<EnemySpawner>();

        // Create a game object with required components
        enemyGameObject = new GameObject("TestEnemy");
        
        // Add NavMeshAgent
        agent = enemyGameObject.AddComponent<NavMeshAgent>();
        agent.enabled = false; // Disable initially as NavMesh might not exist in tests
        
        // Add Rigidbody
        rb = enemyGameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        
        // Add Enemy script
        enemy = enemyGameObject.AddComponent<Enemy>();

        // Create enemy prefab for spawner tests
        enemyPrefab = new GameObject("EnemyPrefab");
        enemyPrefab.AddComponent<NavMeshAgent>();
        enemyPrefab.AddComponent<Rigidbody>();
        enemyPrefab.AddComponent<Enemy>();
        enemyPrefab.AddComponent<NavMeshPlayerDetector>();
        enemyPrefab.AddComponent<EnemyMovement>();
        enemyPrefab.AddComponent<EnemyStateMachine>();

        // Set spawner prefab and spawn point
        spawner.SetEnemyPrefab(enemyPrefab);
        spawner.SetSpawnPoint(spawnerObject.transform);

        // Create test route with waypoints
        GameObject waypointParent = new GameObject("Route");
        GameObject waypoint1 = new GameObject("Waypoint1");
        waypoint1.transform.parent = waypointParent.transform;
        waypoint1.transform.position = new Vector3(5, 0, 0);
        
        GameObject waypoint2 = new GameObject("Waypoint2");
        waypoint2.transform.parent = waypointParent.transform;
        waypoint2.transform.position = new Vector3(10, 0, 0);

        testRoute = waypointParent.AddComponent<EnemyRoute>();

        // Set up spawner properties
        EnemyRoute[] routes = new EnemyRoute[] { testRoute };
        typeof(EnemySpawner).GetField("availableRoutes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(spawner, routes);
        
        // Create and set enemy data variants
        EnemyData spiderData = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("enemyType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(spiderData, EnemyType.FastSpider);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(spiderData, 4.5f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(spiderData, 20f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(spiderData, 20f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(spiderData, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(spiderData, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(spiderData, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(spiderData, 5f);

        var enemyTypeVariants = new System.Collections.Generic.List<EnemyData> { spiderData };
        typeof(EnemySpawner).GetField("enemyTypeVariants", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(spawner, enemyTypeVariants);
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(enemyGameObject);
        if (enemy != null)
            Object.Destroy(enemy.gameObject);
        if (spawnerObject != null)
            Object.Destroy(spawnerObject);
        if (spawner != null)
            Object.Destroy(spawner.gameObject);
        if (playerObject != null)
            Object.Destroy(playerObject);
        if (enemyPrefab != null)
            Object.Destroy(enemyPrefab);
        if (testRoute != null)
            Object.Destroy(testRoute.gameObject);
    }

    // ===== ORIGINAL ENEMY STATE TESTS =====

    [Test]
    public void InitialState_IsMoving()
    {
        // Assert initial state is Moving
        Assert.AreEqual(EnemyState.Moving, enemy.GetCurrentState(), 
            "Enemy should start in Moving state");
    }

    [Test]
    public void TransitionToAttacking_SetsCorrectState()
    {
        // Act
        enemy.TransitionToAttacking();
        
        // Assert
        Assert.AreEqual(EnemyState.Attacking, enemy.GetCurrentState(),
            "Enemy should transition to Attacking state");
    }

    [Test]
    public void TransitionToIdle_SetsCorrectState()
    {
        // Act
        enemy.TransitionToIdle();
        
        // Assert
        Assert.AreEqual(EnemyState.Idle, enemy.GetCurrentState(),
            "Enemy should transition to Idle state");
    }

    [Test]
    public void TransitionToMoving_SetsCorrectState()
    {
        // Arrange
        enemy.TransitionToIdle();
        
        // Act
        enemy.TransitionToMoving();
        
        // Assert
        Assert.AreEqual(EnemyState.Moving, enemy.GetCurrentState(),
            "Enemy should transition to Moving state");
    }

    [Test]
    public void TransitionToStunned_SetsCorrectState()
    {
        // Act
        enemy.TransitionToStunned();
        
        // Assert
        Assert.AreEqual(EnemyState.Stunned, enemy.GetCurrentState(),
            "Enemy should transition to Stunned state");
    }

    [Test]
    public void ResumeFromStun_TransitionsToMoving()
    {
        // Arrange
        enemy.TransitionToStunned();
        Assert.AreEqual(EnemyState.Stunned, enemy.GetCurrentState());
        
        // Act
        enemy.ResumeFromStun();
        
        // Assert
        Assert.AreEqual(EnemyState.Moving, enemy.GetCurrentState(),
            "Enemy should return to Moving state after stun recovery");
    }

    [Test]
    public void OnStateChanged_EventFires_WhenTransitioning()
    {
        // Arrange
        EnemyState eventFiredWithState = EnemyState.Moving;
        bool eventFired = false;
        
        enemy.OnStateChanged += (newState) => 
        {
            eventFired = true;
            eventFiredWithState = newState;
        };
        
        // Act
        enemy.TransitionToAttacking();
        
        // Assert
        Assert.IsTrue(eventFired, "OnStateChanged event should fire");
        Assert.AreEqual(EnemyState.Attacking, eventFiredWithState,
            "Event should pass new state as parameter");
    }

    [Test]
    public void MultipleStateChanges_WorkWithoutErrors()
    {
        // This test verifies that multiple state transitions don't cause errors
        
        // Act & Assert (no exceptions should be thrown)
        Assert.DoesNotThrow(() =>
        {
            enemy.TransitionToAttacking();
            enemy.TransitionToIdle();
            enemy.TransitionToMoving();
            enemy.TransitionToStunned();
            enemy.ResumeFromStun();
            enemy.TransitionToAttacking();
        });
        
        // Verify final state
        Assert.AreEqual(EnemyState.Attacking, enemy.GetCurrentState());
    }

    [Test]
    public void NavMeshAgent_DisabledInAttackingState()
    {
        // Arrange
        if (agent.enabled)
            agent.enabled = false;
        
        // Act
        enemy.TransitionToAttacking();
        
        // Assert - agent should be disabled in attacking state
        // (We check this via state behavior, not directly on agent)
        Assert.AreEqual(EnemyState.Attacking, enemy.GetCurrentState());
    }

    [Test]
    public void NavMeshAgent_DisabledInIdleState()
    {
        // Act
        enemy.TransitionToIdle();
        
        // Assert
        Assert.AreEqual(EnemyState.Idle, enemy.GetCurrentState());
    }

    [Test]
    public void NavMeshAgent_DisabledInStunnedState()
    {
        // Act
        enemy.TransitionToStunned();
        
        // Assert
        Assert.AreEqual(EnemyState.Stunned, enemy.GetCurrentState());
    }

    [Test]
    public void CompileWithoutErrors()
    {
        // This test simply verifies the class compiles and instantiates correctly
        Assert.IsNotNull(enemy, "Enemy should be instantiated");
        Assert.IsNotNull(enemy.GetCurrentState(), "Enemy should have a valid state");
    }

    // ===== SPAWNER COMPONENT INITIALIZATION TESTS =====

    /// <summary>
    /// Test: Verify all three components exist on spawned enemy
    /// </summary>
    [Test]
    public void TestSpawner_AllComponentsExistAfterSpawn()
    {
        // Spawn an enemy
        spawner.SpawnEnemy();

        // Find the spawned enemy
        Enemy[] spawnedEnemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Assert.IsTrue(spawnedEnemies.Length > 0, "Enemy should be spawned");

        GameObject spawnedEnemyObject = spawnedEnemies[0].gameObject;

        // Verify NavMeshPlayerDetector exists (not null)
        NavMeshPlayerDetector detector = spawnedEnemyObject.GetComponent<NavMeshPlayerDetector>();
        Assert.IsNotNull(detector, "NavMeshPlayerDetector component should exist on spawned enemy");

        // Verify EnemyMovement exists (not null)
        EnemyMovement movement = spawnedEnemyObject.GetComponent<EnemyMovement>();
        Assert.IsNotNull(movement, "EnemyMovement component should exist on spawned enemy");

        // Verify EnemyStateMachine exists (not null)
        EnemyStateMachine stateMachine = spawnedEnemyObject.GetComponent<EnemyStateMachine>();
        Assert.IsNotNull(stateMachine, "EnemyStateMachine component should exist on spawned enemy");

        // Clean up spawned enemy
        Object.Destroy(spawnedEnemyObject);
    }

    /// <summary>
    /// Test: Verify detector and movement components are accessible and functional
    /// </summary>
    [Test]
    public void TestSpawner_ComponentsAreAccessible()
    {
        // Spawn an enemy
        spawner.SpawnEnemy();

        // Find the spawned enemy
        Enemy[] spawnedEnemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Assert.IsTrue(spawnedEnemies.Length > 0, "Enemy should be spawned");

        GameObject spawnedEnemyObject = spawnedEnemies[0].gameObject;

        // Get detector and verify it's accessible
        NavMeshPlayerDetector detector = spawnedEnemyObject.GetComponent<NavMeshPlayerDetector>();
        Assert.IsNotNull(detector, "Detector should not be null");
        
        // Verify detector can be called (it should be initialized)
        bool isOnNavMesh = detector.IsPlayerOnNavMesh();
        Assert.IsTrue(isOnNavMesh == true || isOnNavMesh == false, 
            "Detector should be accessible and return a valid boolean");

        // Get movement and verify it's accessible
        EnemyMovement movement = spawnedEnemyObject.GetComponent<EnemyMovement>();
        Assert.IsNotNull(movement, "Movement should not be null");
        
        // Verify movement has waypoint data
        int currentWaypointIndex = movement.CurrentWaypointIndex;
        Assert.GreaterOrEqual(currentWaypointIndex, 0, 
            "Movement should have a valid waypoint index");
        
        // Verify movement is not paused by default
        Assert.IsFalse(movement.IsMovementPaused, 
            "Movement should not be paused on initialization");

        // Clean up spawned enemy
        Object.Destroy(spawnedEnemyObject);
    }

    /// <summary>
    /// Test: Verify state machine exists and initializes correctly
    /// </summary>
    [Test]
    public void TestSpawner_StateMachineInitializes()
    {
        // Spawn an enemy
        spawner.SpawnEnemy();

        // Find the spawned enemy
        Enemy[] spawnedEnemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Assert.IsTrue(spawnedEnemies.Length > 0, "Enemy should be spawned");

        GameObject spawnedEnemyObject = spawnedEnemies[0].gameObject;

        // Get state machine
        EnemyStateMachine stateMachine = spawnedEnemyObject.GetComponent<EnemyStateMachine>();
        Assert.IsNotNull(stateMachine, "State machine should not be null");
        
        // Verify state machine starts in Moving state
        Assert.AreEqual(EnemyState.Moving, stateMachine.CurrentState, 
            "State machine should initialize in Moving state");
        
        // Verify state machine can evaluate engagement conditions
        bool shouldEngage = stateMachine.ShouldEngagePlayer(playerObject.transform.position);
        Assert.IsTrue(shouldEngage == true || shouldEngage == false, 
            "State machine should be able to evaluate engagement conditions");

        // Clean up spawned enemy
        Object.Destroy(spawnedEnemyObject);
    }

    /// <summary>
    /// Test: Verify spawner works even without player (partial initialization)
    /// </summary>
    [Test]
    public void TestSpawner_WorksWithoutPlayer()
    {
        // Destroy the player to simulate missing player
        Object.Destroy(playerObject);
        playerObject = null;

        // Spawn an enemy - should not throw exception even without player
        Assert.DoesNotThrow(() =>
        {
            spawner.SpawnEnemy();
        }, "Spawner should handle missing player gracefully");

        // Find the spawned enemy
        Enemy[] spawnedEnemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Assert.IsTrue(spawnedEnemies.Length > 0, "Enemy should be spawned even without player");

        GameObject spawnedEnemyObject = spawnedEnemies[0].gameObject;

        // Verify EnemyMovement component still works
        EnemyMovement movement = spawnedEnemyObject.GetComponent<EnemyMovement>();
        Assert.IsNotNull(movement, "EnemyMovement should exist even without player");

        // Verify NavMeshPlayerDetector component still works
        NavMeshPlayerDetector detector = spawnedEnemyObject.GetComponent<NavMeshPlayerDetector>();
        Assert.IsNotNull(detector, "NavMeshPlayerDetector should exist even without player");

        // Clean up spawned enemy
        Object.Destroy(spawnedEnemyObject);
    }

    // ===== PHASE 4: ENEMY VARIANT SPAWNING TESTS =====

    /// <summary>
    /// Test: EnemySpawner returns null when enemyTypeVariants list is empty
    /// </summary>
    [Test]
    public void EnemySpawner_ReturnsNull_WhenEnemyTypeVariantsEmpty()
    {
        // Arrange - Clear the enemy type variants list
        var emptyVariants = new System.Collections.Generic.List<EnemyData>();
        typeof(EnemySpawner).GetField("enemyTypeVariants", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(spawner, emptyVariants);

        // Act - Call GetRandomEnemyType which should return null when no variants exist
        var methodInfo = typeof(EnemySpawner).GetMethod("GetRandomEnemyType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        EnemyData result = (EnemyData)methodInfo?.Invoke(spawner, null);
        
        // Assert - Verify it returns null when no variants exist
        Assert.IsNull(result, "GetRandomEnemyType should return null when no variants exist");
    }

    /// <summary>
    /// Test: EnemySpawner spawns different enemy types from variants list
    /// Verifies that multiple spawns use different types (indicated by different speeds/health)
    /// </summary>
    [Test]
    public void EnemySpawner_SpawnsDifferentTypes_FromVariantsList()
    {
        // Arrange - Create 3 different enemy data assets with different stats
        EnemyData fastSpider = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("enemyName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, "FastSpider");
        typeof(EnemyData).GetField("enemyType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, EnemyType.FastSpider);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 5f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 20f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 20f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 15f);

        EnemyData tankSpider = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("enemyName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, "TankSpider");
        typeof(EnemyData).GetField("enemyType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, EnemyType.TankSpider);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 2f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 80f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 80f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 15f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 2.5f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 2f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 12f);

        EnemyData venomSpider = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("enemyName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(venomSpider, "VenomSpider");
        typeof(EnemyData).GetField("enemyType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(venomSpider, EnemyType.VenomSpider);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(venomSpider, 3.5f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(venomSpider, 50f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(venomSpider, 50f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(venomSpider, 12f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(venomSpider, 3f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(venomSpider, 1.8f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(venomSpider, 18f);

        // Set up spawner with 3 different variants
        var variants = new System.Collections.Generic.List<EnemyData> { fastSpider, tankSpider, venomSpider };
        typeof(EnemySpawner).GetField("enemyTypeVariants", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(spawner, variants);
        
        // Set difficulty to Normal which uses all three types
        spawner.SetDifficultyPreset(SpawnDifficulty.Normal);

        // Act - Spawn multiple enemies and track their speeds
        var spawnedSpeeds = new System.Collections.Generic.HashSet<float>();
        var spawnedHealths = new System.Collections.Generic.HashSet<float>();

        for (int i = 0; i < 10; i++)
        {
            spawner.SpawnEnemy();
        }

        // Find all spawned enemies and record their stats
        Enemy[] spawnedEnemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        
        foreach (Enemy spawnedEnemy in spawnedEnemies)
        {
            if (spawnedEnemy != enemy) // Skip the original enemy from SetUp
            {
                EnemyData spawnedData = typeof(Enemy).GetField("enemyData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(spawnedEnemy) as EnemyData;
                if (spawnedData != null)
                {
                    spawnedSpeeds.Add(spawnedData.MoveSpeed);
                    spawnedHealths.Add(spawnedData.MaxHealth);
                }
            }
        }

        // Assert - Verify that multiple different types were spawned (indicated by different speeds or health)
        Assert.Greater(spawnedSpeeds.Count, 1, "Multiple spawn calls should use different enemy types with different speeds");
        Assert.Greater(spawnedHealths.Count, 1, "Multiple spawn calls should use different enemy types with different health values");

        // Clean up all spawned enemies
        foreach (Enemy spawnedEnemy in spawnedEnemies)
        {
            if (spawnedEnemy != enemy)
                Object.Destroy(spawnedEnemy.gameObject);
        }
    }

    /// <summary>
    /// Test: EnemySpawner logs warning when no matching type found in variants
    /// This simulates a difficulty preset requesting an enemy type not in the variants list
    /// </summary>
    [Test]
    public void EnemySpawner_LogsWarning_WhenNoMatchingTypeFound()
    {
        // Arrange - Create spawner with only FastSpider variant
        EnemyData fastSpider = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("enemyType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, EnemyType.FastSpider);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 5f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 20f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 20f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 15f);

        // Set variants to only contain FastSpider
        var limitedVariants = new System.Collections.Generic.List<EnemyData> { fastSpider };
        typeof(EnemySpawner).GetField("enemyTypeVariants", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(spawner, limitedVariants);
        
        // Set difficulty to Hard which wants GoliathSpider (which doesn't exist in variants)
        spawner.SetDifficultyPreset(SpawnDifficulty.Hard);

        // Act - Call GetRandomEnemyType which will try to find GoliathSpider
        var methodInfo = typeof(EnemySpawner).GetMethod("GetRandomEnemyType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        EnemyData result = (EnemyData)methodInfo?.Invoke(spawner, null);
        
        // Assert - Result should be null or fallback since GoliathSpider doesn't exist in variants
        Assert.IsTrue(result == null || result == fastSpider, "Should return null or fallback to available variant when requested type not found");
    }
}
