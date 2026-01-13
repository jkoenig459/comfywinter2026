using UnityEngine;

public class ResourcePile : MonoBehaviour
{
    [Header("Resource")]
    public ResourceType type = ResourceType.Food;

    [Tooltip("Sprite shown when a penguin is carrying 1 of this resource (ex: Fish sprite, Ice sprite).")]
    public Sprite carrySprite;

    [Header("Pile Visuals (4 stages)")]
    [Tooltip("4 sprites representing pile size. Index 0 = 1 item, 3 = 4+ items.")]
    public Sprite[] pileFrames = new Sprite[4];

    [Header("Settings")]
    public int maxCount = 4;

    [SerializeField] private int count = 0;
    private SpriteRenderer sr;

    public ResourcePile outputPile;


    public int Count => count;
    public bool IsEmpty => count <= 0;
    public bool IsFull => count >= maxCount;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        Refresh();
    }

    public void Add(int amount = 1)
    {
        count = Mathf.Clamp(count + amount, 0, maxCount);
        Refresh();
    }

    public bool TryTakeOne(out int taken)
    {
        if (count <= 0)
        {
            taken = 0;
            return false;
        }

        count--;
        taken = 1;
        Refresh();
        return true;
    }

    private void Refresh()
    {
        if (!sr) return;

        if (count <= 0)
        {
            sr.enabled = false;
            return;
        }

        sr.enabled = true;

        int idx = Mathf.Clamp(count - 1, 0, pileFrames.Length - 1);
        sr.sprite = pileFrames[idx];
    }
}
