using UnityEngine;
using NUnit.Framework;

/// <summary>
/// Tests for the PlayerHealth singleton system
/// </summary>
public class PlayerHealthTests
{
    private GameObject testPlayerObject;
    private PlayerHealth playerHealth;

    [SetUp]
    public void Setup()
    {
        // Create a test game object with PlayerHealth component
        testPlayerObject = new GameObject("TestPlayer");
        playerHealth = testPlayerObject.AddComponent<PlayerHealth>();
    }

    [TearDown]
    public void Teardown()
    {
        if (testPlayerObject != null)
            Object.Destroy(testPlayerObject);
        
        // Clean up singleton
        if (PlayerHealth.Instance == playerHealth)
        {
            Object.Destroy(playerHealth.gameObject);
        }
    }

    [Test]
    public void TestPlayerHealthSingletonAccessible()
    {
        // Assert that instance is accessible
        Assert.IsNotNull(PlayerHealth.Instance);
    }

    [Test]
    public void TestHealthInitializesToMax()
    {
        // Assert that current health equals max health on initialization
        Assert.AreEqual(playerHealth.GetMaxHealth(), playerHealth.GetCurrentHealth());
    }

    [Test]
    public void TestDamageReducesHealth()
    {
        float maxHealth = playerHealth.GetMaxHealth();
        float damageAmount = 10f;
        
        playerHealth.Damage(damageAmount);
        
        float expectedHealth = maxHealth - damageAmount;
        Assert.AreEqual(expectedHealth, playerHealth.GetCurrentHealth(), 0.01f);
    }

    [Test]
    public void TestHealthChangedEventFires()
    {
        bool eventFired = false;
        float eventCurrentHealth = -1;
        float eventMaxHealth = -1;
        
        playerHealth.OnHealthChanged += (current, max) =>
        {
            eventFired = true;
            eventCurrentHealth = current;
            eventMaxHealth = max;
        };
        
        float damageAmount = 5f;
        playerHealth.Damage(damageAmount);
        
        Assert.IsTrue(eventFired);
        Assert.AreEqual(playerHealth.GetMaxHealth() - damageAmount, eventCurrentHealth, 0.01f);
        Assert.AreEqual(playerHealth.GetMaxHealth(), eventMaxHealth, 0.01f);
    }

    [Test]
    public void TestHealthDoesNotGoBelowZero()
    {
        playerHealth.Damage(1000f);
        
        Assert.AreEqual(0f, playerHealth.GetCurrentHealth(), 0.01f);
    }

    [Test]
    public void TestDeathEventFires()
    {
        bool deathEventFired = false;
        playerHealth.OnDeath += () =>
        {
            deathEventFired = true;
        };
        
        playerHealth.Damage(playerHealth.GetMaxHealth() + 10f);
        
        Assert.IsTrue(deathEventFired);
    }

    [Test]
    public void TestIsAliveWhenHealthGreaterThanZero()
    {
        Assert.IsTrue(playerHealth.IsAlive());
        
        playerHealth.Damage(5f);
        Assert.IsTrue(playerHealth.IsAlive());
    }

    [Test]
    public void TestIsDeadWhenHealthIsZero()
    {
        playerHealth.Damage(playerHealth.GetMaxHealth());
        
        Assert.IsFalse(playerHealth.IsAlive());
    }

    [Test]
    public void TestRespawnResetsHealthToMax()
    {
        float maxHealth = playerHealth.GetMaxHealth();
        playerHealth.Damage(50f);
        
        // Store the original position for respawn check
        Vector3 originalPos = playerHealth.transform.position;
        
        playerHealth.Respawn(originalPos);
        
        Assert.AreEqual(maxHealth, playerHealth.GetCurrentHealth(), 0.01f);
        Assert.IsTrue(playerHealth.IsAlive());
    }

    [Test]
    public void TestRespawnEventFires()
    {
        bool respawnEventFired = false;
        playerHealth.OnRespawn += () =>
        {
            respawnEventFired = true;
        };
        
        playerHealth.Damage(50f);
        playerHealth.Respawn(Vector3.zero);
        
        Assert.IsTrue(respawnEventFired);
    }

    [Test]
    public void TestSetMaxHealth()
    {
        float newMaxHealth = 150f;
        playerHealth.SetMaxHealth(newMaxHealth);
        
        Assert.AreEqual(newMaxHealth, playerHealth.GetMaxHealth(), 0.01f);
        Assert.AreEqual(newMaxHealth, playerHealth.GetCurrentHealth(), 0.01f);
    }
}
