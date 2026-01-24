using UnityEngine;
using UnityEngine.UIElements;

public class SettingsUI : MonoBehaviour
{
    public static SettingsUI I { get; private set; }

    [Header("UI")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private MenuController menuController;

    private VisualElement root;

    private Slider musicSlider;
    private Slider penguinSFXSlider;
    private Slider resourceSFXSlider;

    private Label musicLabel;
    private Label penguinSFXLabel;
    private Label resourceSFXLabel;

    private Button closeButton;

    private void Awake()
    {
        I = this;

        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
        if (menuController == null)
            menuController = GetComponent<MenuController>();
    }

    private void OnEnable()
    {
        Bind();
        LoadCurrentValues();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Bind()
    {
        if (uiDocument == null)
        {
            Debug.LogError("SettingsUI: Missing UIDocument.");
            enabled = false;
            return;
        }

        root = uiDocument.rootVisualElement;
        root.pickingMode = PickingMode.Position;

        // Find sliders
        musicSlider = root.Q<Slider>("MusicSlider");
        penguinSFXSlider = root.Q<Slider>("PenguinSFXSlider");
        resourceSFXSlider = root.Q<Slider>("ResourceSFXSlider");

        // Find labels
        musicLabel = root.Q<Label>("MusicLabel");
        penguinSFXLabel = root.Q<Label>("PenguinSFXLabel");
        resourceSFXLabel = root.Q<Label>("ResourceSFXLabel");

        // Find close button
        closeButton = root.Q<Button>("CloseButton");

        // Log warnings for missing elements
        if (musicSlider == null) Debug.LogError("SettingsUI: Missing Slider 'MusicSlider'.");
        if (penguinSFXSlider == null) Debug.LogError("SettingsUI: Missing Slider 'PenguinSFXSlider'.");
        if (resourceSFXSlider == null) Debug.LogError("SettingsUI: Missing Slider 'ResourceSFXSlider'.");

        // Register callbacks
        if (musicSlider != null)
        {
            musicSlider.RegisterValueChangedCallback(OnMusicVolumeChanged);
        }

        if (penguinSFXSlider != null)
        {
            penguinSFXSlider.RegisterValueChangedCallback(OnPenguinSFXVolumeChanged);
        }

        if (resourceSFXSlider != null)
        {
            resourceSFXSlider.RegisterValueChangedCallback(OnResourceSFXVolumeChanged);
        }

        if (closeButton != null)
        {
            closeButton.clicked += OnCloseClicked;
        }
    }

    private void Unbind()
    {
        if (musicSlider != null)
            musicSlider.UnregisterValueChangedCallback(OnMusicVolumeChanged);

        if (penguinSFXSlider != null)
            penguinSFXSlider.UnregisterValueChangedCallback(OnPenguinSFXVolumeChanged);

        if (resourceSFXSlider != null)
            resourceSFXSlider.UnregisterValueChangedCallback(OnResourceSFXVolumeChanged);

        if (closeButton != null)
            closeButton.clicked -= OnCloseClicked;
    }

    private void LoadCurrentValues()
    {
        if (AudioManager.I == null)
            return;

        if (musicSlider != null)
        {
            musicSlider.value = AudioManager.I.musicVolume;
            UpdateMusicLabel(AudioManager.I.musicVolume);
        }

        if (penguinSFXSlider != null)
        {
            penguinSFXSlider.value = AudioManager.I.penguinSFXVolume;
            UpdatePenguinSFXLabel(AudioManager.I.penguinSFXVolume);
        }

        if (resourceSFXSlider != null)
        {
            resourceSFXSlider.value = AudioManager.I.resourceSFXVolume;
            UpdateResourceSFXLabel(AudioManager.I.resourceSFXVolume);
        }
    }

    private void OnMusicVolumeChanged(ChangeEvent<float> evt)
    {
        if (AudioManager.I != null)
        {
            AudioManager.I.SetMusicVolume(evt.newValue);
        }

        UpdateMusicLabel(evt.newValue);
    }

    private void OnPenguinSFXVolumeChanged(ChangeEvent<float> evt)
    {
        if (AudioManager.I != null)
        {
            AudioManager.I.SetPenguinSFXVolume(evt.newValue);
        }

        UpdatePenguinSFXLabel(evt.newValue);
    }

    private void OnResourceSFXVolumeChanged(ChangeEvent<float> evt)
    {
        if (AudioManager.I != null)
        {
            AudioManager.I.SetResourceSFXVolume(evt.newValue);
        }

        UpdateResourceSFXLabel(evt.newValue);
    }

    private void UpdateMusicLabel(float value)
    {
        if (musicLabel != null)
        {
            musicLabel.text = $"Music: {Mathf.RoundToInt(value * 100)}%";
        }
    }

    private void UpdatePenguinSFXLabel(float value)
    {
        if (penguinSFXLabel != null)
        {
            penguinSFXLabel.text = $"Penguin SFX: {Mathf.RoundToInt(value * 100)}%";
        }
    }

    private void UpdateResourceSFXLabel(float value)
    {
        if (resourceSFXLabel != null)
        {
            resourceSFXLabel.text = $"Resource SFX: {Mathf.RoundToInt(value * 100)}%";
        }
    }

    private void OnCloseClicked()
    {
        menuController?.Close();
    }

    public void Open()
    {
        menuController?.Open();
    }
}
