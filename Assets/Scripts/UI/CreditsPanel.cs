using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CreditsPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI creditsText;

    [Header("Credits Content")]
    [TextArea(10, 20)]
    [SerializeField] private string credits =
        "CREDITS\n\n" +
        "GAME DESIGN & PROGRAMMING\n" +
        "Your Name Here\n\n" +
        "ART & ANIMATION\n" +
        "Your Name Here\n\n" +
        "AUDIO\n" +
        "Your Name Here\n\n" +
        "SPECIAL THANKS\n" +
        "Claude Code for pathfinding assistance\n\n" +
        "Made with Unity\n" +
        "© 2026";

    private void OnEnable()
    {
        // Update text when panel is shown
        if (creditsText != null)
        {
            creditsText.text = credits;
        }
    }
}
