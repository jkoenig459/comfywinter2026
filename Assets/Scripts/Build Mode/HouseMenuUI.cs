using UnityEngine;
using UnityEngine.UIElements;

public class HouseMenuUI : MonoBehaviour
{
    public static HouseMenuUI I { get; private set; }

    [Header("UI")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private MenuController menuController;

    [Header("Penguin Spawning")]
    [SerializeField] private GameObject penguinPrefab;
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, -0.5f);

    private VisualElement root;

    private Label itemName;
    private Label itemDesc;
    private Label itemCost;

    private Button upgradeBtn;
    private Button addPenguinBtn;
    private Button buildBtn;

    private GameObject currentHouseObject;
    private House currentHouse;

    private enum HouseSelection { None, Upgrade, AddPenguin }
    private HouseSelection currentSelection = HouseSelection.None;

    private const string DEFAULT_DESC = "Click an option on the left for more information";

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
        ShowDefaultInfo();
    }

    private void OnDisable()
    {
        Unbind();
    }

    public void SetTargetHouse(GameObject house)
    {
        currentHouseObject = house;
        currentHouse = house != null ? house.GetComponent<House>() : null;

        if (currentHouse == null && house != null)
            Debug.LogWarning("HouseMenuUI: Target house has no House component.");
    }

    private void Bind()
    {
        if (uiDocument == null)
        {
            Debug.LogError("HouseMenuUI: Missing UIDocument.");
            enabled = false;
            return;
        }

        root = uiDocument.rootVisualElement;
        root.pickingMode = PickingMode.Position;

        itemName = root.Q<Label>("ItemName");
        itemDesc = root.Q<Label>("ItemDesc");
        itemCost = root.Q<Label>("ItemCost");

        upgradeBtn = root.Q<Button>("HouseUpgradeBtn");
        addPenguinBtn = root.Q<Button>("AddPenguinBtn");
        buildBtn = root.Q<Button>("BuildBtn");

        if (itemName == null) Debug.LogError("HouseMenuUI: Missing Label 'ItemName'.");
        if (itemDesc == null) Debug.LogError("HouseMenuUI: Missing Label 'ItemDesc'.");
        if (itemCost == null) Debug.LogError("HouseMenuUI: Missing Label 'ItemCost'.");
        if (upgradeBtn == null) Debug.LogError("HouseMenuUI: Missing Button 'HouseUpgradeBtn'.");
        if (addPenguinBtn == null) Debug.LogError("HouseMenuUI: Missing Button 'AddPenguinBtn'.");
        if (buildBtn == null) Debug.LogError("HouseMenuUI: Missing Button 'BuildBtn'.");

        if (upgradeBtn != null) upgradeBtn.RegisterCallback<ClickEvent>(OnUpgradeClicked);
        if (addPenguinBtn != null) addPenguinBtn.RegisterCallback<ClickEvent>(OnAddPenguinClicked);
        if (buildBtn != null) buildBtn.RegisterCallback<ClickEvent>(OnBuildClicked);
    }

    private void Unbind()
    {
        if (upgradeBtn != null) upgradeBtn.UnregisterCallback<ClickEvent>(OnUpgradeClicked);
        if (addPenguinBtn != null) addPenguinBtn.UnregisterCallback<ClickEvent>(OnAddPenguinClicked);
        if (buildBtn != null) buildBtn.UnregisterCallback<ClickEvent>(OnBuildClicked);
    }

    private void ShowDefaultInfo()
    {
        currentSelection = HouseSelection.None;

        if (itemName != null) itemName.text = "";
        if (itemDesc != null) itemDesc.text = DEFAULT_DESC;
        if (itemCost != null) itemCost.text = "";
    }

    private void OnUpgradeClicked(ClickEvent evt)
    {
        // Play UI button click sound
        if (AudioManager.I != null)
            AudioManager.I.PlayUIButtonClick();

        currentSelection = HouseSelection.Upgrade;

        if (currentHouse == null)
        {
            if (itemName != null) itemName.text = "Upgrade";
            if (itemDesc != null) itemDesc.text = "No house selected.";
            if (itemCost != null) itemCost.text = "";
            return;
        }

        if (!currentHouse.CanUpgrade)
        {
            if (itemName != null) itemName.text = "Upgrade";
            if (itemDesc != null) itemDesc.text = "This house is already at max tier!";
            if (itemCost != null) itemCost.text = "";
            return;
        }

        if (itemName != null) itemName.text = "Upgrade";
        if (itemDesc != null) itemDesc.text = currentHouse.GetUpgradeDescription();
        if (itemCost != null) itemCost.text = $"{currentHouse.UpgradeCost} Ice";
    }

    private void OnAddPenguinClicked(ClickEvent evt)
    {
        // Play UI button click sound
        if (AudioManager.I != null)
            AudioManager.I.PlayUIButtonClick();

        currentSelection = HouseSelection.AddPenguin;

        if (currentHouse == null)
        {
            if (itemName != null) itemName.text = "+1 Penguin";
            if (itemDesc != null) itemDesc.text = "No house selected.";
            if (itemCost != null) itemCost.text = "";
            return;
        }

        if (itemName != null) itemName.text = "+1 Penguin";

        // Check if house is at max capacity
        if (!currentHouse.CanCreatePenguin)
        {
            // At max tier and max penguins
            if (!currentHouse.CanUpgrade)
            {
                if (itemDesc != null) itemDesc.text = $"This house has reached the maximum of {currentHouse.MaxPenguins} penguins!";
                if (itemCost != null) itemCost.text = "";
            }
            // Can upgrade to get more penguin slots
            else
            {
                if (itemDesc != null) itemDesc.text = "This house is full! Upgrade the house to create more penguins.";
                if (itemCost != null) itemCost.text = "";
            }
            return;
        }

        // Can create penguin
        if (itemDesc != null) itemDesc.text = $"Spawn a new penguin at this house. ({currentHouse.PenguinsCreated}/{currentHouse.MaxPenguins} created)";
        if (itemCost != null) itemCost.text = $"{currentHouse.penguinFishCost} Fish, {currentHouse.penguinPebbleCost} Pebble";
    }

    private void OnBuildClicked(ClickEvent evt)
    {
        // Play UI button click sound
        if (AudioManager.I != null)
            AudioManager.I.PlayUIButtonClick();

        if (currentSelection == HouseSelection.None)
            return;

        bool success = false;

        if (currentSelection == HouseSelection.Upgrade)
            success = TryUpgradeHouse();
        else if (currentSelection == HouseSelection.AddPenguin)
            success = TryAddPenguin();

        if (success)
        {
            menuController?.Close();
            ShowDefaultInfo();
        }
    }

    private bool TryUpgradeHouse()
    {
        if (currentHouse == null)
        {
            Debug.LogWarning("HouseMenuUI: No target house set.");
            return false;
        }

        if (!currentHouse.CanUpgrade)
        {
            Debug.Log("HouseMenuUI: House is already at max tier.");
            return false;
        }

        int cost = currentHouse.UpgradeCost;

        if (GameManager.I == null || GameManager.I.ice < cost)
        {
            Debug.Log($"HouseMenuUI: Not enough ice. Need {cost}, have {GameManager.I?.ice ?? 0}");
            return false;
        }

        if (currentHouse.TryUpgrade())
        {
            Debug.Log($"HouseMenuUI: House upgraded to tier {currentHouse.CurrentTier}!");
            return true;
        }

        return false;
    }

    private bool TryAddPenguin()
    {
        if (currentHouse == null)
        {
            Debug.LogWarning("HouseMenuUI: No target house set.");
            return false;
        }

        // Check if house can create more penguins
        if (!currentHouse.CanCreatePenguin)
        {
            Debug.Log($"HouseMenuUI: House is at max capacity ({currentHouse.PenguinsCreated}/{currentHouse.MaxPenguins}).");
            return false;
        }

        if (penguinPrefab == null)
        {
            Debug.LogError("HouseMenuUI: Penguin prefab not assigned!");
            return false;
        }

        int fishCost = currentHouse.penguinFishCost;
        int pebbleCost = currentHouse.penguinPebbleCost;

        if (GameManager.I == null)
        {
            Debug.LogWarning("HouseMenuUI: GameManager not found.");
            return false;
        }

        if (GameManager.I.food < fishCost || GameManager.I.pebbles < pebbleCost)
        {
            Debug.Log($"HouseMenuUI: Not enough resources. Need {fishCost} fish and {pebbleCost} pebbles. Have {GameManager.I.food} fish and {GameManager.I.pebbles} pebbles.");
            return false;
        }

        GameManager.I.food -= fishCost;
        GameManager.I.pebbles -= pebbleCost;

        // Spawn 1 unit lower on Y (additional offset beyond spawnOffset)
        Vector3 spawnPos = currentHouseObject.transform.position + (Vector3)spawnOffset + new Vector3(0f, -1f, 0f);
        GameObject newPenguin = Instantiate(penguinPrefab, spawnPos, Quaternion.identity);

        if (PenguinManager.I != null)
        {
            var jobs = newPenguin.GetComponent<PenguinJobs>();
            if (jobs != null)
                PenguinManager.I.InitializePenguin(jobs);
        }

        // Immediately move penguin to open area to avoid spawning on top of buildings
        var mover = newPenguin.GetComponent<PenguinMover>();
        var anim = newPenguin.GetComponent<PenguinAnimator>();
        var ySorter = newPenguin.GetComponent<YSorter>();

        if (mover != null)
        {
            // Find open position (now stays in camera bounds)
            Vector2 openPos = FindOpenSpawnPosition(spawnPos);

            // Set up animation before movement
            if (anim != null)
            {
                anim.SetWalking();  // Start walk animation
                anim.FaceToward(openPos, spawnPos);  // Face direction of movement
            }

            // Force penguin to render behind all buildings during spawn movement
            if (ySorter != null)
            {
                ySorter.sortingOrderOffset = -10000;
            }

            // Ignore collisions during spawn movement
            mover.SetIgnoreCollisions(true);

            // Capture house Y position to check if penguin has moved clear
            float houseY = currentHouseObject.transform.position.y;

            // Command penguin to move there
            mover.MoveTo(openPos, () => {
                // Stop walking animation
                if (anim != null)
                    anim.SetIdle();

                // Re-enable collisions
                mover.SetIgnoreCollisions(false);

                // Only reset sorting offset if penguin has moved well below the house
                // This prevents penguin from appearing on top of buildings it's near
                if (ySorter != null)
                {
                    float penguinY = newPenguin.transform.position.y;
                    float yDifference = houseY - penguinY;

                    // Only reset if penguin is at least 1 unit below the house
                    if (yDifference >= 1.0f)
                    {
                        ySorter.sortingOrderOffset = 0; // Safe to use normal Y-sorting
                    }
                    else
                    {
                        // Keep rendering behind buildings, will auto-correct once penguin moves
                        StartCoroutine(DelayedSortingReset(ySorter, newPenguin.transform, houseY));
                    }
                }
            });
        }

        // Increment the penguin counter for this house
        currentHouse.IncrementPenguinCount();

        // Increment global penguin count
        if (GameManager.I != null)
            GameManager.I.AddPenguin();

        Debug.Log($"HouseMenuUI: Penguin spawned at {spawnPos}! ({currentHouse.PenguinsCreated}/{currentHouse.MaxPenguins})");
        return true;
    }

    private System.Collections.IEnumerator DelayedSortingReset(YSorter ySorter, Transform penguin, float houseY)
    {
        // Wait and check periodically if penguin has moved clear
        for (int i = 0; i < 20; i++) // Check for up to 2 seconds
        {
            yield return new WaitForSeconds(0.1f);

            if (ySorter == null || penguin == null) yield break;

            float penguinY = penguin.position.y;
            float yDifference = houseY - penguinY;

            // Reset once penguin is 1 unit below house OR has moved significantly away
            if (yDifference >= 1.0f)
            {
                ySorter.sortingOrderOffset = 0;
                yield break;
            }
        }

        // After timeout, reset anyway
        if (ySorter != null)
            ySorter.sortingOrderOffset = 0;
    }

    private Vector2 FindOpenSpawnPosition(Vector3 startPos)
    {
        // Get camera bounds
        Camera cam = Camera.main;
        if (cam == null) return (Vector2)startPos + Vector2.down * 2f;

        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;
        Vector2 camCenter = cam.transform.position;

        float margin = 0.5f; // Keep penguins away from screen edge
        float minX = camCenter.x - (camWidth / 2f) + margin;
        float maxX = camCenter.x + (camWidth / 2f) - margin;
        float minY = camCenter.y - (camHeight / 2f) + margin;
        float maxY = camCenter.y + (camHeight / 2f) - margin;

        // Search in expanding rings for open area
        int buildingsLayer = LayerMask.GetMask("Buildings");
        float searchRadius = 1.5f;
        int numRings = 3;
        int pointsPerRing = 12;

        for (int ring = 1; ring <= numRings; ring++)
        {
            float radius = searchRadius * ring;
            for (int i = 0; i < pointsPerRing; i++)
            {
                float angle = (i / (float)pointsPerRing) * 360f * Mathf.Deg2Rad;
                Vector2 testPos = (Vector2)startPos + new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius
                );

                // Clamp to camera bounds
                testPos.x = Mathf.Clamp(testPos.x, minX, maxX);
                testPos.y = Mathf.Clamp(testPos.y, minY, maxY);

                // Check if position is free of buildings
                if (!Physics2D.OverlapCircle(testPos, 0.3f, buildingsLayer))
                    return testPos;
            }
        }

        // Fallback: clamp spawn position to camera bounds
        Vector2 fallback = (Vector2)startPos + Vector2.down * 2f;
        fallback.x = Mathf.Clamp(fallback.x, minX, maxX);
        fallback.y = Mathf.Clamp(fallback.y, minY, maxY);
        return fallback;
    }
}
