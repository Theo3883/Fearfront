#if UNITY_EDITOR
using UnityEngine;
using NUnit.Framework;

/// <summary>
/// Tests for the PlayerDamage component
/// </summary>
public class PlayerDamageTests
{
    private GameObject testPlayerObject;
    private PlayerDamage playerDamage;
    private PlayerHealth playerHealth;

    [SetUp]
    public void Setup()
    {
        // Create a test game object with both components
        testPlayerObject = new GameObject("TestPlayer");
        playerHealth = testPlayerObject.AddComponent<PlayerHealth>();
        playerDamage = testPlayerObject.AddComponent<PlayerDamage>();
    }

    [TearDown]
    public void Teardown()
    {
        if (testPlayerObject != null)
            Object.Destroy(testPlayerObject);
    }

    [Test]
    public void TestPlayerDamageReferencesPlayerHealth()
    {
        // Assert that PlayerDamage can access PlayerHealth
        Assert.IsNotNull(playerDamage.GetPlayerHealth());
    }

    [Test]
    public void TestTakeDamageReducesHealth()
    {
        float initialHealth = playerHealth.GetCurrentHealth();
        float damageAmount = 10f;
        
        playerDamage.TakeDamage(damageAmount);
        
        Assert.AreEqual(initialHealth - damageAmount, playerHealth.GetCurrentHealth(), 0.01f);
    }

    [Test]
    public void TestDamageHistoryIsTracked()
    {
        playerDamage.TakeDamage(5f);
        playerDamage.TakeDamage(10f);
        
        // Verify damage was recorded (through health reduction)
        float expectedHealth = playerHealth.GetMaxHealth() - 15f;
        Assert.AreEqual(expectedHealth, playerHealth.GetCurrentHealth(), 0.01f);
    }

    [Test]
    public void TestMultipleDamageEvents()
    {
        int eventCount = 0;
        playerHealth.OnHealthChanged += (current, max) =>
        {
            eventCount++;
        };
        
        playerDamage.TakeDamage(5f);
        playerDamage.TakeDamage(5f);
        playerDamage.TakeDamage(5f);
        
        Assert.AreEqual(3, eventCount);
    }
}
#endif
