using UnityEngine;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    [Header("Chunk Prefabs")]
    public GameObject startingChunkPrefab;
    public List<GameObject> obstacleChunkPrefabs = new List<GameObject>();
    public GameObject normalPlatformPrefab; // Add your normal platform prefab here
    public GameObject finishChunkPrefab;
    public GameObject accelerationChunkPrefab;

    [Header("Generation Settings")]
    [Tooltip("How many obstacle chunks to place between the start and finish.")]
    public int numberOfObstacleChunks = 10;
    [Tooltip("How many normal platforms to place between the obstacle chunks.")]
    public int numberOfNormalPlatforms = 3; // Set how many normal platforms you want

    void Start()
    {
        GenerateLevel();
    }

    void GenerateLevel()
    {
        Transform lastEndPoint = null;

        // 1. Spawn Starting Chunk
        if (startingChunkPrefab != null)
        {
            GameObject startChunkInstance = Instantiate(startingChunkPrefab, transform.position, Quaternion.identity);
            lastEndPoint = startChunkInstance.GetComponent<Chunk>().endPoint;
        }

        // --- Start of Changes ---

        // 2. Create a combined list of obstacle and normal platform chunks
        List<GameObject> intermediateChunks = new List<GameObject>();

        // Add the desired number of random obstacle chunks
        for (int i = 0; i < numberOfObstacleChunks; i++)
        {
            if (obstacleChunkPrefabs.Count > 0)
            {
                intermediateChunks.Add(obstacleChunkPrefabs[Random.Range(0, obstacleChunkPrefabs.Count)]);
            }
        }

        // Add the desired number of normal platform chunks
        if (normalPlatformPrefab != null)
        {
            for (int i = 0; i < numberOfNormalPlatforms; i++)
            {
                intermediateChunks.Add(normalPlatformPrefab);
            }
        }

        // Shuffle the list to randomize the order of all intermediate chunks
        for (int i = 0; i < intermediateChunks.Count; i++)
        {
            GameObject temp = intermediateChunks[i];
            int randomIndex = Random.Range(i, intermediateChunks.Count);
            intermediateChunks[i] = intermediateChunks[randomIndex];
            intermediateChunks[randomIndex] = temp;
        }

        // 3. Spawn the shuffled chunks
        foreach (GameObject chunkPrefab in intermediateChunks)
        {
            lastEndPoint = SpawnChunk(chunkPrefab, lastEndPoint);
        }

        // --- End of Changes ---

        // 4. Spawn Finish Chunk
        if (finishChunkPrefab != null)
        {
            lastEndPoint = SpawnChunk(finishChunkPrefab, lastEndPoint);
        }

        // 5. Spawn Acceleration Chunk
        if (accelerationChunkPrefab != null)
        {
            lastEndPoint = SpawnChunk(accelerationChunkPrefab, lastEndPoint);
        }
    }

    Transform SpawnChunk(GameObject chunkPrefab, Transform previousEndPoint)
    {
        // Instantiate the new chunk
        GameObject newChunkInstance = Instantiate(chunkPrefab, Vector3.zero, Quaternion.identity);
        Chunk newChunk = newChunkInstance.GetComponent<Chunk>();

        // Align the new chunk's startPoint with the previous chunk's endPoint
        Vector3 spawnPosition = previousEndPoint.position - newChunk.startPoint.position;
        newChunkInstance.transform.position = spawnPosition;

        // Return the end point of this new chunk for the next iteration
        return newChunk.endPoint;
    }
}