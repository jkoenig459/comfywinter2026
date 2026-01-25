using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager I { get; private set; }

    private Camera cam;
    private Selectable selected;

    // Cache the mask that ignores the Resources layer
    private int selectionMask;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        cam = Camera.main;

        selectionMask = ~LayerMask.GetMask("Resources");
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TrySelectUnderMouse();
    }

    private void TrySelectUnderMouse()
    {
        if (cam == null) cam = Camera.main;

        Vector2 world = cam.ScreenToWorldPoint(Input.mousePosition);

        RaycastHit2D hit = Physics2D.Raycast(
            world,
            Vector2.zero,
            0f,
            selectionMask
        );

        if (!hit.collider)
        {
            SetSelected(null);
            return;
        }

        Selectable sel = hit.collider.GetComponentInParent<Selectable>();
        SetSelected(sel);
    }

    private void SetSelected(Selectable sel)
    {
        if (selected != null)
            selected.SetSelected(false);

        selected = sel;

        if (selected != null)
        {
            selected.SetSelected(true);

            if (AudioManager.I != null)
                AudioManager.I.PlayPenguinChirp();
        }
    }

    public GameObject SelectedObject => selected ? selected.gameObject : null;
}
