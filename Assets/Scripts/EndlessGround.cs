using UnityEngine;
using System.Collections.Generic;

public class EndlessGround : MonoBehaviour
{
    [Header("Ground")]
    public GameObject groundChunkPrefab;   // wide green plane prefab
    public float chunkLength = 100f;       // same as road chunk length
    public int chunksAhead = 3;

    [Header("References")]
    public Transform player;

    private Queue<GameObject> pool = new Queue<GameObject>();
    private List<GameObject> activeChunks = new List<GameObject>();
    private float spawnZ = 0f;

    void Start()
    {
        if (player == null)
            player = GameObject.FindWithTag("Player").transform;

        for (int i = 0; i < chunksAhead + 1; i++)
        {
            GameObject obj = Instantiate(groundChunkPrefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }

        for (int i = 0; i < chunksAhead; i++)
            SpawnChunk();
    }

    void Update()
    {
        if (player.position.z + (chunkLength * (chunksAhead - 1)) > spawnZ)
            SpawnChunk();

        for (int i = activeChunks.Count - 1; i >= 0; i--)
        {
            if (activeChunks[i].transform.position.z + chunkLength < player.position.z)
            {
                activeChunks[i].SetActive(false);
                pool.Enqueue(activeChunks[i]);
                activeChunks.RemoveAt(i);
            }
        }
    }

    void SpawnChunk()
    {
        GameObject chunk = pool.Count > 0 ? pool.Dequeue()
            : Instantiate(groundChunkPrefab);

        chunk.transform.position = new Vector3(0, 0, spawnZ);
        chunk.SetActive(true);
        activeChunks.Add(chunk);
        spawnZ += chunkLength;
    }
}
