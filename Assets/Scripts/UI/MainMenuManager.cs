using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panel References")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Settings")]
    [SerializeField] private string gameSceneName = "Main";

    private void Start()
    {
        // Ensure main menu is shown and others are hidden
        ShowMainMenu();
    }

    // Called by Start Button
    public void OnStartButtonClicked()
    {
        // Load the main game scene
        SceneManager.LoadScene(gameSceneName);
    }

    // Called by How to Play Button
    public void OnHowToPlayButtonClicked()
    {
        // Show how to play panel
        mainMenuPanel.SetActive(false);
        howToPlayPanel.SetActive(true);
    }

    // Called by Credits Button
    public void OnCreditsButtonClicked()
    {
        // Show credits panel
        mainMenuPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    // Called by Quit Button
    public void OnQuitButtonClicked()
    {
        // Quit the application
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Called by Back buttons in sub-panels
    public void OnBackButtonClicked()
    {
        ShowMainMenu();
    }

    private void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        howToPlayPanel.SetActive(false);
        creditsPanel.SetActive(false);
    }
}
