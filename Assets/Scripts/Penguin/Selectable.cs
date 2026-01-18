using UnityEngine;

public class Selectable : MonoBehaviour
{
    [Header("Optional highlight sprite")]
    [SerializeField] private SpriteRenderer highlight;

    [Header("Optional OutlineFx component")]
    [SerializeField] private Behaviour outlineFx;

    private void Awake()
    {
        if (highlight == null)
        {
            var t = transform.Find("Highlight");
            if (t != null)
                highlight = t.GetComponent<SpriteRenderer>();
        }

        if (outlineFx == null)
        {
            outlineFx = GetComponent<MonoBehaviour>();
            var behaviours = GetComponents<Behaviour>();
            foreach (var b in behaviours)
            {
                if (b != null && b.GetType().Name == "OutlineFx")
                {
                    outlineFx = b;
                    break;
                }
            }
        }

        // Start disabled
        if (highlight != null) highlight.enabled = false;
        if (outlineFx != null) outlineFx.enabled = false;
    }

    public void SetSelected(bool selected)
    {
        if (highlight != null)
            highlight.enabled = selected;

        if (outlineFx != null)
            outlineFx.enabled = selected;
    }
}
