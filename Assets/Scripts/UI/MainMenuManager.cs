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
        ShowMainMenu();

        if (AudioManager.I != null)
            AudioManager.I.PlayMusicLoop3();
    }

    private void FixedUpdate()
    {
        if (transition.GetCurrentAnimatorStateInfo(0).IsName("main_menu_transition_end") == true)
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void OnStartButtonClicked()
    {
        if (AudioManager.I != null)
            AudioManager.I.PlayStartButton();

        transition.SetTrigger("Transition");
    }

    public void OnHowToPlayButtonClicked()
    {
        mainMenuPanel.SetActive(false);
        howToPlayPanel.SetActive(true);
    }

    public void OnCreditsButtonClicked()
    {
        mainMenuPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    public void OnQuitButtonClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

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
