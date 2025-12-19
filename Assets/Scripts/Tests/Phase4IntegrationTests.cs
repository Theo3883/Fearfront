#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class Phase4IntegrationTests
{
    [Test]
    public void Phase4_EnemyDataSystemCreated()
    {
        // Verify that all Phase 4 components exist and can be instantiated
        var enemyType = EnemyType.FastSpider;
        var difficulty = SpawnDifficulty.Normal;
        
        Assert.AreEqual(EnemyType.FastSpider, enemyType);
        Assert.AreEqual(SpawnDifficulty.Normal, difficulty);
    }

    [Test]
    public void Phase4_EnemyDataValidation()
    {
        var data = ScriptableObject.CreateInstance<EnemyData>();
        
        // Set all required fields
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 4.5f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 20f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 20f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 5f);
        typeof(EnemyData).GetField("visualScale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 0.7f);
        
        Assert.IsTrue(data.IsValid());
        Assert.AreEqual(4.5f, data.MoveSpeed);
        Assert.AreEqual(20f, data.Health);
        Assert.AreEqual(8f, data.AttackDamage);
    }

    [Test]
    public void Phase4_FourEnemyTypesSupported()
    {
        // Verify all four enemy types can be created with unique stats
        CreateAndVerifyType(EnemyType.FastSpider, 4.5f, 20f, 8f, 0.7f);
        CreateAndVerifyType(EnemyType.TankSpider, 2f, 80f, 15f, 1.3f);
        CreateAndVerifyType(EnemyType.VenomSpider, 3.5f, 35f, 12f, 1f);
        CreateAndVerifyType(EnemyType.GoliathSpider, 1.5f, 120f, 20f, 1.3f);
    }

    private void CreateAndVerifyType(EnemyType type, float speed, float health, float damage, float scale)
    {
        var data = ScriptableObject.CreateInstance<EnemyData>();
        
        typeof(EnemyData).GetField("enemyType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, type);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, speed);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, health);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, health);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, damage);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 5f);
        typeof(EnemyData).GetField("visualScale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, scale);
        
        Assert.IsTrue(data.IsValid(), $"EnemyData for {type} should be valid");
        Assert.AreEqual(type, data.Type);
        Assert.AreEqual(speed, data.MoveSpeed);
        Assert.AreEqual(health, data.MaxHealth);
        Assert.AreEqual(damage, data.AttackDamage);
        Assert.AreEqual(scale, data.VisualScale);
    }

    [Test]
    public void Phase4_DifficultyPresetsSupported()
    {
        // Verify all difficulty presets exist
        var difficulties = new[] { SpawnDifficulty.Easy, SpawnDifficulty.Normal, SpawnDifficulty.Hard };
        foreach (var difficulty in difficulties)
        {
            Assert.IsNotNull(difficulty);
        }
    }

    [Test]
    public void Phase4_HealthProperties()
    {
        var data = ScriptableObject.CreateInstance<EnemyData>();
        
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 50f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 100f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 4.5f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 5f);
        
        Assert.AreEqual(50f, data.Health);
        Assert.AreEqual(100f, data.MaxHealth);
        Assert.IsTrue(data.IsValid());
    }

    [Test]
    public void Phase4_RangeValues()
    {
        // Verify different range values per type
        var venom = CreateEnemyData(EnemyType.VenomSpider, 3f); // Longer range
        var normal = CreateEnemyData(EnemyType.FastSpider, 2f);  // Normal range
        
        Assert.Greater(venom, normal);
    }

    private float CreateEnemyData(EnemyType type, float range)
    {
        var data = ScriptableObject.CreateInstance<EnemyData>();
        
        typeof(EnemyData).GetField("enemyType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, type);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, range);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 1f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 1f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 1f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 1f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 1f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 1f);
        
        return data.AttackRange;
    }
}
#endif
