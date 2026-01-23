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

        // Increment the penguin counter for this house
        currentHouse.IncrementPenguinCount();

        Debug.Log($"HouseMenuUI: Penguin spawned at {spawnPos}! ({currentHouse.PenguinsCreated}/{currentHouse.MaxPenguins})");
        return true;
    }
}
