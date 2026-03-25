using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BuildingSpawner : MonoBehaviour
{
    [Header("Building Prefabs")]
    public GameObject[] buildingPrefabs;

    [Header("Spawn Settings")]
    public float spawnDistance = 60f;
    public float despawnDistance = 30f;
    public float spawnInterval = 12f;

    [Header("Placement Settings")]
    public float leftLaneX = -10f;
    public float rightLaneX = 10f;
    public float groundY = 0f;
    public bool spawnBothSides = true;

    [Header("Random Scale")]
    public float minScale = 0.8f;
    public float maxScale = 1.4f;

    [Header("Rise Animation")]
    public float riseStartY = -15f;       // How far underground buildings start
    public float riseDuration = 1.2f;     // How long the rise takes (seconds)
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Transform player;
    private float nextSpawnZ;
    private List<GameObject> spawnedBuildings = new List<GameObject>();

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogWarning("BuildingSpawner: No GameObject tagged 'Player' found!");

        nextSpawnZ = (player != null ? player.position.z : 0f) + 10f;

        // Pre-spawn some buildings ahead
        for (float z = nextSpawnZ; z < nextSpawnZ + spawnDistance; z += spawnInterval)
        {
            SpawnBuildingAt(z);
            nextSpawnZ = z + spawnInterval;
        }
    }

    void Update()
    {
        if (player == null) return;

        // Spawn new buildings ahead
        while (nextSpawnZ < player.position.z + spawnDistance)
        {
            SpawnBuildingAt(nextSpawnZ);
            nextSpawnZ += spawnInterval;
        }

        // Despawn buildings far behind
        for (int i = spawnedBuildings.Count - 1; i >= 0; i--)
        {
            if (spawnedBuildings[i] == null)
            {
                spawnedBuildings.RemoveAt(i);
                continue;
            }
            if (spawnedBuildings[i].transform.position.z < player.position.z - despawnDistance)
            {
                Destroy(spawnedBuildings[i]);
                spawnedBuildings.RemoveAt(i);
            }
        }
    }

    void SpawnBuildingAt(float zPos)
    {
        if (buildingPrefabs == null || buildingPrefabs.Length == 0) return;

        // Right side
        SpawnSingle(buildingPrefabs[Random.Range(0, buildingPrefabs.Length)], rightLaneX, zPos);

        // Left side
        if (spawnBothSides)
            SpawnSingle(buildingPrefabs[Random.Range(0, buildingPrefabs.Length)], leftLaneX, zPos);
    }

    void SpawnSingle(GameObject prefab, float xPos, float zPos)
    {
        // Start underground
        Vector3 startPos = new Vector3(xPos, riseStartY, zPos);
        Vector3 finalPos = new Vector3(xPos, groundY, zPos);

        GameObject building = Instantiate(prefab, startPos, Quaternion.identity);

        float scale = Random.Range(minScale, maxScale);
        building.transform.localScale = Vector3.one * scale;
        building.tag = "Building";

        building.transform.rotation = Quaternion.identity;

        spawnedBuildings.Add(building);

        // Start the rise animation
        StartCoroutine(RiseUp(building, startPos, finalPos));
    }

    IEnumerator RiseUp(GameObject building, Vector3 from, Vector3 to)
    {
        float elapsed = 0f;

        while (elapsed < riseDuration)
        {
            if (building == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / riseDuration);
            float curvedT = riseCurve.Evaluate(t);

            building.transform.position = Vector3.Lerp(from, to, curvedT);
            yield return null;
        }

        if (building != null)
            building.transform.position = to;
    }
}