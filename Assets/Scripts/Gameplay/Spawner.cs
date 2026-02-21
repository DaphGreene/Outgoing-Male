using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject prefab;

    [Header("Spawn Timing (seconds)")]
    [SerializeField] private float minSpawnRate = 1f;
    [SerializeField] private float maxSpawnRate = 1f;
    [SerializeField] private float firstSpawnDelaySeconds = 3f;
    [SerializeField] private int introObstacleCount = 5;
    [SerializeField] private float introSpacingMultiplier = 2f;

    [Header("Spawn Position")]
    [SerializeField] private float minHeight = -2f;
    [SerializeField] private float maxHeight = 5f;

    [Header("Gameplay Gate")]
    [SerializeField] private GameManager gameManager;

    [Header("Debug")]
    [SerializeField] private bool debugSpawnLogs = false;

    private const float NotPlayingCheckInterval = 0.25f;
    private bool spawnCycleInitialized;
    private int obstaclesSpawnedThisRun;

    private void OnEnable()
    {
        ResetSpawnCycle();
        CancelInvoke();
        Invoke(nameof(Tick), NotPlayingCheckInterval);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void Tick()
    {
        if (prefab == null)
        {
            Debug.LogError("Spawner: Prefab is not assigned.", this);
            return;
        }

        // If not playing, DO NOT schedule fast spawns. Just check again later.
        if (gameManager != null && !gameManager.IsPlaying)
        {
            ResetSpawnCycle();
            CancelInvoke(nameof(Tick));
            Invoke(nameof(Tick), NotPlayingCheckInterval);
            return;
        }

        // First time we enter Playing for this run: wait before the first obstacle.
        if (!spawnCycleInitialized)
        {
            spawnCycleInitialized = true;
            obstaclesSpawnedThisRun = 0;
            CancelInvoke(nameof(Tick));
            Invoke(nameof(Tick), Mathf.Max(0f, firstSpawnDelaySeconds));
            return;
        }

        // Spawn exactly one obstacle
        GameObject obstacleRoot = Instantiate(prefab, transform.position, Quaternion.identity);

        var obstacle = obstacleRoot.GetComponent<Obstacle>();
        if (obstacle != null)
        {
            obstacle.SetGameManager(gameManager);
        }

        float yOffset = Random.Range(minHeight, maxHeight);
        obstacleRoot.transform.position += Vector3.up * yOffset;

        if (debugSpawnLogs)
            Debug.Log($"Spawner: Spawned '{obstacleRoot.name}' at {obstacleRoot.transform.position}", this);

        obstaclesSpawnedThisRun++;

        // Schedule the next spawn
        float delay = Random.Range(minSpawnRate, maxSpawnRate);
        if (obstaclesSpawnedThisRun < introObstacleCount)
            delay *= Mathf.Max(1f, introSpacingMultiplier);

        CancelInvoke(nameof(Tick));
        Invoke(nameof(Tick), delay);
    }

    private void OnValidate()
    {
        if (maxSpawnRate < minSpawnRate) maxSpawnRate = minSpawnRate;
        if (maxHeight < minHeight) maxHeight = minHeight;
        if (firstSpawnDelaySeconds < 0f) firstSpawnDelaySeconds = 0f;
        if (introObstacleCount < 0) introObstacleCount = 0;
        if (introSpacingMultiplier < 1f) introSpacingMultiplier = 1f;
    }

    private void ResetSpawnCycle()
    {
        spawnCycleInitialized = false;
        obstaclesSpawnedThisRun = 0;
    }
}
