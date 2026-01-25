using UnityEngine;

public class House : MonoBehaviour
{
    [Header("Tier Settings")]
    [SerializeField] private int currentTier = 1;
    [SerializeField] private int penguinsCreated = 0;

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
    [Tooltip("If true, automatically resize collider to match sprite bounds on upgrade.")]
    public bool autoResizeCollider = true;

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;

    public const int MAX_TIER = 3;

    public int CurrentTier => currentTier;
    public bool CanUpgrade => currentTier < MAX_TIER;
    public int PenguinsCreated => penguinsCreated;

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

    public bool CanCreatePenguin => penguinsCreated < MaxPenguins;
    public int RemainingPenguinSlots => Mathf.Max(0, MaxPenguins - penguinsCreated);

    public void IncrementPenguinCount()
    {
        penguinsCreated++;
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

        circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider == null)
            circleCollider = GetComponentInChildren<CircleCollider2D>();

        if (GetComponent<YSorter>() == null)
        {
            var ySorter = gameObject.AddComponent<YSorter>();
            ySorter.sortingOrderOffset = -10;
        }

        UpdateSprite(updateCollider: false);
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
        UpdateSprite(updateCollider: true);

        if (AudioManager.I != null)
            AudioManager.I.PlayIglooUpgrade();

        return true;
    }

    private void UpdateSprite(bool updateCollider = true)
    {
        if (spriteRenderer == null) return;

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

            Vector2 newBottom = GetSpriteBottom();
            Vector2 offset = oldBottom - newBottom;
            transform.position += (Vector3)offset;

            if (updateCollider)
            {
                UpdateCollider(targetSprite);
            }
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
        if (circleCollider == null || sprite == null) return;

        float width = sprite.rect.width / sprite.pixelsPerUnit;
        float height = sprite.rect.height / sprite.pixelsPerUnit;
        float newRadius = Mathf.Max(width, height) * 0.5f;

        Vector2 rectCenter = new Vector2(sprite.rect.width * 0.5f, sprite.rect.height * 0.5f);
        Vector2 pivotToCenter = rectCenter - sprite.pivot;
        Vector2 newOffset = pivotToCenter / sprite.pixelsPerUnit;

        circleCollider.radius = newRadius;
        circleCollider.offset = newOffset;
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