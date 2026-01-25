using UnityEngine;

public class MenuOpener : MonoBehaviour
{
    [Header("Prefab-safe menu lookup")]
    [SerializeField] private string targetMenuId = "Build";

    [Header("Behavior")]
    [SerializeField] private bool toggle = true;

    private void OnMouseUpAsButton()
    {
        if (BuildModePlacer.I != null && BuildModePlacer.I.IsPlacing)
            return;

        if (MenuController.IsPointerOverAnyOpenMenuUI(Input.mousePosition))
            return;

        if (!MenuController.TryGet(targetMenuId, out var menu) || menu == null)
        {
            return;
        }

        if (targetMenuId == "House" && HouseMenuUI.I != null)
        {
            HouseMenuUI.I.SetTargetHouse(gameObject);
        }

        if (toggle) menu.Toggle();
        else menu.Open();
    }
}