using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using Sydewa;

public class EnemySpawner : MonoBehaviour
{
    private enum EnemyPrefabFamily
    {
        Auto,
        Spider,
        Chicken,
        Ghost
    }

    [Header("Specific Prefabs")]
    [SerializeField] private GameObject spiderPrefab;
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private GameObject chickenPrefab;

    [SerializeField] private EnemyRoute[] availableRoutes;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private Transform spawnPoint;
    
    [SerializeField] private List<EnemyData> enemyTypeVariants = new List<EnemyData>();
    [SerializeField] private SpawnDifficulty difficultyPreset = SpawnDifficulty.Normal;
    [SerializeField] private EnemyPrefabFamily prefabFamily = EnemyPrefabFamily.Auto;
    [SerializeField] private bool autoLoadVariantsFromResourcesOnStart = true;

    private void Start()
    {
        if (spiderPrefab == null && ghostPrefab == null && chickenPrefab == null)
        {
            Debug.LogError("No enemy prefabs (Spider/Ghost/Chicken) assigned in Inspector!");
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
        
        HookIntoLightingManager();
    }

    private void HookIntoLightingManager()
    {
        
        // Find ALL instances to handle potential duplicates/ghost objects
        Sydewa.LightingManager[] managers = FindObjectsByType<Sydewa.LightingManager>(FindObjectsSortMode.None);
        
        if (managers == null || managers.Length == 0)
        {
             Debug.LogError("<color=red>[EnemySpawner] FATAL: Could not find ANY object of type Sydewa.LightingManager in the scene!</color>");
             return;
        }

        Sydewa.LightingManager targetManager = null;
        
        foreach (var manager in managers)
        {
            int count = (manager.events != null) ? manager.events.Count : 0;
            
            if (count > 0)
            {
                targetManager = manager;
                break; // Found the good one
            }
        }
        
        if (targetManager == null)
        {
            Debug.LogError("<color=red>[EnemySpawner] Found LightingManager(s), but ALL have 0 events! Please check Inspector configuration.</color>");
            return;
        }

        
        foreach (var evt in targetManager.events)
        {
            
            if (evt.eventName == "Start Night")
            {
                evt.Event.RemoveListener(StartNightWave); 
                evt.Event.AddListener(StartNightWave);
            }
            else if (evt.eventName == "End Night")
            {
                evt.Event.RemoveListener(EndNightWave);
                evt.Event.AddListener(EndNightWave);
            }
            else if (evt.eventName == "Start Day")
            {
                evt.Event.RemoveListener(StartDayEvent);
                evt.Event.AddListener(StartDayEvent);
            }
            else if (evt.eventName == "End Day")
            {
                evt.Event.RemoveListener(EndDayEvent);
                evt.Event.AddListener(EndDayEvent);
            }
        }
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

    /// <summary>
    /// Spawn an enemy - public version for testing and external calls
    /// </summary>
    public void SpawnEnemy()
    {
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
        
        EnemyData selectedEnemyData = GetRandomEnemyType();
        GameObject prefabToUse = spiderPrefab;

        if (selectedEnemyData != null)
        {
            if (IsTypeInFamily(selectedEnemyData.Type, EnemyPrefabFamily.Spider))
            {
                if (spiderPrefab != null) prefabToUse = spiderPrefab;
            }
            else if (IsTypeInFamily(selectedEnemyData.Type, EnemyPrefabFamily.Ghost))
            {
                if (ghostPrefab != null) prefabToUse = ghostPrefab;
            }
            else if (IsTypeInFamily(selectedEnemyData.Type, EnemyPrefabFamily.Chicken))
            {
                if (chickenPrefab != null) prefabToUse = chickenPrefab;
            }
        }
        
        if (prefabToUse == null) 
        {
             Debug.LogError("Attempted to spawn enemy but resolved prefab is NULL! Check Inspector assignments.");
             return;
        }
        
        GameObject newEnemyObject = Instantiate(prefabToUse, spawnPosition, Quaternion.identity);
        
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
        
        if (selectedEnemyData != null)
        {
            enemy.SetEnemyData(selectedEnemyData);
        }
        
        enemy.Initialize(waypoints, this);
        
        if (selectedEnemyData != null)
        {
            InitializeEnemyComponents(newEnemyObject, waypoints, selectedEnemyData);
        }
        
        if (difficultyMultiplier > 1.0f && !isDayEventActive) 
        {
            enemy.ApplyDifficulty(difficultyMultiplier);
        }
        
        var simpleInteractable = newEnemyObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (simpleInteractable == null)
        {
            simpleInteractable = newEnemyObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        }
        simpleInteractable.interactionLayers = (UnityEngine.XR.Interaction.Toolkit.InteractionLayerMask)(-1);
        
        var enemyInteractable = newEnemyObject.GetComponent<EnemyInteractable>();
        if (enemyInteractable == null)
        {
            newEnemyObject.AddComponent<EnemyInteractable>();
        }

        var healthBar = newEnemyObject.GetComponent<EnemyHealthBar>();
        if (healthBar == null)
        {
            newEnemyObject.AddComponent<EnemyHealthBar>();
        }
        
        bool hasNonTriggerCollider = false;
        foreach (var col in newEnemyObject.GetComponentsInChildren<Collider>())
        {
            if (!col.isTrigger)
            {
                hasNonTriggerCollider = true;
                break;
            }
        }
        
        if (!hasNonTriggerCollider)
        {
            var capsule = newEnemyObject.AddComponent<CapsuleCollider>();
            capsule.isTrigger = false;
            capsule.radius = 0.4f;
            capsule.height = 1f;
            capsule.center = new Vector3(0, 0.5f, 0);
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
        PlayerHealth playerHealth = FindPlayerHealth();
        if (playerHealth == null)
        {
            Debug.LogWarning("Could not find player or PlayerHealth component. Enemy movement and detection will work, but state machine may not engage properly without player reference.");
        }

        NavMeshPlayerDetector detector = enemyObject.GetComponent<NavMeshPlayerDetector>();
        if (detector == null)
        {
            detector = enemyObject.AddComponent<NavMeshPlayerDetector>();
        }
        
        if (playerHealth != null)
        {
            detector.SetPlayerReference(playerHealth.transform);
        }

        EnemyStateMachine stateMachine = enemyObject.GetComponent<EnemyStateMachine>();
        if (stateMachine == null)
        {
            stateMachine = enemyObject.AddComponent<EnemyStateMachine>();
        }
        
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



    // Difficulty & Wave Control
    private int currentNight = 1; // Start at Night 1
    private float difficultyMultiplier = 1.0f;
    [SerializeField] private float difficultyScalePerNight = 0.1f; // +10% per night

    // Wave State
    private bool isNightWaveActive = false;
    private bool isDayEventActive = false;
    private Coroutine activeSpawnCoroutine;

    // --- Time-Based Event Hooks (Called by LightingManager) ---

    /// <summary>
    /// Call this at 18:00
    /// </summary>
    public void StartNightWave()
    {
        EndDayEvent(); // Ensure day event is over
        
        isNightWaveActive = true;
        
        // Difficulty handling
        // Night 1 is base difficulty (multiplier 1.0)
        // Night 2+ scales up
        if (currentNight > 1)
        {
            float addedDifficulty = (currentNight - 1) * difficultyScalePerNight;
            difficultyMultiplier = 1.0f + addedDifficulty;
        }
        else 
        {
            difficultyMultiplier = 1.0f;
        }

        Debug.Log($"<color=red>Night {currentNight} Started!</color> Difficulty: x{difficultyMultiplier:F2}");
        
        if (activeSpawnCoroutine != null) StopCoroutine(activeSpawnCoroutine);
        activeSpawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    /// <summary>
    /// Call this at 06:00
    /// </summary>
    public void EndNightWave()
    {
        isNightWaveActive = false;
        Debug.Log($"<color=green>Night {currentNight} Ended!</color>");
        currentNight++;
        
        if (activeSpawnCoroutine != null) StopCoroutine(activeSpawnCoroutine);
        activeSpawnCoroutine = null;
    }

    /// <summary>
    /// Call this at 12:00
    /// </summary>
    public void StartDayEvent()
    {
        if (isNightWaveActive) return;
        isDayEventActive = true;
        Debug.Log("<color=yellow>Day Event Started! (Chickens)</color>");
        
        if (activeSpawnCoroutine != null) StopCoroutine(activeSpawnCoroutine);
        activeSpawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    /// <summary>
    /// Call this at 16:00
    /// </summary>
    public void EndDayEvent()
    {
        isDayEventActive = false;
        Debug.Log("<color=yellow>Day Event Ended!</color>");
        
        if (activeSpawnCoroutine != null) StopCoroutine(activeSpawnCoroutine);
        activeSpawnCoroutine = null;
    }

    private IEnumerator SpawnRoutine()
    {
        while (isNightWaveActive || isDayEventActive)
        {
            float currentInterval = spawnInterval;
            if (isNightWaveActive && currentNight > 1)
            {
                currentInterval = Mathf.Max(0.5f, spawnInterval / (1f + (currentNight * 0.05f)));
            }

            SpawnEnemy();
            
            yield return new WaitForSeconds(currentInterval);
        }
    }

    /// <summary>
    /// Gets a random enemy type based on specific night/day logic
    /// </summary>
    private EnemyData GetRandomEnemyType()
    {
        if (enemyTypeVariants.Count == 0) return null;

        EnemyPrefabFamily targetedFamily = EnemyPrefabFamily.Auto;

        if (isDayEventActive)
        {
            // Day Logic: Chickens
            targetedFamily = EnemyPrefabFamily.Chicken;
        }
        else if (isNightWaveActive)
        {
            // Night Logic
            if (currentNight == 1)
            {
                // Night 1: Spiders Only
                targetedFamily = EnemyPrefabFamily.Spider;
            }
            else if (currentNight == 2)
            {
                // Night 2: Ghosts Only
                targetedFamily = EnemyPrefabFamily.Ghost;
            }
            else
            {
                // Night 3+: Mixed (Spiders + Ghosts)
                // 50/50 chance for family
                targetedFamily = Random.value > 0.5f ? EnemyPrefabFamily.Spider : EnemyPrefabFamily.Ghost;
            }
        }
        else
        {
            // Fallback if called outside events (e.g. manual debug spawn)
            targetedFamily = ResolvePrefabFamily();
        }

        // Get distribution for the chosen family
        (EnemyType[] types, float[] chances) = GetDifficultyDistribution(targetedFamily);

        List<EnemyData> candidates = new List<EnemyData>();
        List<float> candidateChances = new List<float>();
        float totalChance = 0f;

        for (int i = 0; i < types.Length; i++)
        {
            EnemyData data = FindEnemyDataByType(types[i]);
            if (data == null) continue;

            float chance = Mathf.Max(0f, chances[i]);
            if (chance <= 0f) continue;

            candidates.Add(data);
            candidateChances.Add(chance);
            totalChance += chance;
        }

        if (candidates.Count == 0) return FindAnyValidVariantForFamily(targetedFamily);

        float pick = Random.value * totalChance;
        float cumulative = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            cumulative += candidateChances[i];
            if (pick <= cumulative) return candidates[i];
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
            case SpawnDifficulty.Easy: return GetFamilyDistribution_Easy(family);
            case SpawnDifficulty.Normal: return GetFamilyDistribution_Normal(family);
            case SpawnDifficulty.Hard: return GetFamilyDistribution_Hard(family);
            default: return (new[] { EnemyType.FastSpider }, new[] { 1f });
        }
    }

    private EnemyPrefabFamily ResolvePrefabFamily()
    {
        if (prefabFamily != EnemyPrefabFamily.Auto)
        {
            return prefabFamily;
        }

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
            Destroy(enemy.gameObject, 0.1f);
        }
    }

    /// <summary>
    /// Public setters for testing and runtime configuration
    /// </summary>
    public void SetSpawnInterval(float interval) { spawnInterval = Mathf.Max(0.1f, interval); }
    public void SetSpawnPoint(Transform spawnPointTransform) { spawnPoint = spawnPointTransform; }
    
    public void SetSpiderPrefab(GameObject prefab) { spiderPrefab = prefab; }
    public void SetGhostPrefab(GameObject prefab) { ghostPrefab = prefab; }
    public void SetChickenPrefab(GameObject prefab) { chickenPrefab = prefab; }

    /// <summary>
    /// Legacy setter for tests - sets all specific prefabs
    /// </summary>
    public void SetEnemyPrefab(GameObject prefab)
    {
        spiderPrefab = prefab;
        ghostPrefab = prefab;
        chickenPrefab = prefab;
    }
}