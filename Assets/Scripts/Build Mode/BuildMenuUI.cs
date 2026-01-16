using UnityEngine;
using UnityEngine.UIElements;

public class BuildMenuUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private MenuController menuController;

    private VisualElement root;

    private Label itemName;
    private Label itemDesc;
    private Label itemCost;

    private Button houseBtn;
    private Button storageBtn;
    private Button researchBtn;
    private Button confirmBtn;

    private BuildSelection currentSelection = BuildSelection.None;

    private enum BuildSelection
    {
        None,
        House,
        Storage,
        Research
    }

    private const string DEFAULT_DESC = "Click an item on the left for more information";

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (menuController == null)
            menuController = GetComponent<MenuController>();

        if (uiDocument == null)
        {
            enabled = false;
            return;
        }

        root = uiDocument.rootVisualElement;

        itemName = root.Q<Label>("ItemName");
        itemDesc = root.Q<Label>("ItemDesc");
        itemCost = root.Q<Label>("ItemCost");

        houseBtn = root.Q<Button>("HouseBtn");
        storageBtn = root.Q<Button>("StorageBtn");
        researchBtn = root.Q<Button>("ReasearchBtn");
        confirmBtn = root.Q<Button>("BuildBtn");

        if (houseBtn != null) houseBtn.clicked += SelectHouse;
        if (storageBtn != null) storageBtn.clicked += SelectStorage;
        if (researchBtn != null) researchBtn.clicked += SelectResearch;
        if (confirmBtn != null) confirmBtn.clicked += OnConfirmClicked;

        ShowDefaultInfo();
    }

    private void ShowDefaultInfo()
    {
        currentSelection = BuildSelection.None;

        if (itemName != null) itemName.text = "";
        if (itemCost != null) itemCost.text = "";
        if (itemDesc != null) itemDesc.text = DEFAULT_DESC;
    }

    private void SelectHouse()
    {
        currentSelection = BuildSelection.House;

        if (itemName != null) itemName.text = "House";
        if (itemDesc != null) itemDesc.text = "Basic housing. Enough for 2 Penguins";
        if (itemCost != null) itemCost.text = "5 Ice";
    }

    private void SelectStorage()
    {
        currentSelection = BuildSelection.Storage;

        if (itemName != null) itemName.text = "Storage";
        if (itemDesc != null) itemDesc.text = "Store resources to prevent decay";
        if (itemCost != null) itemCost.text = "3 Ice, 2 Snow";
    }

    private void SelectResearch()
    {
        currentSelection = BuildSelection.Research;

        if (itemName != null) itemName.text = "Upgrade HQ";
        if (itemDesc != null) itemDesc.text = "Unlock new technologies and buildings";
        if (itemCost != null) itemCost.text = "10 Ice, 5 Snow";
    }

    private void OnConfirmClicked()
    {
        if (currentSelection == BuildSelection.None)
            return;

        switch (currentSelection)
        {
            case BuildSelection.House:
                BuildModePlacer.I?.BeginPlacingHouse();
                break;
            case BuildSelection.Storage:
                break;
            case BuildSelection.Research:
                break;
        }

        menuController?.Close();
        ShowDefaultInfo();
    }
}