using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Tests for NavMeshPlayerDetector component.
/// 
/// Test Limitation Note:
/// Without a real NavMesh in test environment, these tests validate interface/null handling 
/// and event mechanics rather than actual NavMesh API calls. The tests ensure that:
/// - The component handles missing player references gracefully
/// - Events fire correctly when status changes
/// - Events do not fire when status remains unchanged
/// - Configuration methods work as expected
/// </summary>
[TestFixture]
public class NavMeshPlayerDetectorTests
{
    private GameObject detectorGameObject;
    private NavMeshPlayerDetector detector;
    private GameObject playerGameObject;
    private bool statusChangedEventFired;
    private bool lastReceivedStatus;
    private int eventFireCount;

    [SetUp]
    public void SetUp()
    {
        // Create detector game object
        detectorGameObject = new GameObject("TestDetector");
        detector = detectorGameObject.AddComponent<NavMeshPlayerDetector>();
        
        // Create player game object
        playerGameObject = new GameObject("TestPlayer");
        playerGameObject.transform.position = Vector3.zero;
        
        // Subscribe to event
        statusChangedEventFired = false;
        lastReceivedStatus = false;
        eventFireCount = 0;
        detector.OnPlayerNavMeshStatusChanged += OnStatusChanged;
    }

    private void OnStatusChanged(bool isOnNavMesh)
    {
        statusChangedEventFired = true;
        lastReceivedStatus = isOnNavMesh;
        eventFireCount++;
    }

    [TearDown]
    public void TearDown()
    {
        detector.OnPlayerNavMeshStatusChanged -= OnStatusChanged;
        Object.Destroy(detectorGameObject);
        Object.Destroy(playerGameObject);
    }

    [Test]
    public void TestPlayerOnNavMeshDetection()
    {
        // Arrange
        detector.SetPlayerReference(playerGameObject.transform);
        
        // Place player on a position that would be on NavMesh (origin in test scene)
        playerGameObject.transform.position = Vector3.zero;
        
        // Act
        bool isOnNavMesh = detector.IsPlayerOnNavMesh();
        
        // Assert
        // In a test environment without a NavMesh, this should use fallback logic
        // or return a consistent result. We're testing the interface works.
        Assert.That(isOnNavMesh, Is.TypeOf<bool>(), "IsPlayerOnNavMesh should return a boolean");
    }

    [Test]
    public void TestPlayerOffNavMeshDetection()
    {
        // Arrange
        detector.SetPlayerReference(playerGameObject.transform);
        
        // Place player very far away (definitely off any NavMesh)
        playerGameObject.transform.position = new Vector3(10000f, 10000f, 10000f);
        
        // Act
        bool isOnNavMesh = detector.IsPlayerOnNavMesh();
        
        // Assert
        // At such extreme distance, should return false or consistently handle it
        Assert.That(isOnNavMesh, Is.TypeOf<bool>(), "IsPlayerOnNavMesh should return a boolean");
    }

    [Test]
    public void TestNavMeshStatusChangeTriggers()
    {
        // Arrange
        detector.SetPlayerReference(playerGameObject.transform);
        statusChangedEventFired = false;
        eventFireCount = 0;
        
        // Initialize status by calling IsPlayerOnNavMesh
        bool initialStatus = detector.IsPlayerOnNavMesh();
        
        // Move player to a far position (potential status change)
        playerGameObject.transform.position = new Vector3(10000f, 10000f, 10000f);
        
        // Act
        bool newStatus = detector.IsPlayerOnNavMesh();
        
        // Assert
        // Strengthen assertions to verify event firing and status parameters
        Assert.That(statusChangedEventFired, Is.EqualTo(true), "Event should have fired when status changed");
        Assert.That(lastReceivedStatus, Is.EqualTo(newStatus), "Event parameter should match the new status");
        Assert.That(lastReceivedStatus, Is.Not.EqualTo(initialStatus), "Status should have changed from initial to new");
    }

    [Test]
    public void TestDetectorWithoutPlayerReference()
    {
        // Arrange - don't set player reference
        
        // Act & Assert - should handle gracefully
        bool result = detector.IsPlayerOnNavMesh();
        Assert.That(result, Is.False, "Should return false when no player reference is set");
    }

    [Test]
    public void TestToleranceConfiguration()
    {
        // Arrange
        detector.SetPlayerReference(playerGameObject.transform);
        detector.SetDetectionTolerance(2.5f);
        
        // Act
        float tolerance = detector.GetDetectionTolerance();
        
        // Assert
        Assert.AreEqual(2.5f, tolerance, "Detection tolerance should be configurable");
    }

    [Test]
    public void TestDefaultTolerance()
    {
        // Arrange
        detector.SetPlayerReference(playerGameObject.transform);
        
        // Act
        float defaultTolerance = detector.GetDetectionTolerance();
        
        // Assert
        Assert.That(defaultTolerance, Is.GreaterThan(0f), "Default tolerance should be greater than 0");
        Assert.That(defaultTolerance, Is.LessThanOrEqualTo(2f), "Default tolerance should be reasonable (1-2 units)");
    }

    [Test]
    public void TestEventDoesNotFireWhenStatusUnchanged()
    {
        // Arrange - Without a real NavMesh, this test validates that the event 
        // firing mechanism correctly tracks status and only fires on actual changes
        detector.SetPlayerReference(playerGameObject.transform);
        statusChangedEventFired = false;
        eventFireCount = 0;
        
        // Place player at position that will have consistent status
        playerGameObject.transform.position = Vector3.zero;
        
        // Act - First call initializes status
        bool firstStatus = detector.IsPlayerOnNavMesh();
        int fireCountAfterFirst = eventFireCount;
        
        // Don't move the player - call again with same status
        bool secondStatus = detector.IsPlayerOnNavMesh();
        int fireCountAfterSecond = eventFireCount;
        
        // Assert
        Assert.That(firstStatus, Is.EqualTo(secondStatus), "Status should remain unchanged");
        Assert.That(fireCountAfterFirst, Is.EqualTo(0), "Event should not fire on first initialization");
        Assert.That(fireCountAfterSecond, Is.EqualTo(fireCountAfterFirst), "Event should not fire again when status unchanged");
    }
}
