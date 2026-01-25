using UnityEngine;

public class PebbleSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject pebblePrefab;

    [Header("Spawn Settings")]
    [Tooltip("Minimum seconds between spawn attempts.")]
    public float minSpawnInterval = 10f;

    [Tooltip("Maximum seconds between spawn attempts.")]
    public float maxSpawnInterval = 30f;

    [Tooltip("Maximum pebbles allowed in the world at once.")]
    public int maxPebbles = 5;

    [Header("Spawn Area")]
    [Tooltip("Margin from camera edges (in world units).")]
    public float edgeMargin = 0.5f;

    [Tooltip("Radius to check for overlapping objects before spawning.")]
    public float overlapCheckRadius = 0.3f;

    [Tooltip("Layers that block pebble spawning.")]
    public LayerMask blockingLayers;

    [Header("Spawn Attempts")]
    [Tooltip("Max attempts to find a clear spawn position.")]
    public int maxSpawnAttempts = 10;

    private Camera cam;
    private float nextSpawnTime;

    private void Awake()
    {
        cam = Camera.main;
        ScheduleNextSpawn();
    }

    private void Update()
    {
        if (Time.time < nextSpawnTime) return;

        TrySpawnPebble();
        ScheduleNextSpawn();
    }

    private void ScheduleNextSpawn()
    {
        nextSpawnTime = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    private void TrySpawnPebble()
    {
        if (pebblePrefab == null)
        {
            return;
        }

        int currentCount = FindObjectsByType<Pebble>(FindObjectsSortMode.None).Length;
        if (currentCount >= maxPebbles)
            return;

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector2 spawnPos = GetRandomPositionInView();

            if (IsPositionClear(spawnPos))
            {
                Instantiate(pebblePrefab, spawnPos, Quaternion.identity);
                return;
            }
        }
    }

    private Vector2 GetRandomPositionInView()
    {
        if (cam == null)
            cam = Camera.main;

        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;

        Vector2 camCenter = cam.transform.position;

        float halfWidth = (camWidth / 2f) - edgeMargin;
        float halfHeight = (camHeight / 2f) - edgeMargin;

        float x = camCenter.x + Random.Range(-halfWidth, halfWidth);
        float y = camCenter.y + Random.Range(-halfHeight, halfHeight);

        return new Vector2(x, y);
    }

    private bool IsPositionClear(Vector2 position)
    {
        Collider2D hit = Physics2D.OverlapCircle(position, overlapCheckRadius, blockingLayers);
        return hit == null;
    }
}