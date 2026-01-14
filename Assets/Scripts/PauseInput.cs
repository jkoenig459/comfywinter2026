using UnityEngine;

public class PauseInput : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            GameManager.I.TogglePause();
    }
}
