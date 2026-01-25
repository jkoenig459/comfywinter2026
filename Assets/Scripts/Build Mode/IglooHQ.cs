using UnityEngine;

/// <summary>
/// Marker component for the main Igloo HQ.
/// Used to identify the HQ for placement restrictions.
/// </summary>
public class IglooHQ : MonoBehaviour
{
    public static IglooHQ Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple IglooHQ instances found. Only one should exist.");
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
