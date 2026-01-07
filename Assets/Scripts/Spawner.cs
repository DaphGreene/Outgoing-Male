using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject prefab;

    [Header("Spawn Timing (seconds)")]
    [SerializeField] private float minSpawnRate = 1f;
    [SerializeField] private float maxSpawnRate = 2f;

    [Header("Spawn Position")]
    [SerializeField] private float minHeight = -2f;
    [SerializeField] private float maxHeight = 5f;

    [Header("Gameplay Gate")]
    [SerializeField] private GameManager gameManager;

    [Header("Debug")]
    [SerializeField] private bool debugSpawnLogs = false;

    private const float NotPlayingCheckInterval = 0.25f;

    private void OnEnable()
    {
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
            CancelInvoke(nameof(Tick));
            Invoke(nameof(Tick), NotPlayingCheckInterval);
            return;
        }

        // Spawn exactly one obstacle
        GameObject obstacleRoot = Instantiate(prefab, transform.position, Quaternion.identity);

        var obstacle = obstacleRoot.GetComponent<Obstacles>();
        if (obstacle != null)
        {
            obstacle.SetGameManager(gameManager);
        }

        float yOffset = Random.Range(minHeight, maxHeight);
        obstacleRoot.transform.position += Vector3.up * yOffset;

        if (debugSpawnLogs)
            Debug.Log($"Spawner: Spawned '{obstacleRoot.name}' at {obstacleRoot.transform.position}", this);

        // Schedule the next spawn
        float delay = Random.Range(minSpawnRate, maxSpawnRate);
        CancelInvoke(nameof(Tick));
        Invoke(nameof(Tick), delay);
    }

    private void OnValidate()
    {
        if (maxSpawnRate < minSpawnRate) maxSpawnRate = minSpawnRate;
        if (maxHeight < minHeight) maxHeight = minHeight;
    }
}
