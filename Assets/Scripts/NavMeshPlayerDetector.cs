using UnityEngine;
using UnityEngine.AI;
using System;

/// <summary>
/// NavMeshPlayerDetector detects whether the player is on or off the NavMesh.
/// Uses NavMesh.SamplePosition() with a configurable tolerance to determine NavMesh status.
/// </summary>
public class NavMeshPlayerDetector : MonoBehaviour
{
    [SerializeField] private float detectionTolerance = 1.5f;
    
    [SerializeField] private Transform playerTransform;
    private bool lastDetectedStatus = false;
    private bool statusInitialized = false;
    
    /// <summary>
    /// Event fired when player's NavMesh status changes
    /// </summary>
    public event Action<bool> OnPlayerNavMeshStatusChanged;

    /// <summary>
    /// Sets the player reference for detection
    /// </summary>
    public void SetPlayerReference(Transform player)
    {
        playerTransform = player;
        statusInitialized = false; // Reset status tracking when new player is set
    }

    /// <summary>
    /// Checks if the player is currently on the NavMesh
    /// </summary>
    /// <returns>True if player is on NavMesh, false otherwise</returns>
    public bool IsPlayerOnNavMesh()
    {
        if (playerTransform == null)
        {
            return false;
        }

        // Use NavMesh.SamplePosition to check if player position is on the NavMesh
        NavMeshHit hit;
        bool isOnNavMesh = NavMesh.SamplePosition(
            playerTransform.position, 
            out hit, 
            detectionTolerance, 
            NavMesh.AllAreas
        );

        // Check for status change and fire event if status has changed
        if (statusInitialized && isOnNavMesh != lastDetectedStatus)
        {
            OnPlayerNavMeshStatusChanged?.Invoke(isOnNavMesh);
        }

        // Update tracking variables
        lastDetectedStatus = isOnNavMesh;
        statusInitialized = true;

        return isOnNavMesh;
    }

    /// <summary>
    /// Sets the detection tolerance (how far from NavMesh to still consider "on NavMesh")
    /// </summary>
    public void SetDetectionTolerance(float tolerance)
    {
        detectionTolerance = Mathf.Max(0.1f, tolerance); // Ensure positive value
    }

    /// <summary>
    /// Gets the current detection tolerance
    /// </summary>
    public float GetDetectionTolerance()
    {
        return detectionTolerance;
    }
}
