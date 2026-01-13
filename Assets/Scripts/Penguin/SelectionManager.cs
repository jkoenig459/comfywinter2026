using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager I { get; private set; }

    private Camera cam;
    private Selectable selected;

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        cam = Camera.main;
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

        var hit = Physics2D.Raycast(world, Vector2.zero);

        Debug.Log($"Click at world {world}, hit: {(hit.collider ? hit.collider.name : "none")}");

        if (!hit.collider)
        {

            SetSelected(null);
            return;
        }

        var sel = hit.collider.GetComponentInParent<Selectable>();
        SetSelected(sel);
    }

    private void SetSelected(Selectable sel)
    {
        if (selected != null) selected.SetSelected(false);
        selected = sel;
        if (selected != null) selected.SetSelected(true);
    }

    public GameObject SelectedObject => selected ? selected.gameObject : null;
}
