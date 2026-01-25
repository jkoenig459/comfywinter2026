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
        int windowWidth = targetWidth * pixelScale;
        int windowHeight = targetHeight * pixelScale;

        Screen.SetResolution(windowWidth, windowHeight, fullscreen);

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    public void ChangeResolution(int width, int height, bool isFullscreen)
    {
        targetWidth = width;
        targetHeight = height;
        fullscreen = isFullscreen;
        SetResolution();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            SetResolution();
        }
    }
}
