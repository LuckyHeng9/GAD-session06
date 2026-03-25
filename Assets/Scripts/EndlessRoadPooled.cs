using UnityEngine;
using System.Collections.Generic;

public class __EndlessRoadPooled__ : MonoBehaviour
{
    [Header("Chunks")]
    public GameObject roadChunkPrefab;
    public float chunkLength = 100f;
    public int chunksAhead = 3;

    [Header("Spawn Height")]
    public float spawnY = 0.05f;
    public float spawnXOffset = 0f;

    [Header("Ground")]
    public GameObject groundChunkPrefab;
    public float groundWidth = 100f;

    [Header("Buildings")]
    public GameObject[] buildingPrefabs;
    public float buildingLookahead = 1000f;
    public int maxBuildingsPerSide = 3;
    public float buildingSpawnDistanceFromPlayer = 1000f;
    public float buildingRiseDistance = 550f;
    public float buildingRiseDuration = 0.8f;
    public float roadHalfWidth = 6f;
    public int buildingPoolSize = 15;
    public float extraBuryDepth = 25f; 

    [Header("Building Grid")]
    public int gridColumns = 3;
    public int gridRows = 5;
    public float columnSpacing = 8f;
    public float rowSpacing = 18f;
    [Range(0f, 1f)]
    public float fillChance = 0.5f;

    [Header("Building Collision")]
    public bool gameOverOnHit = false;
    public float bounceForce = 5f;

    [Header("References")]
    public Transform player;

    // ── Road / ground pools ──────────────────────────────────────
    private Queue<GameObject> pool = new Queue<GameObject>();
    private List<GameObject> activeChunks = new List<GameObject>();
    private Queue<GameObject> groundPool = new Queue<GameObject>();
    private List<GameObject> activeGroundChunks = new List<GameObject>();

    // ── Building pools ───────────────────────────────────────────
    private List<Queue<GameObject>> buildingPools = new List<Queue<GameObject>>();
    private List<BuildingInstance> activeBuildings = new List<BuildingInstance>();
    private float[] prefabHeights;

    // ── Spawn cursors ────────────────────────────────────────────
    private float spawnZ = 0f;
    private float buildingSpawnZ = 0f;

    private Rigidbody playerRb;

    private struct BuildingInstance
    {
        public GameObject go;
        public int prefabIndex;
        public bool hasRisen;
        public float buryDepth;   // prefab height only
        public float totalDepth;  // prefab height + extraBuryDepth at spawn time
    }

