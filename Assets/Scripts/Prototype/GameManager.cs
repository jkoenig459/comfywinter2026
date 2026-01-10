using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    [Header("Resources")]
    public int ice = 0;
    public int food = 0;

    [Header("Pause")]
    public bool isPaused;

    private void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    public void AddIce(int amount) => ice += amount;
    public void AddFood(int amount) => food += amount;

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
    }
}
