using UnityEngine;

public class House : MonoBehaviour
{
    [Header("Tier Settings")]
    [SerializeField] private int currentTier = 1;

    [Header("Sprites (assign in Inspector)")]
    [Tooltip("Sprite for Tier 1 house")]
    public Sprite tier1Sprite;
    [Tooltip("Sprite for Tier 2 house")]
    public Sprite tier2Sprite;
    [Tooltip("Sprite for Tier 3 house")]
    public Sprite tier3Sprite;

    [Header("Upgrade Costs")]
    public int tier2IceCost = 10;
    public int tier3IceCost = 20;

    [Header("Penguin Spawn Cost")]
    public int penguinFishCost = 5;
    public int penguinPebbleCost = 1;

    [Header("Collider")]
    [Tooltip("If true, automatically resize BoxCollider2D to match sprite bounds on upgrade.")]
    public bool autoResizeCollider = true;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;

    public const int MAX_TIER = 3;

    public int CurrentTier => currentTier;
    public bool CanUpgrade => currentTier < MAX_TIER;

    public int MaxPenguins
    {
        get
        {
            return currentTier switch
            {
                1 => 2,
                2 => 4,
                3 => 8,
                _ => 2
            };
        }
    }

    public int UpgradeCost
    {
        get
        {
            return currentTier switch
            {
                1 => tier2IceCost,
                2 => tier3IceCost,
                _ => 0
            };
        }
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
            boxCollider = GetComponentInChildren<BoxCollider2D>();

        UpdateSprite();
    }

    public bool TryUpgrade()
    {
        if (!CanUpgrade)
            return false;

        int cost = UpgradeCost;

        if (GameManager.I == null || GameManager.I.ice < cost)
            return false;

        GameManager.I.ice -= cost;
        currentTier++;
        UpdateSprite();

        Debug.Log($"House upgraded to Tier {currentTier}! Max penguins: {MaxPenguins}");
        return true;
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        // Store the bottom position before changing sprite
        Vector2 oldBottom = GetSpriteBottom();

        Sprite targetSprite = currentTier switch
        {
            1 => tier1Sprite,
            2 => tier2Sprite,
            3 => tier3Sprite,
            _ => tier1Sprite
        };

        if (targetSprite != null)
        {
            spriteRenderer.sprite = targetSprite;

            // Adjust position to keep bottom anchored
            Vector2 newBottom = GetSpriteBottom();
            Vector2 offset = oldBottom - newBottom;
            transform.position += (Vector3)offset;

            UpdateCollider(targetSprite);
        }
    }

    private Vector2 GetSpriteBottom()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return transform.position;

        Bounds bounds = spriteRenderer.bounds;
        return new Vector2(bounds.center.x, bounds.min.y);
    }

    private void UpdateCollider(Sprite sprite)
    {
        if (!autoResizeCollider) return;
        if (boxCollider == null || sprite == null) return;

        // Resize and recalculate offset based on new sprite bounds
        boxCollider.size = sprite.bounds.size;
        boxCollider.offset = sprite.bounds.center;
    }

    public string GetUpgradeDescription()
    {
        if (!CanUpgrade)
            return "Max tier reached!";

        int nextTier = currentTier + 1;
        int nextCapacity = nextTier switch
        {
            2 => 4,
            3 => 8,
            _ => 2
        };

        return $"Upgrade to Tier {nextTier}. Increases capacity to {nextCapacity} Penguins.";
    }
}