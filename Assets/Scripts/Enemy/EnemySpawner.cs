using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using Sydewa;

public class EnemySpawner : MonoBehaviour
{


    [Header("Specific Prefabs")]
    [SerializeField] private GameObject spiderPrefab;
    [SerializeField] private GameObject ghostPrefab;
    [SerializeField] private GameObject chickenPrefab;

    [SerializeField] private EnemyRoute[] availableRoutes;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private Transform spawnPoint;
    
    [Header("Enemy Variants (Manual Assignment)")]
    [SerializeField] private EnemyData variantNormal;
    [SerializeField] private EnemyData variantFast;
    [SerializeField] private EnemyData variantTank;
    [SerializeField] private EnemyData variantRanged;
    [SerializeField] private EnemyData variantHeavy;
    [SerializeField] private EnemyData variantBoss;
    [SerializeField] private SpawnDifficulty difficultyPreset = SpawnDifficulty.Normal;

    [SerializeField] private EnemyFamily prefabFamily = EnemyFamily.Spider;


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

        
        HookIntoLightingManager();
    }

    private void HookIntoLightingManager()
    {
        
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

    private EnemyData GetVariantData(EnemyVariantType type)
    {
        switch (type)
        {
            case EnemyVariantType.Normal: return variantNormal;
            case EnemyVariantType.Fast: return variantFast;
            case EnemyVariantType.Tank: return variantTank;
            case EnemyVariantType.Ranged: return variantRanged;
            case EnemyVariantType.Heavy: return variantHeavy;
            case EnemyVariantType.Boss: return variantBoss;
            default: return variantNormal; // Fallback
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
            if (selectedEnemyData.Family == EnemyFamily.Spider)
            {
                if (spiderPrefab != null) prefabToUse = spiderPrefab;
            }
            else if (selectedEnemyData.Family == EnemyFamily.Ghost)
            {
                if (ghostPrefab != null) prefabToUse = ghostPrefab;
            }
            else if (selectedEnemyData.Family == EnemyFamily.Chicken)
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
    private bool bossSpawnedThisNight = false;
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

        // Reset Boss flag for the new night
        bossSpawnedThisNight = false;

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
            
            // Progressive Difficulty: Spawn faster each night
            if (isNightWaveActive && currentNight > 1)
            {
                // Decrease interval by 0.5s per night, clamped to 0.5s minimum
                float reduction = (currentNight - 1) * 0.5f;
                currentInterval = Mathf.Max(0.5f, spawnInterval - reduction);
            }

            SpawnEnemy();
            
            yield return new WaitForSeconds(currentInterval);
        }
    }

    /// <summary>
    /// Gets a random enemy type based on specific night/day logic
    /// </summary>
    /// <summary>
    /// Gets a random enemy type based on specific night/day logic
    /// </summary>
    private EnemyData GetRandomEnemyType()
    {
        // Safety check: ensure at least Normal variant is assigned
        if (variantNormal == null) 
        {
            Debug.LogError("EnemySpawner: variantNormal is not assigned in the Inspector!");
            return null;
        }

        EnemyFamily targetedFamily = ResolvePrefabFamily();

        if (isDayEventActive)
        {
            targetedFamily = EnemyFamily.Chicken;
        }
        else if (isNightWaveActive)
        {
            // Night Logic
            if (currentNight == 1)
            {
                // Night 1: Spiders Only
                targetedFamily = EnemyFamily.Spider;
            }
            else if (currentNight == 2)
            {
                // Night 2: Ghosts Only
                targetedFamily = EnemyFamily.Ghost;
            }
            else
            {
                // Night 3+: Mixed (Spiders + Ghosts)
                targetedFamily = Random.value > 0.5f ? EnemyFamily.Spider : EnemyFamily.Ghost;
            }
        }

        // Logic for Variants (Normal, Fast, Boss, etc)
        EnemyVariantType[] types;
        float[] chances;

        bool forceBoss = false;
        // Check for Boss Spawn (Night 2+, once per night)
        if (isNightWaveActive && currentNight >= 2 && !bossSpawnedThisNight)
        {
            forceBoss = true;
            bossSpawnedThisNight = true; 
        }

        if (forceBoss)
        {
            types = new[] { EnemyVariantType.Boss };
            chances = new[] { 1.0f };
        }
        else
        {
             var distribution = GetDifficultyDistribution();
             types = distribution.Item1;
             chances = distribution.Item2;
        }
        
        List<EnemyData> candidates = new List<EnemyData>();
        List<float> candidateChances = new List<float>();
        float totalChance = 0f;

        for (int i = 0; i < types.Length; i++)
        {
            EnemyData data = FindEnemyData(targetedFamily, types[i]);
            
            // Fallback for Boss if specific family boss isn't found
            if (forceBoss && data == null)
            {
                data = variantBoss; // Just use the assigned Boss slot
            }

            if (data == null) continue;

            float chance = Mathf.Max(0f, chances[i]);
            if (chance <= 0f) continue;

            candidates.Add(data);
            candidateChances.Add(chance);
            totalChance += chance;
        }

        if (candidates.Count == 0) return variantNormal;

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
    /// Returns Generic Variants (Normal, Fast, Tank) which apply to ANY Family
    /// </summary>
    private (EnemyVariantType[], float[]) GetDifficultyDistribution()
    {   
        switch (difficultyPreset)
        {
            case SpawnDifficulty.Easy:
                return (
                    new[] { EnemyVariantType.Normal, EnemyVariantType.Tank },
                    new[] { 0.7f, 0.3f }
                );
            case SpawnDifficulty.Normal:
                return (
                    new[] { EnemyVariantType.Normal, EnemyVariantType.Fast, EnemyVariantType.Tank, EnemyVariantType.Ranged },
                    new[] { 0.4f, 0.3f, 0.2f, 0.1f }
                );
            case SpawnDifficulty.Hard:
            default:
                return (
                    new[] { EnemyVariantType.Normal, EnemyVariantType.Fast, EnemyVariantType.Tank, EnemyVariantType.Ranged, EnemyVariantType.Heavy },
                    new[] { 0.2f, 0.3f, 0.2f, 0.2f, 0.1f }
                );
        }
    }

    private EnemyFamily ResolvePrefabFamily()
    {
        return prefabFamily;
    }

    private EnemyData FindAnyValidVariantForFamily(EnemyFamily family)
    {
         return variantNormal;
    }

    /// <summary>
    /// Finds EnemyData by Family AND Variant
    /// </summary>
    /// <summary>
    /// Finds EnemyData by Family AND Variant
    /// </summary>
    private EnemyData FindEnemyData(EnemyFamily family, EnemyVariantType variant)
    {
        return GetVariantData(variant);
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
        if (PlayerHealth.Instance != null)
            return PlayerHealth.Instance;

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