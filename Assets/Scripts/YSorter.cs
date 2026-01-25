using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSorter : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Higher values = more precise sorting. Default is 100 (precision to 0.01 units)")]
    public int sortingOrderMultiplier = 100;

    [Tooltip("Base sorting offset. Use this if you want this object to always render above/below others at the same Y position")]
    public int sortingOrderOffset = 0;

    private SpriteRenderer spriteRenderer;
    private Transform cachedTransform;

    // Spawning mode: temporarily render behind a specific object until clear of buildings
    private bool isSpawning;
    private int spawningSortingOrder;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        cachedTransform = transform;
    }

    private void LateUpdate()
    {
        if (isSpawning)
        {
            spriteRenderer.sortingOrder = spawningSortingOrder;
            return;
        }

        int sortingOrder = Mathf.RoundToInt(-cachedTransform.position.y * sortingOrderMultiplier) + sortingOrderOffset;
        spriteRenderer.sortingOrder = sortingOrder;
    }

    public void EnterSpawningMode(int sortingOrder)
    {
        isSpawning = true;
        spawningSortingOrder = sortingOrder;
    }

    public void ExitSpawningMode()
    {
        isSpawning = false;
    }

    public bool IsSpawning => isSpawning;
}
