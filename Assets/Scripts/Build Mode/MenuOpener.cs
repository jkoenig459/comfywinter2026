using UnityEngine;

public class MenuOpener : MonoBehaviour
{
    [Header("Prefab-safe menu lookup")]
    [SerializeField] private string targetMenuId = "Build";

    [Header("Behavior")]
    [SerializeField] private bool toggle = true;

    private void OnMouseUpAsButton()
    {
        if (MenuController.IsPointerOverAnyOpenMenuUI(Input.mousePosition))
            return;

        if (!MenuController.TryGet(targetMenuId, out var menu) || menu == null)
        {
            Debug.LogWarning($"{name}: No MenuController registered with id '{targetMenuId}'.");
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