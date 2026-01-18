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
    [SerializeField] private Vector2 entranceOffset = new Vector2(0f, -1f);

    private VisualElement root;

    private Label itemName;
    private Label itemDesc;
    private Label itemCost;

    private Button upgradeBtn;
    private Button addPenguinBtn;
    private Button confirmBtn;

    private GameObject currentHouse;

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
        currentHouse = house;
    }

    private void Bind()
    {
        if (uiDocument == null)
        {
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
        confirmBtn = root.Q<Button>("BuildBtn");

        if (upgradeBtn != null) upgradeBtn.RegisterCallback<ClickEvent>(OnUpgradeClicked);
        if (addPenguinBtn != null) addPenguinBtn.RegisterCallback<ClickEvent>(OnAddPenguinClicked);
        if (confirmBtn != null) confirmBtn.RegisterCallback<ClickEvent>(OnConfirmClicked);
    }

    private void Unbind()
    {
        if (upgradeBtn != null) upgradeBtn.UnregisterCallback<ClickEvent>(OnUpgradeClicked);
        if (addPenguinBtn != null) addPenguinBtn.UnregisterCallback<ClickEvent>(OnAddPenguinClicked);
        if (confirmBtn != null) confirmBtn.UnregisterCallback<ClickEvent>(OnConfirmClicked);
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

        if (itemName != null) itemName.text = "Upgrade";
        if (itemDesc != null) itemDesc.text = "Upgrade this house to improve it.";
        if (itemCost != null) itemCost.text = "10 Ice, 3 Pebble";
    }

    private void OnAddPenguinClicked(ClickEvent evt)
    {
        currentSelection = HouseSelection.AddPenguin;

        if (itemName != null) itemName.text = "+1 Penguin";
        if (itemDesc != null) itemDesc.text = "Increase capacity by 1 Penguin.";
        if (itemCost != null) itemCost.text = "5 Ice, 2 Pebble";
    }

    private void OnConfirmClicked(ClickEvent evt)
    {
        if (currentSelection == HouseSelection.None)
            return;

        switch (currentSelection)
        {
            case HouseSelection.Upgrade:
                break;
            case HouseSelection.AddPenguin:
                SpawnPenguin();
                break;
        }

        menuController?.Close();
        ShowDefaultInfo();
    }

    private void SpawnPenguin()
    {
        if (currentHouse == null)
        {
            Debug.LogWarning("HouseMenuUI: Cannot spawn penguin, no house selected.");
            return;
        }

        if (penguinPrefab == null)
        {
            Debug.LogWarning("HouseMenuUI: Cannot spawn penguin, penguinPrefab not assigned.");
            return;
        }

        Vector3 spawnPosition = currentHouse.transform.position + (Vector3)entranceOffset;
        GameObject spawnedPenguin = Instantiate(penguinPrefab, spawnPosition, Quaternion.identity);

        if (PenguinManager.I != null)
        {
            PenguinJobs jobs = spawnedPenguin.GetComponent<PenguinJobs>();
            if (jobs != null)
            {
                PenguinManager.I.InitializePenguin(jobs);
            }
        }
    }
}