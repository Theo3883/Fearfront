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
        enemy.Initialize(waypoints, this);
    }

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

    public void SetEnemiesToSpawn(int count)
    {
        enemiesToSpawn = count;
    }

    public void SetSpawnInterval(float interval)
    {
        spawnInterval = Mathf.Max(0.1f, interval);
    }

    public void SetInfiniteWaves(bool infinite)
    {
        infiniteWaves = infinite;
    }

    public void SetWaveTimeThreshold(float threshold)
    {
        waveTimeThreshold = Mathf.Max(0.1f, threshold);
    }

    public int GetWaveCount()
    {
        return waveCount;
    }
}
