using UnityEngine;

public class IglooHQ : MonoBehaviour
{
    public static IglooHQ Instance { get; private set; }

    private void Awake()
    {
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
