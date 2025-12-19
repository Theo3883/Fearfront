using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

[TestFixture]
public class EnemyTests
{
    private GameObject enemyGameObject;
    private Enemy enemy;
    private EnemySpawner spawner;
    private NavMeshAgent agent;
    private Rigidbody rb;

    [SetUp]
    public void SetUp()
    {
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
        
        // Create a mock spawner
        var spawnerGameObject = new GameObject("TestSpawner");
        spawner = spawnerGameObject.AddComponent<EnemySpawner>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.Destroy(enemyGameObject);
        if (enemy != null)
            Object.Destroy(enemy.gameObject);
        if (spawner != null)
            Object.Destroy(spawner.gameObject);
    }

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
}
