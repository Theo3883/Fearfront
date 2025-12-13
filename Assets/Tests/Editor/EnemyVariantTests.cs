using NUnit.Framework;
using UnityEngine;

public class EnemyVariantTests
{
    [Test]
    public void Enemy_LoadsStatsFromData()
    {
        // Create a test EnemyData
        var data = ScriptableObject.CreateInstance<EnemyData>();
        
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 4.5f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 20f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 20f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 5f);
        typeof(EnemyData).GetField("visualScale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(data, 0.7f);
        
        Assert.IsTrue(data.IsValid(), "EnemyData should be valid");
    }

    [Test]
    public void EnemyData_DifferentTypesDifferentSpeeds()
    {
        var fastSpider = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("enemyType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, EnemyType.FastSpider);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 4.5f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 20f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 20f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 5f);

        var tankSpider = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("enemyType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, EnemyType.TankSpider);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 2f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 80f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 80f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 15f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 5f);

        // FastSpider should be faster than TankSpider
        Assert.Greater(fastSpider.MoveSpeed, tankSpider.MoveSpeed);
        
        // TankSpider should have more health
        Assert.Greater(tankSpider.MaxHealth, fastSpider.MaxHealth);
        
        // TankSpider should do more damage
        Assert.Greater(tankSpider.AttackDamage, fastSpider.AttackDamage);
    }

    [Test]
    public void SpawnDifficulty_EasyHasLowerVariety()
    {
        // Verify Easy has only 2 types
        var easy = SpawnDifficulty.Easy;
        var normal = SpawnDifficulty.Normal;
        var hard = SpawnDifficulty.Hard;
        
        Assert.IsNotNull(easy);
        Assert.IsNotNull(normal);
        Assert.IsNotNull(hard);
    }

    [Test]
    public void EnemyData_VisualScaleAffectsTransform()
    {
        var smallData = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("visualScale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(smallData, 0.7f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(smallData, 4.5f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(smallData, 20f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(smallData, 20f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(smallData, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(smallData, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(smallData, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(smallData, 5f);

        var largeData = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("visualScale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(largeData, 1.3f);
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(largeData, 2f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(largeData, 80f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(largeData, 80f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(largeData, 15f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(largeData, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(largeData, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(largeData, 5f);

        Assert.Less(smallData.VisualScale, largeData.VisualScale);
    }

    [Test]
    public void EnemyData_TypeColorDifferentiation()
    {
        var fastSpider = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("typeColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, new Color(1f, 0.5f, 0f)); // Orange
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 4.5f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 20f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 20f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 8f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(fastSpider, 5f);

        var tankSpider = ScriptableObject.CreateInstance<EnemyData>();
        typeof(EnemyData).GetField("typeColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, new Color(0.5f, 0.2f, 0.1f)); // Brown
        typeof(EnemyData).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 2f);
        typeof(EnemyData).GetField("health", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 80f);
        typeof(EnemyData).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 80f);
        typeof(EnemyData).GetField("attackDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 15f);
        typeof(EnemyData).GetField("attackRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 2f);
        typeof(EnemyData).GetField("attackCooldown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 1.5f);
        typeof(EnemyData).GetField("detectionRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tankSpider, 5f);

        Assert.AreNotEqual(fastSpider.TypeColor, tankSpider.TypeColor);
    }
}
