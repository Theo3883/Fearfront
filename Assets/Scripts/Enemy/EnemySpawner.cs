using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private EnemyRoute[] availableRoutes;
    [SerializeField] private int enemiesToSpawn = 10;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool infiniteWaves = false;
    [SerializeField] private float delayBetweenWaves = 3f;
    [SerializeField] private float waveTimeThreshold = 30f;

    // Phase 4 - Enemy type variants
    [SerializeField] private List<EnemyData> enemyTypeVariants = new List<EnemyData>();
    [SerializeField] private SpawnDifficulty difficultyPreset = SpawnDifficulty.Normal;

    private int waveCount = 0;
    private float waveStartTime = 0f;

    private void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy prefab not assigned!");
            return;
        }

        if (availableRoutes == null || availableRoutes.Length == 0)
        {
            Debug.LogError("No routes assigned!");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("Spawn point not assigned, using this transform's position");
            spawnPoint = transform;
        }

        StartCoroutine(SpawnWavesCoroutine());
    }

    private IEnumerator SpawnWavesCoroutine()
    {
        while (true)
        {
            waveCount++;
            waveStartTime = Time.time;
            
            yield return StartCoroutine(SpawnWaveCoroutine());
            
            if (!infiniteWaves)
                break;
            
            yield return new WaitForSeconds(delayBetweenWaves);
        }
    }

    private IEnumerator SpawnWaveCoroutine()
    {
        int spawnedInWave = 0;
        float waveEndTime = Time.time + waveTimeThreshold;

        while (Time.time < waveEndTime)
        {
            if (spawnedInWave < enemiesToSpawn)
            {
                SpawnEnemy();
                spawnedInWave++;
                yield return new WaitForSeconds(spawnInterval);
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// Spawn an enemy - public version for testing and external calls
    /// </summary>
    public void SpawnEnemy()
    {
        // Sample spawn point to nearest NavMesh position
        NavMeshHit hit;
        Vector3 spawnPosition = spawnPoint.position;
        if (NavMesh.SamplePosition(spawnPoint.position, out hit, 10.0f, NavMesh.AllAreas))
        {
            spawnPosition = hit.position;
        }
        else
        {
            Debug.LogWarning("Spawn point not on NavMesh. Enemies may not navigate correctly.");
        }
        
        GameObject newEnemyObject = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        
        Enemy enemy = newEnemyObject.GetComponent<Enemy>();
        if (enemy == null)
        {
            Debug.LogError("Enemy prefab doesn't have an Enemy component!");
            Destroy(newEnemyObject);
            return;
        }

        EnemyRoute randomRoute = GetRandomRoute();
        if (randomRoute == null || !randomRoute.IsValid())
        {
            Debug.LogError("Selected route is invalid!");
            Destroy(newEnemyObject);
            return;
        }

        Transform[] waypoints = randomRoute.GetWaypoints();
        
        // CRITICAL: Set enemy data BEFORE Initialize() to avoid null warnings
        EnemyData selectedEnemyData = GetRandomEnemyType();
        if (selectedEnemyData != null)
        {
            enemy.SetEnemyData(selectedEnemyData);
        }
        
        // Now initialize with data already set
        enemy.Initialize(waypoints, this);
        
        // Initialize refactored components (now with EnemyData already assigned)
        if (selectedEnemyData != null)
        {
            InitializeEnemyComponents(newEnemyObject, waypoints, selectedEnemyData);
        }
    }

    /// <summary>
    /// Initialize the three refactored components on a spawned enemy
    /// Allows partial initialization - if playerHealth is null, logs warning but continues initialization of movement and detector
    /// Only fails if components themselves cannot be added to the enemy
    /// NOTE: EnemyMovement is already initialized by Enemy.Initialize(), so we skip it here
    /// </summary>
    private void InitializeEnemyComponents(GameObject enemyObject, Transform[] waypoints, EnemyData enemyData)
    {
        // Find or get player reference
        PlayerHealth playerHealth = FindPlayerHealth();
        if (playerHealth == null)
        {
            Debug.LogWarning("Could not find player or PlayerHealth component. Enemy movement and detection will work, but state machine may not engage properly without player reference.");
        }

        // Get/add NavMeshPlayerDetector and initialize it
        NavMeshPlayerDetector detector = enemyObject.GetComponent<NavMeshPlayerDetector>();
        if (detector == null)
        {
            detector = enemyObject.AddComponent<NavMeshPlayerDetector>();
        }
        
        // Only set player reference if player was found
        if (playerHealth != null)
        {
            detector.SetPlayerReference(playerHealth.transform);
        }

        // EnemyMovement is already initialized by Enemy.Initialize(), so we don't re-initialize it here
        // This avoids double-initialization of waypoints

        // Get/add EnemyStateMachine and initialize it
        EnemyStateMachine stateMachine = enemyObject.GetComponent<EnemyStateMachine>();
        if (stateMachine == null)
        {
            stateMachine = enemyObject.AddComponent<EnemyStateMachine>();
        }
        
        // Initialize state machine with player health (may be null, state machine should handle this)
        stateMachine.Initialize(detector, enemyData.DetectionRadius, playerHealth);
    }

    /// <summary>
    /// Randomly selects an enemy type variant and applies it to the enemy
    /// </summary>
    private void RandomizeEnemyType(Enemy enemy)
    {
        EnemyData selectedType = GetRandomEnemyType();
        if (selectedType != null)
        {
            enemy.SetEnemyData(selectedType);
        }
    }

    /// <summary>
    /// Gets a random enemy type based on difficulty preset
    /// </summary>
    private EnemyData GetRandomEnemyType()
    {
        if (enemyTypeVariants.Count == 0)
        {
            Debug.LogWarning("EnemySpawner has no Enemy Type Variants assigned! Spawning with default values.");
            return null;
        }

        float rand = Random.value;
        float cumulativeChance = 0f;

        // Get difficulty distribution
        (EnemyType[] types, float[] chances) = GetDifficultyDistribution();

        for (int i = 0; i < types.Length; i++)
        {
            cumulativeChance += chances[i];
            if (rand <= cumulativeChance)
            {
                return FindEnemyDataByType(types[i]);
            }
        }

        // Fallback to first available
        return enemyTypeVariants[0];
    }

    /// <summary>
    /// Gets the type distribution and probabilities based on difficulty
    /// </summary>
    private (EnemyType[], float[]) GetDifficultyDistribution()
    {
        switch (difficultyPreset)
        {
            case SpawnDifficulty.Easy:
                return (
                    new[] { EnemyType.FastSpider, EnemyType.TankSpider },
                    new[] { 0.7f, 0.3f }
                );

            case SpawnDifficulty.Normal:
                return (
                    new[] { EnemyType.FastSpider, EnemyType.TankSpider, EnemyType.VenomSpider },
                    new[] { 0.5f, 0.3f, 0.2f }
                );

            case SpawnDifficulty.Hard:
                return (
                    new[] { EnemyType.FastSpider, EnemyType.TankSpider, EnemyType.VenomSpider, EnemyType.GoliathSpider },
                    new[] { 0.3f, 0.3f, 0.25f, 0.15f }
                );

            default:
                return (
                    new[] { EnemyType.FastSpider },
                    new[] { 1f }
                );
        }
    }

    /// <summary>
    /// Finds EnemyData by type
    /// </summary>
    private EnemyData FindEnemyDataByType(EnemyType type)
    {
        foreach (EnemyData data in enemyTypeVariants)
        {
            if (data != null && data.Type == type && data.IsValid())
            {
                return data;
            }
        }
        Debug.LogWarning($"No EnemyData found for type {type} in variants list.");
        return null;
    }

    /// <summary>
    /// Sets the difficulty preset for spawning
    /// </summary>
    public void SetDifficultyPreset(SpawnDifficulty difficulty)
    {
        difficultyPreset = difficulty;
    }

    /// <summary>
    /// Adds an enemy type variant to available types
    /// </summary>
    public void AddEnemyTypeVariant(EnemyData data)
    {
        if (data != null && !enemyTypeVariants.Contains(data))
        {
            enemyTypeVariants.Add(data);
        }
    }

    private EnemyRoute GetRandomRoute()
    {
        if (availableRoutes.Length == 0)
            return null;

        int randomIndex = Random.Range(0, availableRoutes.Length);
        return availableRoutes[randomIndex];
    }

    /// <summary>
    /// Find the PlayerHealth component in the scene
    /// Tries multiple methods: tag lookup, FindObjectOfType, singleton access
    /// </summary>
    private PlayerHealth FindPlayerHealth()
    {
        // First try PlayerHealth singleton
        if (PlayerHealth.Instance != null)
            return PlayerHealth.Instance;

        // Try to find by tag
        GameObject playerObject = null;
        try { playerObject = GameObject.FindWithTag("Player"); }
        catch { playerObject = null; }
        
        if (playerObject != null)
        {
            PlayerHealth ph = playerObject.GetComponent<PlayerHealth>();
            if (ph != null)
                return ph;
        }

        // Try FindFirstObjectByType as last resort
        return FindFirstObjectByType<PlayerHealth>();
    }

    public void OnEnemyReachedEnd(Enemy enemy)
    {
    }
    
    public void SetEnemiesToSpawn(int count) { enemiesToSpawn = count; }
    public void SetSpawnInterval(float interval) { spawnInterval = Mathf.Max(0.1f, interval); }
    public void SetInfiniteWaves(bool infinite) { infiniteWaves = infinite; }
    public void SetWaveTimeThreshold(float threshold) { waveTimeThreshold = Mathf.Max(0.1f, threshold); }
    public int GetWaveCount() { return waveCount; }
    
    /// <summary>
    /// Public setter for enemy prefab (for testing)
    /// </summary>
    public void SetEnemyPrefab(GameObject prefab)
    {
        enemyPrefab = prefab;
    }

    /// <summary>
    /// Public setter for spawn point (for testing)
    /// </summary>
    public void SetSpawnPoint(Transform spawnPointTransform)
    {
        spawnPoint = spawnPointTransform;
    }
}