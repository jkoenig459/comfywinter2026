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

        // Prevent penguins from colliding with each other (Layer 8 = Penguin layer)
        Physics2D.IgnoreLayerCollision(8, 8, true);
    }

    private void Start()
    {
        // Play initial game music
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

        // Change music to loop 2 when reaching 10 penguins
        if (totalPenguins >= 10 && !musicChangedTo10Penguins)
        {
            musicChangedTo10Penguins = true;
            if (AudioManager.I != null)
                AudioManager.I.PlayMusicLoop2();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
    }

    private void Update()
    {
        // Debug cheat: F1 to add resources for testing
        if (Input.GetKeyDown(KeyCode.F1))
        {
            AddIce(50);
            AddFood(50);
            AddPebbles(50);
            Debug.Log("Added 50 of each resource! Ice: " + ice + ", Food: " + food + ", Pebbles: " + pebbles);
        }
    }
}
