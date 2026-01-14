using UnityEngine;

public class OpenBuildMenu : MonoBehaviour
{
    private void OnMouseDown()
    {
        BuildMenuController.I?.Toggle();
    }
}
