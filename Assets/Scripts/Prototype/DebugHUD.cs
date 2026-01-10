using TMPro;
using UnityEngine;

public class DebugHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    private void Reset()
    {
        text = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (GameManager.I == null) return;

        text.text =
            $"ICE: {GameManager.I.ice}\n" +
            $"FOOD: {GameManager.I.food}\n" +
            $"PAUSED: {GameManager.I.isPaused}";
    }
}
