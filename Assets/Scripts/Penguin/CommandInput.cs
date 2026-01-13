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

        var unit = selectedObj.GetComponentInParent<PenguinJobs>();
        if (unit == null) return;

        // Block orders while dropping off resources
        if (!unit.CanAcceptOrders)
            return;

        if (cam == null) cam = Camera.main;
        Vector2 world = cam.ScreenToWorldPoint(Input.mousePosition);

        var hit = Physics2D.Raycast(world, Vector2.zero);
        if (hit.collider != null)
        {
            var pile = hit.collider.GetComponentInParent<ResourcePile>();
            if (pile != null)
            {
                unit.AssignPickup(pile);
                return;
            }

            var node = hit.collider.GetComponentInParent<ResourceNode>();
            if (node != null)
            {
                if (node.type == ResourceType.Food)
                {
                    unit.AssignFish(node);
                    return;
                }

                if (node.type == ResourceType.Ice)
                {
                    unit.AssignCutIce(node);
                    return;
                }

                unit.AssignMove(node.transform.position);
                return;
            }
        }

        unit.AssignMove(world);
    }
}
