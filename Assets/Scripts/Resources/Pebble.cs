using UnityEngine;

public class Pebble : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("Sprite shown when a penguin is carrying this pebble.")]
    public Sprite carrySprite;

    [Header("Settings")]
    [Tooltip("If true, destroy this object when picked up.")]
    public bool destroyOnPickup = true;

    private bool pickedUp = false;

    public bool IsPickedUp => pickedUp;

    public bool TryPickup(out Sprite sprite)
    {
        if (pickedUp)
        {
            sprite = null;
            return false;
        }

        pickedUp = true;
        sprite = carrySprite;

        if (destroyOnPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);

        return true;
    }
}