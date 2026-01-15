using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    private enum EnemyPrefabFamily
    {
        Auto,
        Spider,
        Chicken,
        Ghost
    }

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
    [SerializeField] private EnemyPrefabFamily prefabFamily = EnemyPrefabFamily.Auto;
    [SerializeField] private bool autoLoadVariantsFromResourcesOnStart = true;

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

        if (autoLoadVariantsFromResourcesOnStart)
        {
            LoadEnemyVariantsFromResources();
        }

        StartCoroutine(SpawnWavesCoroutine());
    }

    private void LoadEnemyVariantsFromResources()
    {
        EnemyData[] loaded = Resources.LoadAll<EnemyData>("EnemyVariants");
        if (loaded == null || loaded.Length == 0)
        {
            return;
        }

        for (int i = 0; i < loaded.Length; i++)
        {
            AddEnemyTypeVariant(loaded[i]);
        }
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

        EnemyPrefabFamily family = ResolvePrefabFamily();

        float rand = Random.value;
        float cumulativeChance = 0f;

        // Get difficulty distribution
        (EnemyType[] types, float[] chances) = GetDifficultyDistribution(family);

        // Convert distribution into available candidates (ignore missing types)
        List<EnemyData> candidates = new List<EnemyData>(types.Length);
        List<float> candidateChances = new List<float>(types.Length);
        float totalChance = 0f;

        for (int i = 0; i < types.Length; i++)
        {
            EnemyData data = FindEnemyDataByType(types[i]);
            if (data == null)
            {
                continue;
            }

            float chance = Mathf.Max(0f, chances[i]);
            if (chance <= 0f)
            {
                continue;
            }

            candidates.Add(data);
            candidateChances.Add(chance);
            totalChance += chance;
        }

        // If none of the requested types exist, fall back to any valid variant (prefer same family)
        if (candidates.Count == 0)
        {
            EnemyData fallback = FindAnyValidVariantForFamily(family);
            if (fallback != null)
            {
                return fallback;
            }

            // Last resort: first available non-null
            for (int i = 0; i < enemyTypeVariants.Count; i++)
            {
                if (enemyTypeVariants[i] != null)
                {
                    return enemyTypeVariants[i];
                }
            }

            return null;
        }

        // Weighted pick
        float pick = Random.value * totalChance;
        float cumulative = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            cumulative += candidateChances[i];
            if (pick <= cumulative)
            {
                return candidates[i];
            }
        }

        return candidates[candidates.Count - 1];
    }

    /// <summary>
    /// Gets the type distribution and probabilities based on difficulty
    /// </summary>
    private (EnemyType[], float[]) GetDifficultyDistribution(EnemyPrefabFamily family)
    {
        switch (difficultyPreset)
        {
            case SpawnDifficulty.Easy:
                return GetFamilyDistribution_Easy(family);

            case SpawnDifficulty.Normal:
                return GetFamilyDistribution_Normal(family);

            case SpawnDifficulty.Hard:
                return GetFamilyDistribution_Hard(family);

            default:
                return (
                    new[] { EnemyType.FastSpider },
                    new[] { 1f }
                );
        }
    }

    private EnemyPrefabFamily ResolvePrefabFamily()
    {
        if (prefabFamily != EnemyPrefabFamily.Auto)
        {
            return prefabFamily;
        }

        if (enemyPrefab == null)
        {
            return EnemyPrefabFamily.Spider;
        }

        string name = enemyPrefab.name;
        if (string.IsNullOrEmpty(name))
        {
            return EnemyPrefabFamily.Spider;
        }

        string lower = name.ToLowerInvariant();
        if (lower.Contains("chicken")) return EnemyPrefabFamily.Chicken;
        if (lower.Contains("ghost")) return EnemyPrefabFamily.Ghost;
        if (lower.Contains("spider")) return EnemyPrefabFamily.Spider;

        // Default to spider to preserve legacy behavior/tests
        return EnemyPrefabFamily.Spider;
    }

    private (EnemyType[], float[]) GetFamilyDistribution_Easy(EnemyPrefabFamily family)
    {
        switch (family)
        {
            case EnemyPrefabFamily.Chicken:
                return (
                    new[] { EnemyType.FastChicken, EnemyType.TankChicken },
                    new[] { 0.7f, 0.3f }
                );
            case EnemyPrefabFamily.Ghost:
                return (
                    new[] { EnemyType.WispGhost, EnemyType.PhantomGhost },
                    new[] { 0.7f, 0.3f }
                );
            case EnemyPrefabFamily.Spider:
            default:
                return (
                    new[] { EnemyType.FastSpider, EnemyType.TankSpider },
                    new[] { 0.7f, 0.3f }
                );
        }
    }

    private (EnemyType[], float[]) GetFamilyDistribution_Normal(EnemyPrefabFamily family)
    {
        switch (family)
        {
            case EnemyPrefabFamily.Chicken:
                return (
                    new[] { EnemyType.FastChicken, EnemyType.TankChicken, EnemyType.RabidChicken },
                    new[] { 0.5f, 0.3f, 0.2f }
                );
            case EnemyPrefabFamily.Ghost:
                return (
                    new[] { EnemyType.WispGhost, EnemyType.PhantomGhost, EnemyType.PoltergeistGhost },
                    new[] { 0.5f, 0.3f, 0.2f }
                );
            case EnemyPrefabFamily.Spider:
            default:
                return (
                    new[] { EnemyType.FastSpider, EnemyType.TankSpider, EnemyType.VenomSpider },
                    new[] { 0.5f, 0.3f, 0.2f }
                );
        }
    }

    private (EnemyType[], float[]) GetFamilyDistribution_Hard(EnemyPrefabFamily family)
    {
        switch (family)
        {
            case EnemyPrefabFamily.Chicken:
                return (
                    new[] { EnemyType.FastChicken, EnemyType.TankChicken, EnemyType.RabidChicken, EnemyType.GiantChicken },
                    new[] { 0.3f, 0.3f, 0.25f, 0.15f }
                );
            case EnemyPrefabFamily.Ghost:
                return (
                    new[] { EnemyType.WispGhost, EnemyType.PhantomGhost, EnemyType.PoltergeistGhost, EnemyType.ReaperGhost },
                    new[] { 0.3f, 0.3f, 0.25f, 0.15f }
                );
            case EnemyPrefabFamily.Spider:
            default:
                return (
                    new[] { EnemyType.FastSpider, EnemyType.TankSpider, EnemyType.VenomSpider, EnemyType.GoliathSpider },
                    new[] { 0.3f, 0.3f, 0.25f, 0.15f }
                );
        }
    }

    private EnemyData FindAnyValidVariantForFamily(EnemyPrefabFamily family)
    {
        // Prefer variants that match the family, but fall back if none exist.
        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < enemyTypeVariants.Count; i++)
            {
                EnemyData data = enemyTypeVariants[i];
                if (data == null || !data.IsValid())
                {
                    continue;
                }

                if (pass == 1)
                {
                    return data;
                }

                if (IsTypeInFamily(data.Type, family))
                {
                    return data;
                }
            }
        }

        return null;
    }

    private bool IsTypeInFamily(EnemyType type, EnemyPrefabFamily family)
    {
        switch (family)
        {
            case EnemyPrefabFamily.Chicken:
                return type == EnemyType.FastChicken || type == EnemyType.TankChicken || type == EnemyType.RabidChicken || type == EnemyType.GiantChicken;
            case EnemyPrefabFamily.Ghost:
                return type == EnemyType.WispGhost || type == EnemyType.PhantomGhost || type == EnemyType.PoltergeistGhost || type == EnemyType.ReaperGhost;
            case EnemyPrefabFamily.Spider:
            default:
                return type == EnemyType.FastSpider || type == EnemyType.TankSpider || type == EnemyType.VenomSpider || type == EnemyType.GoliathSpider;
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
        if (enemy != null)
        {
            Debug.Log($"Enemy '{enemy.gameObject.name}' reached end of path. Despawning.");
            Destroy(enemy.gameObject, 0.1f); // Small delay to allow event processing to complete
        }
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