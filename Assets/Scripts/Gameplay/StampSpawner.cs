using UnityEngine;

public class StampSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject stampPickupPrefab;
    [SerializeField] private StampCatalog stampCatalog;
    [SerializeField] private GameManager gameManager;

    [Header("Spawn Timing (seconds)")]
    [SerializeField] private float minSpawnRate = 2.5f;
    [SerializeField] private float maxSpawnRate = 4f;
    [SerializeField] private float firstSpawnDelaySeconds = 4f;
    [SerializeField, Range(0f, 1f)] private float spawnChance = 0.5f;

    [Header("Spawn Position")]
    [SerializeField] private float fallbackMinHeight = -2f;
    [SerializeField] private float fallbackMaxHeight = 3f;

    [Header("Spawn Limits")]
    [SerializeField, Min(1)] private int maxConcurrentStamps = 1;

    [Header("Debug")]
    [SerializeField] private bool debugSpawnLogs = false;

    private const float NotPlayingCheckInterval = 0.25f;
    private bool spawnCycleInitialized;

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
        if (stampPickupPrefab == null)
        {
            Debug.LogError("StampSpawner: Stamp pickup prefab is not assigned.", this);
            return;
        }

        if (stampCatalog == null)
        {
            Debug.LogError("StampSpawner: Stamp catalog is not assigned.", this);
            return;
        }

        if (gameManager != null && !gameManager.IsPlaying)
        {
            ResetSpawnCycle();
            CancelInvoke(nameof(Tick));
            Invoke(nameof(Tick), NotPlayingCheckInterval);
            return;
        }

        if (!spawnCycleInitialized)
        {
            spawnCycleInitialized = true;
            CancelInvoke(nameof(Tick));
            Invoke(nameof(Tick), Mathf.Max(0f, firstSpawnDelaySeconds));
            return;
        }

        TrySpawnStamp();

        float delay = Random.Range(minSpawnRate, maxSpawnRate);
        CancelInvoke(nameof(Tick));
        Invoke(nameof(Tick), delay);
    }

    private void TrySpawnStamp()
    {
        int activeStampCount = Object.FindObjectsByType<StampPickup>(FindObjectsSortMode.None).Length;
        if (activeStampCount >= maxConcurrentStamps)
        {
            if (debugSpawnLogs)
                Debug.Log($"StampSpawner: Active cap reached ({activeStampCount}/{maxConcurrentStamps}).", this);
            return;
        }

        if (Random.value > spawnChance)
            return;

        StampDefinition definition = PickWeightedStamp();
        if (definition == null)
            return;

        float minHeight = definition != null ? definition.SpawnMinHeight : fallbackMinHeight;
        float maxHeight = definition != null ? definition.SpawnMaxHeight : fallbackMaxHeight;
        if (maxHeight < minHeight)
            maxHeight = minHeight;

        Vector3 spawnPosition = transform.position + Vector3.up * Random.Range(minHeight, maxHeight);
        GameObject stampObject = Instantiate(stampPickupPrefab, spawnPosition, Quaternion.identity);

        StampPickup pickup = stampObject.GetComponent<StampPickup>();
        if (pickup == null)
        {
            Debug.LogWarning("StampSpawner: Spawned prefab has no StampPickup component.", stampObject);
            return;
        }

        pickup.Configure(definition, gameManager);

        if (debugSpawnLogs)
            Debug.Log($"StampSpawner: Spawned '{definition.DisplayName}' at {spawnPosition}", this);
    }

    private StampDefinition PickWeightedStamp()
    {
        var stamps = stampCatalog.Stamps;
        float totalWeight = 0f;

        for (int i = 0; i < stamps.Count; i++)
        {
            StampDefinition stamp = stamps[i];
            if (stamp == null || !stamp.EnabledForSpawning || stamp.SpawnWeight <= 0f)
                continue;

            totalWeight += stamp.SpawnWeight;
        }

        if (totalWeight <= 0f)
        {
            if (debugSpawnLogs)
                Debug.Log("StampSpawner: No enabled stamps with positive spawn weight.", this);
            return null;
        }

        float roll = Random.Range(0f, totalWeight);
        for (int i = 0; i < stamps.Count; i++)
        {
            StampDefinition stamp = stamps[i];
            if (stamp == null || !stamp.EnabledForSpawning || stamp.SpawnWeight <= 0f)
                continue;

            roll -= stamp.SpawnWeight;
            if (roll <= 0f)
                return stamp;
        }

        return null;
    }

    private void OnValidate()
    {
        if (maxSpawnRate < minSpawnRate) maxSpawnRate = minSpawnRate;
        if (firstSpawnDelaySeconds < 0f) firstSpawnDelaySeconds = 0f;
        if (fallbackMaxHeight < fallbackMinHeight) fallbackMaxHeight = fallbackMinHeight;
        if (maxConcurrentStamps < 1) maxConcurrentStamps = 1;
    }

    private void ResetSpawnCycle()
    {
        spawnCycleInitialized = false;
    }
}
