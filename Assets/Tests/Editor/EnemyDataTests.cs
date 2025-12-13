using NUnit.Framework;
using UnityEngine;

public class EnemyDataTests
{
    [Test]
    public void EnemyData_FastSpider_HasCorrectStats()
    {
        var data = ScriptableObject.CreateInstance<EnemyData>();
        
        // Set values via reflection (since fields are private)
        typeof(EnemyData).GetField("enemyName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, "FastSpider");
        typeof(EnemyData).GetField("enemyType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, EnemyType.FastSpider);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 4.5f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 20f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 20f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 5f);
        typeof(EnemyData).GetField("visualScale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 0.7f);
        
        Assert.AreEqual(EnemyType.FastSpider, data.Type);
        Assert.AreEqual(4.5f, data.MoveSpeed);
        Assert.AreEqual(20f, data.Health);
        Assert.AreEqual(20f, data.MaxHealth);
        Assert.AreEqual(8f, data.AttackDamage);
    }

    [Test]
    public void EnemyData_TankSpider_HasCorrectStats()
    {
        var data = ScriptableObject.CreateInstance<EnemyData>();
        
        typeof(EnemyData).GetField("enemyName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, "TankSpider");
        typeof(EnemyData).GetField("enemyType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, EnemyType.TankSpider);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 2f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 80f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 80f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 15f);
        typeof(EnemyData).GetField("visualScale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 1.3f);
        
        Assert.AreEqual(EnemyType.TankSpider, data.Type);
        Assert.AreEqual(2f, data.MoveSpeed);
        Assert.AreEqual(80f, data.Health);
        Assert.AreEqual(15f, data.AttackDamage);
    }

    [Test]
    public void EnemyData_VenomSpider_HasCorrectRange()
    {
        var data = ScriptableObject.CreateInstance<EnemyData>();
        
        typeof(EnemyData).GetField("enemyType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, EnemyType.VenomSpider);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 3f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 3.5f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 35f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 35f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 12f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 5f);
        
        Assert.AreEqual(3f, data.AttackRange);
    }

    [Test]
    public void EnemyData_GoliathSpider_HasLargeDetectionRadius()
    {
        var data = ScriptableObject.CreateInstance<EnemyData>();
        
        typeof(EnemyData).GetField("enemyType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, EnemyType.GoliathSpider);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 8f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 1.5f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 120f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 120f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 20f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 1.5f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 2f);
        
        Assert.AreEqual(8f, data.DetectionRadius);
    }

    [Test]
    public void EnemyData_Validation_FailsWithZeroSpeed()
    {
        var data = ScriptableObject.CreateInstance<EnemyData>();
        
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 0f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 20f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 20f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 5f);
        
        Assert.IsFalse(data.IsValid());
    }

    [Test]
    public void EnemyData_Validation_FailsWithHealthExceedingMax()
    {
        var data = ScriptableObject.CreateInstance<EnemyData>();
        
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 4.5f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 100f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 50f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 5f);
        
        Assert.IsFalse(data.IsValid());
    }

    [Test]
    public void EnemyData_Validation_PassesWithValidValues()
    {
        var data = ScriptableObject.CreateInstance<EnemyData>();
        
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 4.5f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 20f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 20f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 5f);
        typeof(EnemyData).GetField("visualScale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 0.7f);
        
        Assert.IsTrue(data.IsValid());
    }

    [Test]
    public void EnemyData_VisualScale_RespectsConstraints()
    {
        var data = ScriptableObject.CreateInstance<EnemyData>();
        
        typeof(EnemyData).GetField("visualScale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 1.3f);
        
        Assert.That(data.VisualScale, Is.GreaterThanOrEqualTo(0.1f).And.LessThanOrEqualTo(2f));
    }

    [TearDown]
    public void Cleanup()
    {
        // Clean up any created assets
    }
}
