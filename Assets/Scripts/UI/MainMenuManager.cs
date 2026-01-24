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

    [Header("Transition")]
    [SerializeField] private Animator transition;

    private void Start()
    {
        // Ensure main menu is shown and others are hidden
        ShowMainMenu();

        // Play main menu music
        if (AudioManager.I != null)
            AudioManager.I.PlayMusicLoop3();
    }

    private void FixedUpdate()
    {
        // Check if animation is finished
        if (transition.GetCurrentAnimatorStateInfo(0).IsName("main_menu_transition_end") == true)
        {
            // Load the main game scene
            SceneManager.LoadScene(gameSceneName);
        }
    }


    // Called by Start Button
    public void OnStartButtonClicked()
    {
        // Play start button sound
        if (AudioManager.I != null)
            AudioManager.I.PlayStartButton();

        // Play the transition animation
        transition.SetTrigger("Transition");

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
