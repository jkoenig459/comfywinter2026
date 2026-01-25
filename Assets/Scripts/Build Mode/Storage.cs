using UnityEngine;

public class Storage : MonoBehaviour
{
    public static Storage I { get; private set; }

    [Header("Storage Tier")]
    [SerializeField] private int currentTier = 0;

    [Header("Tier 1 Sprites (Basic Storage)")]
    [Tooltip("Sprite shown when storage is empty")]
    public Sprite tier1EmptySprite;
    [Tooltip("Sprite shown when storage is full")]
    public Sprite tier1FullSprite;

    [Header("Tier 2 Sprites (Upgraded Storage)")]
    [Tooltip("Sprite shown when storage is empty")]
    public Sprite tier2EmptySprite;
    [Tooltip("Sprite shown when storage is full")]
    public Sprite tier2FullSprite;

    [Header("Upgrade Costs")]
    public int tier1IceCost = 3;
    public int tier2IceCost = 10;

    [Header("Storage Capacities")]
    public int tier0Capacity = 10;
    public int tier1Capacity = 25;
    public int tier2Capacity = 50;

    [Header("Full Threshold")]
    [Tooltip("Percentage of capacity to consider 'full' for visual purposes (0-1)")]
    [Range(0f, 1f)]
    public float fullThreshold = 0.8f;

    private SpriteRenderer spriteRenderer;

    public const int MAX_TIER = 2;

    public int CurrentTier => currentTier;
    public bool CanUpgrade => currentTier < MAX_TIER;

    public int CurrentCapacity
    {
        get
        {
            return currentTier switch
            {
                0 => tier0Capacity,
                1 => tier1Capacity,
                2 => tier2Capacity,
                _ => tier0Capacity
            };
        }
    }

    public int UpgradeCost
    {
        get
        {
            return currentTier switch
            {
                0 => tier1IceCost,
                1 => tier2IceCost,
                _ => 0
            };
        }
    }

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        UpdateVisuals();
    }

    private void Update()
    {
        // Update visuals based on current resource amounts
        UpdateVisuals();
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

        if (GameManager.I != null)
        {
            GameManager.I.maxStorage = CurrentCapacity;
        }

        UpdateVisuals();

        return true;
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer == null)
            return;

        if (currentTier == 0)
        {
            spriteRenderer.enabled = false;
            return;
        }

        spriteRenderer.enabled = true;

        bool isFull = IsStorageFull();

        if (currentTier == 1)
        {
            spriteRenderer.sprite = isFull ? tier1FullSprite : tier1EmptySprite;
        }
        else if (currentTier == 2)
        {
            spriteRenderer.sprite = isFull ? tier2FullSprite : tier2EmptySprite;
        }
    }

    private bool IsStorageFull()
    {
        if (GameManager.I == null)
            return false;

        int currentCapacity = CurrentCapacity;
        float threshold = currentCapacity * fullThreshold;

        return GameManager.I.ice >= threshold ||
               GameManager.I.food >= threshold ||
               GameManager.I.pebbles >= threshold;
    }

    public string GetUpgradeDescription()
    {
        if (!CanUpgrade)
            return "Max storage capacity reached!";

        int nextTier = currentTier + 1;
        int nextCapacity = nextTier switch
        {
            1 => tier1Capacity,
            2 => tier2Capacity,
            _ => tier0Capacity
        };

        return $"Upgrade to Tier {nextTier}. Increases storage capacity to {nextCapacity} per resource.";
    }
}
