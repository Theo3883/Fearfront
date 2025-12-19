#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class EnemyTests
{
    private GameObject testObject;
    private Enemy enemy;

    [SetUp]
    public void SetUp()
    {
        // Create a game object with Enemy component
        testObject = new GameObject("TestEnemy");
        enemy = testObject.AddComponent<Enemy>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(testObject);
    }

    [Test]
    public void Enemy_LoadsHealthFromEnemyData_NotInspectorValue()
    {
        // Arrange
        var data = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 100f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 100f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 5f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 5f);
        
        // Act
        enemy.SetEnemyData(data);
        
        // Assert: Verify GetMaxHealth returns 100 from EnemyData, not Inspector default (20)
        Assert.AreEqual(100f, enemy.GetMaxHealth(), "GetMaxHealth should return 100 from EnemyData, not Inspector default (20)");
        
        Object.DestroyImmediate(data);
    }

    [Test]
    public void Enemy_RequiresEnemyDataBeforeInitialize_SetsHealthCorrectly()
    {
        // Arrange
        var data = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 75f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 75f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 5f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 5f);
        
        // Act
        enemy.SetEnemyData(data);
        
        // Assert: Verify currentHealth is set to 75 from EnemyData
        Assert.AreEqual(75f, enemy.GetHealth(), "currentHealth should be initialized to 75 from EnemyData");
        
        Object.DestroyImmediate(data);
    }

    [Test]
    public void Enemy_SetEnemyData_OverridesHealthValues()
    {
        // Arrange
        var data1 = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data1, 50f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data1, 50f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data1, 5f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data1, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data1, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data1, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data1, 5f);
        
        var data2 = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data2, 150f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data2, 150f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data2, 5f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data2, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data2, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data2, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data2, 5f);
        
        // Act - First SetEnemyData
        enemy.SetEnemyData(data1);
        Assert.AreEqual(50f, enemy.GetMaxHealth(), "First SetEnemyData should set maxHealth to 50");
        
        // Act - Second SetEnemyData with different values
        enemy.SetEnemyData(data2);
        
        // Assert: Verify health values updated to data2 values
        Assert.AreEqual(150f, enemy.GetMaxHealth(), "SetEnemyData should override previous health values with new data (150)");
        Assert.AreEqual(150f, enemy.GetHealth(), "currentHealth should also be updated to new EnemyData values");
        
        Object.DestroyImmediate(data1);
        Object.DestroyImmediate(data2);
    }

    [Test]
    public void Enemy_DoesNotStoreWaypoints_OnlyDelegates()
    {
        // Arrange
        var data = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 100f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 100f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 5f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 5f);

        enemy.SetEnemyData(data);

        // Create waypoints array
        Transform[] testWaypoints = new Transform[2];
        testWaypoints[0] = new GameObject("Waypoint0").transform;
        testWaypoints[1] = new GameObject("Waypoint1").transform;

        // Act
        enemy.Initialize(testWaypoints, null);

        // Assert: Verify Enemy does NOT have a waypoints field storing the waypoints
        var waypointsField = typeof(Enemy).GetField("waypoints", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Either the field doesn't exist, or it's null after Initialize
        if (waypointsField != null)
        {
            var waypointsValue = waypointsField.GetValue(enemy);
            Assert.IsNull(waypointsValue, "Enemy.waypoints should be null - waypoints should not be stored in Enemy");
        }
        // If field doesn't exist, that's also passing - which means we removed it

        // Verify EnemyMovement HAS the waypoints stored
        var enemyMovement = testObject.GetComponent<EnemyMovement>();
        if (enemyMovement != null)
        {
            var movementWaypointsField = typeof(EnemyMovement).GetField("waypoints", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(movementWaypointsField, "EnemyMovement should have waypoints field");
            var movementWaypoints = movementWaypointsField.GetValue(enemyMovement) as Transform[];
            Assert.IsNotNull(movementWaypoints, "EnemyMovement.waypoints should be initialized with the waypoints array");
            Assert.AreEqual(testWaypoints.Length, movementWaypoints.Length, "EnemyMovement should store all waypoints passed to it");
        }

        // Cleanup
        Object.DestroyImmediate(testWaypoints[0].gameObject);
        Object.DestroyImmediate(testWaypoints[1].gameObject);
        Object.DestroyImmediate(data);
    }

    [Test]
    public void EnemyMovement_IsOnlyWaypointStorage_AfterInitialize()
    {
        // Arrange
        var enemyMovement = testObject.AddComponent<EnemyMovement>();
        var data = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 100f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 100f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 5f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 5f);

        Transform[] testWaypoints = new Transform[3];
        testWaypoints[0] = new GameObject("Waypoint0").transform;
        testWaypoints[1] = new GameObject("Waypoint1").transform;
        testWaypoints[2] = new GameObject("Waypoint2").transform;

        // Act
        enemyMovement.Initialize(testWaypoints, data);

        // Assert: Verify CurrentWaypointIndex property exists and is initialized correctly
        Assert.AreEqual(0, enemyMovement.CurrentWaypointIndex, "CurrentWaypointIndex should be 0 after Initialize");

        // Verify waypoints are stored in EnemyMovement
        var waypointsField = typeof(EnemyMovement).GetField("waypoints", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.IsNotNull(waypointsField, "EnemyMovement should have waypoints field");
        var storedWaypoints = waypointsField.GetValue(enemyMovement) as Transform[];
        Assert.IsNotNull(storedWaypoints, "EnemyMovement.waypoints should be initialized");
        Assert.AreEqual(3, storedWaypoints.Length, "EnemyMovement should store all 3 waypoints");
        
        // Verify the waypoints array is the same as what we passed
        for (int i = 0; i < testWaypoints.Length; i++)
        {
            Assert.AreEqual(testWaypoints[i], storedWaypoints[i], $"Waypoint at index {i} should match the initialized waypoint");
        }

        // Cleanup
        for (int i = 0; i < testWaypoints.Length; i++)
        {
            Object.DestroyImmediate(testWaypoints[i].gameObject);
        }
        Object.DestroyImmediate(data);
    }

    [Test]
    public void EnemySpawner_PassesWaypointsOnce_ViaEnemyInitialize()
    {
        // Arrange - Create waypoints
        Transform[] testWaypoints = new Transform[2];
        testWaypoints[0] = new GameObject("Waypoint0").transform;
        testWaypoints[1] = new GameObject("Waypoint1").transform;

        // Create mock EnemySpawner with minimal setup
        var spawnerObject = new GameObject("TestSpawner");
        var spawner = spawnerObject.AddComponent<EnemySpawner>();

        // Create enemy with EnemyMovement component
        var enemy = testObject.AddComponent<Enemy>();
        var enemyMovement = testObject.AddComponent<EnemyMovement>();

        var data = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 100f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 100f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 5f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 5f);

        enemy.SetEnemyData(data);

        // Act - Initialize enemy with waypoints (this is what EnemySpawner does)
        enemy.Initialize(testWaypoints, spawner);

        // Assert: Verify Enemy passed waypoints to EnemyMovement
        var movementWaypointsField = typeof(EnemyMovement).GetField("waypoints", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var movementWaypoints = movementWaypointsField?.GetValue(enemyMovement) as Transform[];
        Assert.IsNotNull(movementWaypoints, "EnemyMovement should have waypoints after Enemy.Initialize()");
        Assert.AreEqual(testWaypoints.Length, movementWaypoints.Length, "EnemyMovement should have exactly the waypoints passed, not duplicated");

        // Verify Enemy does NOT store waypoints (avoiding triple storage)
        var enemyWaypointsField = typeof(Enemy).GetField("waypoints", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (enemyWaypointsField != null)
        {
            var enemyWaypoints = enemyWaypointsField.GetValue(enemy);
            Assert.IsNull(enemyWaypoints, "Enemy should NOT store waypoints (only EnemyMovement should)");
        }

        // Cleanup
        Object.DestroyImmediate(testWaypoints[0].gameObject);
        Object.DestroyImmediate(testWaypoints[1].gameObject);
        Object.DestroyImmediate(spawnerObject);
        Object.DestroyImmediate(data);
    }

    [Test]
    public void Enemy_AppliesVisualScale_FromEnemyData()
    {
        // Arrange
        var data = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("visualScale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 1.3f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 100f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 100f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 5f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 5f);

        // Act
        enemy.SetEnemyData(data);

        // Assert
        Assert.AreEqual(new Vector3(1.3f, 1.3f, 1.3f), testObject.transform.localScale, "transform.localScale should equal VisualScale from EnemyData");

        Object.DestroyImmediate(data);
    }

    [Test]
    public void Enemy_AppliesTypeColor_ToRenderers()
    {
        // Arrange - Create a renderer to verify color is applied
        var renderer = testObject.AddComponent<MeshRenderer>();
        testObject.AddComponent<MeshFilter>();
        
        var data = ScriptableObject.CreateInstance<EnemyData>();
        Color orangeColor = new Color(1f, 0.5f, 0f, 1f);
        typeof(EnemyData).GetField("typeColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, orangeColor);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 100f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 100f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 5f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 5f);

        // Act
        enemy.SetEnemyData(data);

        // Assert
        Color appliedColor = renderer.material.color;
        Assert.AreEqual(orangeColor, appliedColor, "Renderer material color should equal TypeColor from EnemyData");

        Object.DestroyImmediate(data);
    }

    [Test]
    public void Enemy_ClampsVisualScale_To05_20Range()
    {
        // Arrange - Test with scale too large (5f)
        var data1 = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("visualScale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data1, 5f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data1, 100f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data1, 100f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data1, 5f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data1, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data1, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data1, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data1, 5f);

        // Act - Apply too large scale
        enemy.SetEnemyData(data1);

        // Assert - Should be clamped to 2.0
        Assert.AreEqual(new Vector3(2.0f, 2.0f, 2.0f), testObject.transform.localScale, "Visual scale should be clamped to max 2.0");

        // Arrange - Test with scale too small (0.1f)
        var data2 = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("visualScale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data2, 0.1f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data2, 100f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data2, 100f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data2, 5f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data2, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data2, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data2, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data2, 5f);

        // Act - Apply too small scale
        enemy.SetEnemyData(data2);

        // Assert - Should be clamped to 0.5
        Assert.AreEqual(new Vector3(0.5f, 0.5f, 0.5f), testObject.transform.localScale, "Visual scale should be clamped to min 0.5");

        Object.DestroyImmediate(data1);
        Object.DestroyImmediate(data2);
    }
}
#endif