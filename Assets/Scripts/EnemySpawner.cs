using UnityEngine;
using System.Collections;

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

    // --- NEW VARIABLES START ---
    [Header("Randomization Settings")]
    [Tooltip("Minimum size of the spider")]
    [SerializeField] private float minScale = 0.7f; 
    
    [Tooltip("Maximum size of the spider")]
    [SerializeField] private float maxScale = 1.3f;

    [Tooltip("The spider will be a random blend between Color A and Color E")]
    [SerializeField] private Color colorVariantA = Color.white; 
    [SerializeField] private Color colorVariantB = Color.gray;
    [SerializeField] private Color colorVariantC = Color.black;
    [SerializeField] private Color colorVariantD = Color.red;
    [SerializeField] private Color colorVariantE = Color.green;
    // --- NEW VARIABLES END ---
    
    [Header("Life Settings")]
    [Tooltip("How many seconds until the spider dies automatically?")]
    [SerializeField] private float enemyLifeTime = 5f; // <--- ADD THIS VARIABLE

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

    private void SpawnEnemy()
    {
        GameObject newEnemyObject = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);

        // --- NEW LOGIC START ---
        ApplyRandomization(newEnemyObject);
        // --- NEW LOGIC END ---
        
        Enemy enemy = newEnemyObject.GetComponent<Enemy>();
        if (enemy == null)
        {
            Debug.LogError("Enemy prefab doesn't have an Enemy component!");
            Destroy(newEnemyObject);
            return;
        }
        
        // --- NEW LINE HERE ---
        // Tell the enemy to self-destruct after X seconds
        enemy.ActivateSelfDestruct(enemyLifeTime); 
        // ---------------------

        EnemyRoute randomRoute = GetRandomRoute();
        if (randomRoute == null || !randomRoute.IsValid())
        {
            Debug.LogError("Selected route is invalid!");
            Destroy(newEnemyObject);
            return;
        }

        Transform[] waypoints = randomRoute.GetWaypoints();
        enemy.Initialize(waypoints, this);
    }

    // --- NEW HELPER FUNCTION ---
    private void ApplyRandomization(GameObject spider)
    {
        // 1. Randomize Scale
        float randomScale = Random.Range(minScale, maxScale);
        spider.transform.localScale = Vector3.one * randomScale;

        // 2. Randomize Color (The correct way for 5 specific colors)
        // We pick a random number between 0 and 4 (because there are 4 gaps between 5 colors)
        float randomVal = Random.Range(0f, 4f); 

        Color resultColor;

        if (randomVal < 1f)
            resultColor = Color.Lerp(colorVariantA, colorVariantB, randomVal);       // 0 to 1
        else if (randomVal < 2f)
            resultColor = Color.Lerp(colorVariantB, colorVariantC, randomVal - 1f);  // 1 to 2
        else if (randomVal < 3f)
            resultColor = Color.Lerp(colorVariantC, colorVariantD, randomVal - 2f);  // 2 to 3
        else
            resultColor = Color.Lerp(colorVariantD, colorVariantE, randomVal - 3f);  // 3 to 4

        // Apply to all renderers
        Renderer[] renderers = spider.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.material.color = resultColor;
        }
    }
    // ---------------------------

    private EnemyRoute GetRandomRoute()
    {
        if (availableRoutes.Length == 0)
            return null;

        int randomIndex = Random.Range(0, availableRoutes.Length);
        return availableRoutes[randomIndex];
    }

    public void OnEnemyReachedEnd(Enemy enemy)
    {
    }
    
    // ... (Rest of your setters/getters remain unchanged)
    public void SetEnemiesToSpawn(int count) { enemiesToSpawn = count; }
    public void SetSpawnInterval(float interval) { spawnInterval = Mathf.Max(0.1f, interval); }
    public void SetInfiniteWaves(bool infinite) { infiniteWaves = infinite; }
    public void SetWaveTimeThreshold(float threshold) { waveTimeThreshold = Mathf.Max(0.1f, threshold); }
    public int GetWaveCount() { return waveCount; }
}