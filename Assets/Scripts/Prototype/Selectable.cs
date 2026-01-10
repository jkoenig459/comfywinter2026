using UnityEngine;

public class Selectable : MonoBehaviour
{
    [SerializeField] private SpriteRenderer highlight;

    private void Awake()
    {
        // Auto-find a child named "Highlight" if not assigned
        if (highlight == null)
        {
            var t = transform.Find("Highlight");
            if (t != null) highlight = t.GetComponent<SpriteRenderer>();
        }

        if (highlight != null) highlight.enabled = false;
    }

    public void SetSelected(bool selected)
    {
        if (highlight != null) highlight.enabled = selected;
    }
}
