using UnityEngine;
using UnityEngine.UIElements;

public class SettingsButton : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Settings")]
    [SerializeField] private SettingsUI settingsUI;

    [Header("Custom Sprites (Optional)")]
    [Tooltip("Custom sprite for the settings button. Leave empty to use default.")]
    public Sprite customButtonSprite;

    private VisualElement root;
    private Button settingsButton;

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        Bind();
        ApplyCustomSprite();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Bind()
    {
        if (uiDocument == null)
        {
            Debug.LogError("SettingsButton: Missing UIDocument.");
            enabled = false;
            return;
        }

        root = uiDocument.rootVisualElement;

        settingsButton = root.Q<Button>("SettingsButton");

        if (settingsButton == null)
        {
            Debug.LogError("SettingsButton: Missing Button 'SettingsButton' in UI.");
            return;
        }

        settingsButton.clicked += OnSettingsClicked;
    }

    private void Unbind()
    {
        if (settingsButton != null)
        {
            settingsButton.clicked -= OnSettingsClicked;
        }
    }

    private void ApplyCustomSprite()
    {
        if (settingsButton == null || customButtonSprite == null)
            return;

        // Apply custom sprite via USS style
        // The sprite needs to be converted to a background image
        var background = new StyleBackground(customButtonSprite);
        settingsButton.style.backgroundImage = background;
    }

    private void OnSettingsClicked()
    {
        if (settingsUI != null)
        {
            settingsUI.Open();
        }
        else
        {
            Debug.LogWarning("SettingsButton: SettingsUI reference not assigned!");
        }
    }
}