    // ─────────────────────────────────────────────────────────────
    void Start()
    {
        if (player == null)
            player = GameObject.FindWithTag("Player").transform;

        playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
            playerRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        spawnZ = player.position.z;
        buildingSpawnZ = player.position.z + buildingSpawnDistanceFromPlayer;

        // Pre-warm road pool
        for (int i = 0; i < chunksAhead + 1; i++)
        {
            var obj = Instantiate(roadChunkPrefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }

        // Pre-warm ground pool
        for (int i = 0; i < chunksAhead + 1; i++)
        {
            var obj = Instantiate(groundChunkPrefab);
            obj.SetActive(false);
            groundPool.Enqueue(obj);
        }

        // Cache prefab heights + pre-warm building pools
        if (buildingPrefabs != null && buildingPrefabs.Length > 0)
        {
            prefabHeights = new float[buildingPrefabs.Length];
            for (int p = 0; p < buildingPrefabs.Length; p++)
            {
                prefabHeights[p] = MeasurePrefabHeight(buildingPrefabs[p]);
                var q = new Queue<GameObject>();
                int perPrefab = Mathf.Max(2, buildingPoolSize / buildingPrefabs.Length);
                for (int i = 0; i < perPrefab; i++)
                {
                    var obj = Instantiate(buildingPrefabs[p]);
                    obj.SetActive(false);
                    q.Enqueue(obj);
                }
                buildingPools.Add(q);
            }
        }

        // Spawn initial road chunks
        for (int i = 0; i < chunksAhead; i++)
            SpawnChunk();

        // Pre-place buildings
        float buildingSpawnEndZ = player.position.z + buildingSpawnDistanceFromPlayer + buildingLookahead;
        while (buildingSpawnZ < buildingSpawnEndZ)
        {
            SpawnBuildingsForChunk(buildingSpawnZ);
            buildingSpawnZ += chunkLength;
        }
    }

    // ─────────────────────────────────────────────────────────────
    void Update()
    {
        if (player.position.z + (chunkLength * (chunksAhead - 1)) > spawnZ)
            SpawnChunk();

        float buildingSpawnEndZ = player.position.z + buildingSpawnDistanceFromPlayer + buildingLookahead;
        while (buildingSpawnZ < buildingSpawnEndZ)
        {
            SpawnBuildingsForChunk(buildingSpawnZ);
            buildingSpawnZ += chunkLength;
        }

        TickBuildingRise();
        RecycleOldChunks();
    }

    // ─────────────────────────────────────────────────────────────
    void SpawnChunk()
    {
        GameObject chunk;
        if (pool.Count > 0)
        {
            chunk = pool.Dequeue();
            chunk.transform.position = new Vector3(spawnXOffset, 0.1f, spawnZ);
            chunk.SetActive(true);
        }
        else
        {
            chunk = Instantiate(roadChunkPrefab,
                new Vector3(spawnXOffset, spawnY, spawnZ), Quaternion.identity);
        }
        activeChunks.Add(chunk);

        GameObject groundChunk;
        if (groundPool.Count > 0)
        {
            groundChunk = groundPool.Dequeue();
            groundChunk.transform.position = new Vector3(0, 0, spawnZ);
            groundChunk.SetActive(true);
        }
        else
        {
            groundChunk = Instantiate(groundChunkPrefab,
                new Vector3(0, 0, spawnZ), Quaternion.identity);
        }
        activeGroundChunks.Add(groundChunk);

        spawnZ += chunkLength;
    }

    // ─────────────────────────────────────────────────────────────
    void SpawnBuildingsForChunk(float chunkStartZ)
    {
        if (buildingPrefabs == null || buildingPrefabs.Length == 0) return;

        int maxSpawnPerSide = Mathf.Clamp(maxBuildingsPerSide, 1, 5);

        foreach (float side in new float[] { -1f, 1f })
        {
            int spawnedThisSide = 0;

            for (int row = 0; row < gridRows; row++)
            {
                for (int col = 0; col < gridColumns; col++)
                {
                    if (spawnedThisSide >= maxSpawnPerSide) break;
                    if (Random.value > fillChance) continue;

                    float xPos = spawnXOffset + side * (roadHalfWidth + (col + 1) * columnSpacing);
                    float zPos = chunkStartZ + (row + 0.5f) * (chunkLength / gridRows);

                    int   prefabIdx = Random.Range(0, buildingPrefabs.Length);
                    float height    = prefabHeights[prefabIdx];
                    float yRot      = (side > 0 ? 180f : 0f) + Random.Range(-10f, 10f);

                    // Spawn below ground by full height + extraBuryDepth so it's
                    // completely hidden before the rise animation begins
                    Vector3 startPos = new Vector3(xPos, -height - extraBuryDepth, zPos);

                    GameObject go;
                    var q = buildingPools[prefabIdx];
                    if (q.Count > 0)
                    {
                        go = q.Dequeue();
                        go.transform.SetPositionAndRotation(startPos, Quaternion.Euler(0, yRot, 0));
                        go.SetActive(true);
                    }
                    else
                    {
                        go = Instantiate(buildingPrefabs[prefabIdx], startPos,
                            Quaternion.Euler(0, yRot, 0));
                    }

                    EnsureCollider(go);
                    EnsureBuildingTag(go);

                    activeBuildings.Add(new BuildingInstance
                    {
                        go          = go,
                        prefabIndex = prefabIdx,
                        hasRisen    = false,
                        buryDepth   = height,
                        totalDepth  = height + extraBuryDepth
                    });

                    spawnedThisSide++;
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    void EnsureCollider(GameObject go)
    {
        if (go == null) return;

        foreach (var rb in go.GetComponentsInChildren<Rigidbody>())
            Destroy(rb);

        var existing = go.GetComponentsInChildren<Collider>(true);
        if (existing.Length > 0)
        {
            foreach (var col in existing)
            {
                col.enabled   = true;
                col.isTrigger = false;
            }
            return;
        }

        foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh   = mf.sharedMesh;
            mc.convex       = true;
            mc.isTrigger    = false;
        }
    }

    // ─────────────────────────────────────────────────────────────
    void EnsureBuildingTag(GameObject go)
    {
        go.tag = "Building";
        foreach (Transform child in go.GetComponentsInChildren<Transform>())
            child.gameObject.tag = "Building";
    }

    // ─────────────────────────────────────────────────────────────
    public void OnPlayerHitBuilding(Collision collision)
    {
        if (playerRb == null) return;

        if (gameOverOnHit)
        {
            Debug.Log("Game Over!");
            // GameManager.Instance.GameOver();
            return;
        }

        playerRb.velocity = Vector3.zero;
        Vector3 bounceDir = (player.position - collision.contacts[0].point).normalized;
        bounceDir.y = 0f;
        playerRb.AddForce(bounceDir * bounceForce, ForceMode.Impulse);
    }

    // ─────────────────────────────────────────────────────────────
    void TickBuildingRise()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < activeBuildings.Count; i++)
        {
            var b = activeBuildings[i];
            if (b.hasRisen || b.go == null) continue;

            float distAhead = b.go.transform.position.z - player.position.z;
            if (distAhead > buildingRiseDistance) continue;

            Vector3 pos = b.go.transform.position;

            // Use totalDepth (height + extraBuryDepth) so rise speed covers the
            // full underground distance within buildingRiseDuration seconds
            float newY = Mathf.MoveTowards(pos.y, 0f,
                             (b.totalDepth / buildingRiseDuration) * dt);

            b.go.transform.position = new Vector3(pos.x, newY, pos.z);

            if (Mathf.Approximately(newY, 0f))
                b.hasRisen = true;

            activeBuildings[i] = b;
        }
    }

    // ─────────────────────────────────────────────────────────────
    void RecycleOldChunks()
    {
        for (int i = activeChunks.Count - 1; i >= 0; i--)
        {
            if (activeChunks[i].transform.position.z + chunkLength < player.position.z)
            {
                activeChunks[i].SetActive(false);
                pool.Enqueue(activeChunks[i]);
                activeChunks.RemoveAt(i);
            }
        }

        for (int i = activeGroundChunks.Count - 1; i >= 0; i--)
        {
            if (activeGroundChunks[i].transform.position.z + chunkLength < player.position.z)
            {
                activeGroundChunks[i].SetActive(false);
                groundPool.Enqueue(activeGroundChunks[i]);
                activeGroundChunks.RemoveAt(i);
            }
        }

        for (int i = activeBuildings.Count - 1; i >= 0; i--)
        {
            var b = activeBuildings[i];
            if (b.go == null) { activeBuildings.RemoveAt(i); continue; }

            if (b.go.transform.position.z + chunkLength < player.position.z)
            {
                b.go.SetActive(false);
                buildingPools[b.prefabIndex].Enqueue(b.go);
                activeBuildings.RemoveAt(i);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    float MeasurePrefabHeight(GameObject prefab)
    {
        var temp      = Instantiate(prefab);
        var renderers = temp.GetComponentsInChildren<Renderer>();
        float height  = 10f;
        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            height = b.size.y;
        }
        Destroy(temp);
        return height;
    }
}