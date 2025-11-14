using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manager central pentru interactiuni
/// Monitorizeaza progresul si gestioneaza logica jocului
/// </summary>
public class InteractionManager : MonoBehaviour
{
    [Header("Game Objects")]
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private GameObject spherePrefab;
    [SerializeField] private Transform cubeSpawnPoint;
    [SerializeField] private Transform sphereSpawnPoint;
    
    [Header("Zones")]
    [SerializeField] private PlacementZone cubeZone;
    [SerializeField] private PlacementZone sphereZone;
    
    [Header("Game State")]
    [SerializeField] private bool cubeInZone = false;
    [SerializeField] private bool sphereInZone = false;
    
    [Header("Feedback")]
    [SerializeField] private GameObject successIndicator;
    
    private List<string> completedActions = new List<string>();
    
    void Start()
    {
        Debug.Log("=== VR Interaction Prototype Started ===");
        Debug.Log("Obiective:");
        Debug.Log("1. Grab cube-ul cu controller-ul");
        Debug.Log("2. Plaseaza cube-ul in zona marcata");
        Debug.Log("3. Grab sphere-ul cu controller-ul");
        Debug.Log("4. Plaseaza sphere-ul in zona marcata");
        
        if (successIndicator != null)
        {
            successIndicator.SetActive(false);
        }
    }
    
    void Update()
    {
        // Verifica daca ambele obiecte sunt plasate
        CheckWinCondition();
    }
    
    public void OnObjectPlaced(GameObject obj, PlacementZone zone)
    {
        string actionKey = $"{obj.name}_in_{zone.name}";
        
        if (!completedActions.Contains(actionKey))
        {
            completedActions.Add(actionKey);
            Debug.Log($"✓ Action completed: {obj.name} placed in {zone.name}");
            Debug.Log($"Total actions completed: {completedActions.Count}");
        }
        
        // Update game state
        if (obj.CompareTag("Cube"))
        {
            cubeInZone = true;
        }
        else if (obj.CompareTag("Sphere"))
        {
            sphereInZone = true;
        }
    }
    
    private void CheckWinCondition()
    {
        if (cubeInZone && sphereInZone)
        {
            if (successIndicator != null && !successIndicator.activeSelf)
            {
                successIndicator.SetActive(true);
                Debug.Log("=== SUCCESS! All objects placed correctly! ===");
            }
        }
    }
    
    public void ResetGame()
    {
        Debug.Log("Resetting game...");
        
        cubeInZone = false;
        sphereInZone = false;
        completedActions.Clear();
        
        if (cubeZone != null)
        {
            cubeZone.ResetZone();
        }
        
        if (sphereZone != null)
        {
            sphereZone.ResetZone();
        }
        
        if (successIndicator != null)
        {
            successIndicator.SetActive(false);
        }
        
        // Respawn objects la pozitiile initiale
        RespawnObjects();
    }
    
    private void RespawnObjects()
    {
        // Cautare si repoziționare obiecte existente
        GameObject[] cubes = GameObject.FindGameObjectsWithTag("Cube");
        GameObject[] spheres = GameObject.FindGameObjectsWithTag("Sphere");
        
        if (cubes.Length > 0 && cubeSpawnPoint != null)
        {
            cubes[0].transform.position = cubeSpawnPoint.position;
            cubes[0].transform.rotation = cubeSpawnPoint.rotation;
            
            Rigidbody rb = cubes[0].GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        
        if (spheres.Length > 0 && sphereSpawnPoint != null)
        {
            spheres[0].transform.position = sphereSpawnPoint.position;
            spheres[0].transform.rotation = sphereSpawnPoint.rotation;
            
            Rigidbody rb = spheres[0].GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
    
    public int GetCompletedActionsCount()
    {
        return completedActions.Count;
    }
    
    public bool IsGameComplete()
    {
        return cubeInZone && sphereInZone;
    }
}

