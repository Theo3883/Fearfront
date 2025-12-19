using UnityEngine;
using UnityEngine.AI;
using System;

/// <summary>
/// NavMeshPlayerDetector detects whether the player is on or off the NavMesh.
/// Uses NavMesh.SamplePosition() with a configurable tolerance to determine NavMesh status.
/// </summary>
public class NavMeshPlayerDetector : MonoBehaviour
{
    [SerializeField] private float detectionTolerance = 10f;
    
    [SerializeField] private Transform playerTransform;
    private bool lastDetectedStatus = false;
    private bool statusInitialized = false;
    
    /// <summary>
    /// Event fired when player's NavMesh status changes
    /// </summary>
    public event Action<bool> OnPlayerNavMeshStatusChanged;

    private void Start()
    {
        // Auto-find player if not assigned
        if (playerTransform == null)
        {
            AutoFindPlayer();
        }
    }

    /// <summary>
    /// Auto-finds the player by tag "Player" or PlayerHealth singleton
    /// </summary>
    private void AutoFindPlayer()
    {
        // Try PlayerHealth singleton first
        if (PlayerHealth.Instance != null)
        {
            playerTransform = PlayerHealth.Instance.transform;
            return;
        }

        // Try finding by tag
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            return;
        }

        Debug.LogWarning($"NavMeshPlayerDetector on '{gameObject.name}' could not auto-find player. Assign manually or ensure player has 'Player' tag.");
    }

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
