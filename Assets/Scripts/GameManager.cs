using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    [Header("Resources")]
    public int ice = 0;
    public int food = 0;
    public int pebbles = 0;

    [Header("Storage")]
    public int maxStorage = 10;

    [Header("Penguins")]
    public int totalPenguins = 0;
    private bool musicChangedTo10Penguins = false;

    [Header("End Game")]
    public GameObject endGameTransition;
    private bool endGameTriggered = false;

    [Header("Pause")]
    public bool isPaused;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);

        Physics2D.IgnoreLayerCollision(8, 8, true);
    }

    private void Start()
    {
        if (AudioManager.I != null)
            AudioManager.I.PlayMusicLoop1();
    }

    public void AddIce(int amount)
    {
        ice = Mathf.Min(ice + amount, maxStorage);
    }

    public void AddFood(int amount)
    {
        food = Mathf.Min(food + amount, maxStorage);
    }

    public void AddPebbles(int amount)
    {
        pebbles = Mathf.Min(pebbles + amount, maxStorage);
    }

    public void AddPenguin()
    {
        totalPenguins++;

        if (totalPenguins >= 10 && !musicChangedTo10Penguins)
        {
            musicChangedTo10Penguins = true;
            if (AudioManager.I != null)
                AudioManager.I.PlayMusicLoop2();
        }

        if (totalPenguins >= 20 && !endGameTriggered)
        {
            endGameTriggered = true;
            if (AudioManager.I != null)
                AudioManager.I.StopAllAudio();
            if (endGameTransition != null)
                endGameTransition.SetActive(true);
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            AddIce(50);
            AddFood(50);
            AddPebbles(50);
        }
    }
}
