using System.Collections;
using System.Linq;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    public GameObject[] enemyPrefabs;

    [Header("Spawn Settings")]
    public float spawnDistance = 120f;
    public float spawnIntervalMin = 2f;
    public float spawnIntervalMax = 4f;
    public float minSpawnInterval = 1.2f;
    public float spawnRampRate = 0.005f;
    public int maxEnemies = 3;              // max alive at once

    [Header("Lane Positions")]
    public float[] lanePositions = { -3.5f, 0f, 3.5f };

    [Header("References")]
    public Transform player;
    public Camera playerCamera;

    private float gameTime;
    private bool isSpawning = false;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
            else Debug.LogWarning("EnemySpawner: No Player found.");
        }

        if (playerCamera == null)
            playerCamera = Camera.main;

        StartSpawning();
    }

    void Update()
    {
        if (isSpawning)
            gameTime += Time.deltaTime;
    }

    public void StartSpawning()
    {
        if (!isSpawning)
        {
            isSpawning = true;
            StartCoroutine(SpawnRoutine());
        }
    }

    public void StopSpawning() => isSpawning = false;

    public void ResetSpawner()
    {
        StopSpawning();
        gameTime = 0f;
    }

    IEnumerator SpawnRoutine()
    {
        while (isSpawning)
        {
            float rampFactor = Mathf.Min(spawnRampRate * gameTime, spawnIntervalMax - minSpawnInterval);
            float interval = Mathf.Max(
                minSpawnInterval,
                Random.Range(spawnIntervalMin, spawnIntervalMax) - rampFactor
            );

            yield return new WaitForSeconds(interval);

            if (isSpawning)
                SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;
        if (player == null) return;

        // Count current alive enemies — don't spawn if at limit
        int aliveCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
        if (aliveCount >= maxEnemies) return;

        var validPrefabs = enemyPrefabs.Where(p => p != null).ToArray();
        if (validPrefabs.Length == 0)
        {
            Debug.LogWarning("EnemySpawner: All enemy prefabs are NULL!");
            return;
        }

        GameObject prefab = validPrefabs[Random.Range(0, validPrefabs.Length)];
        float laneX = lanePositions[Random.Range(0, lanePositions.Length)];
        Vector3 spawnPos = new Vector3(laneX, 0f, player.position.z + spawnDistance);

        // Push spawn point out of camera view if needed
        if (playerCamera != null)
        {
            Vector3 viewPos = playerCamera.WorldToViewportPoint(spawnPos);
            int safety = 0;
            while (viewPos.z > 0 && viewPos.x > -0.1f && viewPos.x < 1.1f
                   && viewPos.y > -0.1f && viewPos.y < 1.1f && safety < 10)
            {
                spawnPos.z += 10f;
                viewPos = playerCamera.WorldToViewportPoint(spawnPos);
                safety++;
            }
        }

        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        enemy.tag = "Enemy";

        Enemy enemyScript = enemy.GetComponent<Enemy>();
        if (enemyScript != null)
            enemyScript.player = player;
    }
}