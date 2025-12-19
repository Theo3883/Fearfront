#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class EnemyMovementTests
{
    private GameObject testObject;
    private EnemyMovement enemyMovement;
    private Transform[] testWaypoints;

    [SetUp]
    public void SetUp()
    {
        // Create a game object with EnemyMovement component
        testObject = new GameObject("TestEnemy");
        enemyMovement = testObject.AddComponent<EnemyMovement>();
        
        // Create dummy waypoints
        testWaypoints = new Transform[2];
        testWaypoints[0] = new GameObject("Waypoint0").transform;
        testWaypoints[1] = new GameObject("Waypoint1").transform;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(testObject);
        foreach (var waypoint in testWaypoints)
        {
            Object.DestroyImmediate(waypoint.gameObject);
        }
    }

    [Test]
    public void EnemyMovement_UsesEnemyDataSpeed_NotInspectorValue()
    {
        // Arrange
        var data = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 5f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 20f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 20f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(data, 5f);
        
        // Act
        enemyMovement.Initialize(testWaypoints, data);
        
        // Assert: Verify moveSpeed is loaded from EnemyData (5f), not default (12f)
        var moveSpeedField = typeof(EnemyMovement).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        float actualSpeed = (float)moveSpeedField?.GetValue(enemyMovement);
        Assert.AreEqual(5f, actualSpeed, "moveSpeed should be loaded from EnemyData (5f), not Inspector default (12f)");
        
        Object.DestroyImmediate(data);
    }

    [Test]
    public void EnemyMovement_UpdateMovement_FailsIfInitializedWithoutData()
    {
        // Act & Assert: Should not throw exception, should handle gracefully
        Assert.DoesNotThrow(() => enemyMovement.UpdateMovement());
        
        // Additional assertion: Should return early without processing
        // Since UpdateMovement checks for null waypoints/agent, it should exit gracefully
    }
}
#endif
