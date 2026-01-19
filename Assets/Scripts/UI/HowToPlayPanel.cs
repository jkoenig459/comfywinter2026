using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HowToPlayPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI instructionsText;

    [Header("Instructions Content")]
    [TextArea(10, 20)]
    [SerializeField] private string instructions =
        "HOW TO PLAY\n\n" +
        "SELECT PENGUINS:\n" +
        "• Click to select a penguin\n" +
        "• Hold Shift and click to add penguins to selection\n" +
        "• Drag to box-select multiple penguins\n\n" +
        "COMMANDS:\n" +
        "• Right-click resources to gather\n" +
        "• Right-click resource piles to haul\n\n" +
        "BUILDING:\n" +
        "• Press [B] to open build menu\n" +
        "• Select a structure to place\n" +
        "• Click to place the structure\n\n" +
        "GOAL:\n" +
        "Build and manage your penguin colony!";

    private void OnEnable()
    {
        // Update text when panel is shown
        if (instructionsText != null)
        {
            instructionsText.text = instructions;
        }
    }
}
