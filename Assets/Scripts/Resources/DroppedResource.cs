using UnityEngine;

public class DroppedResource : MonoBehaviour
{
    [Header("Resource")]
    public ResourceType type = ResourceType.Food;

    [Header("Visuals")]
    [Tooltip("Sprite shown when a penguin is carrying this resource.")]
    public Sprite carrySprite;

    [Header("Settings")]
    [Tooltip("If true, destroy this object when picked up.")]
    public bool destroyOnPickup = true;

    private bool pickedUp = false;

    public bool IsPickedUp => pickedUp;

    public bool TryPickup(out Sprite sprite, out ResourceType resourceType)
    {
        if (pickedUp)
        {
            sprite = null;
            resourceType = ResourceType.Food;
            return false;
        }

        pickedUp = true;
        sprite = carrySprite;
        resourceType = type;

        if (destroyOnPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);

        return true;
    }

    public void Initialize(ResourceType resourceType, Sprite sprite)
    {
        type = resourceType;
        carrySprite = sprite;

        // Update visual if there's a SpriteRenderer
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && sprite != null)
        {
            sr.sprite = sprite;
        }
    }
}
