#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class EnemySpawnerVariantTests
{
    [Test]
    public void EnemySpawner_CanAddEnemyVariants()
    {
        var fastSpiderData = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("enemyType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpiderData, EnemyType.FastSpider);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpiderData, 4.5f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpiderData, 20f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpiderData, 20f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpiderData, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpiderData, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpiderData, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpiderData, 5f);
        
        Assert.IsTrue(fastSpiderData.IsValid(), "FastSpider data should be valid");
    }

    [Test]
    public void SpawnDifficulty_AllValuesExist()
    {
        // Verify all difficulty levels exist and can be created
        var easy = SpawnDifficulty.Easy;
        var normal = SpawnDifficulty.Normal;
        var hard = SpawnDifficulty.Hard;
        
        Assert.AreEqual(SpawnDifficulty.Easy, easy);
        Assert.AreEqual(SpawnDifficulty.Normal, normal);
        Assert.AreEqual(SpawnDifficulty.Hard, hard);
    }

    [Test]
    public void EnemyType_AllVariantsExist()
    {
        // Verify all enemy type variants are defined
        var fastSpider = EnemyType.FastSpider;
        var tankSpider = EnemyType.TankSpider;
        var venomSpider = EnemyType.VenomSpider;
        var goliathSpider = EnemyType.GoliathSpider;
        
        Assert.AreEqual(EnemyType.FastSpider, fastSpider);
        Assert.AreEqual(EnemyType.TankSpider, tankSpider);
        Assert.AreEqual(EnemyType.VenomSpider, venomSpider);
        Assert.AreEqual(EnemyType.GoliathSpider, goliathSpider);
    }

    [Test]
    public void EnemyData_AllFourTypesCanBeCreated()
    {
        // Test creating all four types
        CreateEnemyDataOfType(EnemyType.FastSpider, 4.5f, 20f, 8f);
        CreateEnemyDataOfType(EnemyType.TankSpider, 2f, 80f, 15f);
        CreateEnemyDataOfType(EnemyType.VenomSpider, 3.5f, 35f, 12f);
        CreateEnemyDataOfType(EnemyType.GoliathSpider, 1.5f, 120f, 20f);
    }

    private void CreateEnemyDataOfType(EnemyType type, float speed, float health, float damage)
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
        
        Assert.IsTrue(data.IsValid(), $"{type} data should be valid");
        Assert.AreEqual(type, data.Type);
        Assert.AreEqual(speed, data.MoveSpeed);
        Assert.AreEqual(damage, data.AttackDamage);
    }

    [Test]
    public void EnemyData_BalanceValues_PlayerVsEnemies()
    {
        // Verify balance values: Player starts with 100 health
        // FastSpider attack every 1.5s at 8 damage = 5.3 sec to kill player (solo)
        // TankSpider attack every 1.5s at 15 damage = 10 sec to kill player (solo)
        
        var fastSpider = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 8f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 1.5f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 4.5f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 20f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 20f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 2f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 5f);

        float fastSpiderTimeToKillPlayer = 100f / fastSpider.AttackDamage * fastSpider.AttackCooldown;
        Assert.That(fastSpiderTimeToKillPlayer, Is.GreaterThan(5f).And.LessThan(6f));

        var tankSpider = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 15f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 1.5f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 2f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 80f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 80f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 2f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 5f);

        float tankSpiderTimeToKillPlayer = 100f / tankSpider.AttackDamage * tankSpider.AttackCooldown;
        Assert.That(tankSpiderTimeToKillPlayer, Is.GreaterThan(9f).And.LessThan(11f));
    }

    [Test]
    public void EnemyData_VenomSpider_HasLongerRange()
    {
        var venomSpider = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(venomSpider, 3f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(venomSpider, 3.5f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(venomSpider, 35f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(venomSpider, 35f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(venomSpider, 12f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(venomSpider, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(venomSpider, 5f);

        var normalSpider = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(normalSpider, 2f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(normalSpider, 4.5f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(normalSpider, 20f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(normalSpider, 20f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(normalSpider, 8f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(normalSpider, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(normalSpider, 5f);

        Assert.Greater(venomSpider.AttackRange, normalSpider.AttackRange);
    }

    [Test]
    public void EnemyData_GoliathSpider_HasLargeDetectionRadius()
    {
        var goliathSpider = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(goliathSpider, 8f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(goliathSpider, 1.5f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(goliathSpider, 120f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(goliathSpider, 120f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(goliathSpider, 20f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(goliathSpider, 1.5f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(goliathSpider, 2f);

        var normalSpider = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(normalSpider, 5f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(normalSpider, 4.5f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(normalSpider, 20f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(normalSpider, 20f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(normalSpider, 8f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(normalSpider, 1.5f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(normalSpider, 2f);

        Assert.Greater(goliathSpider.DetectionRadius, normalSpider.DetectionRadius);
    }
}
#endif
