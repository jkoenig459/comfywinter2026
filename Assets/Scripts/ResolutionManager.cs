using UnityEngine;

public class ResolutionManager : MonoBehaviour
{
    [Header("Target Resolution")]
    [SerializeField] private int targetWidth = 320;
    [SerializeField] private int targetHeight = 180;
    [SerializeField] private bool fullscreen = false;

    [Header("Scaling")]
    [SerializeField] private int pixelScale = 4; // Scales the window by this factor
    [SerializeField] private bool maintainAspectRatio = true;

    private void Awake()
    {
        SetResolution();
    }

    private void SetResolution()
    {
        // Calculate window size based on pixel scale
        int windowWidth = targetWidth * pixelScale;
        int windowHeight = targetHeight * pixelScale;

        // Set the screen resolution
        Screen.SetResolution(windowWidth, windowHeight, fullscreen);

        // Ensure pixel perfect rendering
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        Debug.Log($"Resolution set to {windowWidth}x{windowHeight} (internal: {targetWidth}x{targetHeight})");
    }

    // Call this if you want to change resolution at runtime
    public void ChangeResolution(int width, int height, bool isFullscreen)
    {
        targetWidth = width;
        targetHeight = height;
        fullscreen = isFullscreen;
        SetResolution();
    }

    private void OnValidate()
    {
        // Update in editor when values change
        if (Application.isPlaying)
        {
            SetResolution();
        }
    }
}
