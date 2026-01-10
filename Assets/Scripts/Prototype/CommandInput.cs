using UnityEngine;

public class CommandInput : MonoBehaviour
{
    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
            IssueCommand();
    }

    private void IssueCommand()
    {
        var selectedObj = SelectionManager.I.SelectedObject;
        if (selectedObj == null) return;

        var unit = selectedObj.GetComponent<PenguinUnit>();
        if (unit == null) return;

        if (cam == null) cam = Camera.main;
        Vector2 world = cam.ScreenToWorldPoint(Input.mousePosition);

        // Check if clicked a resource node
        var hit = Physics2D.Raycast(world, Vector2.zero);
        if (hit.collider != null)
        {
            var node = hit.collider.GetComponentInParent<ResourceNode>();
            if (node != null)
            {
                unit.AssignGather(node);
                return;
            }
        }

        // Otherwise move command
        unit.AssignMove(world);
    }
}
