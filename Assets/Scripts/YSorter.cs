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

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        cachedTransform = transform;
    }

    private void LateUpdate()
    {
        // Convert Y position to sorting order
        // Lower Y = higher sorting order (renders in front)
        // Multiply by -1 so objects lower on screen appear in front
        int sortingOrder = Mathf.RoundToInt(-cachedTransform.position.y * sortingOrderMultiplier) + sortingOrderOffset;
        spriteRenderer.sortingOrder = sortingOrder;
    }
}
