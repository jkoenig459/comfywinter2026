using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class BuildMenuController : MonoBehaviour
{
    public static BuildMenuController I { get; private set; }

    [Header("UI")]
    public UIDocument uiDocument;

    [Header("Slide Animation")]
    [Tooltip("How long the slide takes (seconds).")]
    public float slideDuration = 0.18f;

    [Tooltip("How far above its normal position the menu starts (pixels). Negative = above.")]
    public float hiddenMarginTop = -420f;

    // UI references
    private VisualElement root;
    private VisualElement menuRoot;

    private Label itemName;
    private Label itemDesc;
    private Label itemCost;

    private Button houseBtn;
    private Button buildBtn;

    private bool isOpen = false;
    private bool animating = false;

    // Current selection
    private BuildSelection currentSelection = BuildSelection.None;

    private enum BuildSelection
    {
        None,
        House
    }

    private const string DEFAULT_DESC = "Click an item on the left for more information";

    private void Awake()
    {
        I = this;

        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        root = uiDocument.rootVisualElement;

        // This MUST match your UXML element name
        menuRoot = root.Q<VisualElement>("MenuRoot");
        if (menuRoot == null)
        {
            Debug.LogError("BuildMenuController: Missing VisualElement named 'MenuRoot'.");
            return;
        }

        itemName = root.Q<Label>("ItemName");
        itemDesc = root.Q<Label>("ItemDesc");
        itemCost = root.Q<Label>("ItemCost");

        houseBtn = root.Q<Button>("HouseBtn");
        buildBtn = root.Q<Button>("BuildBtn");


        if (houseBtn != null) houseBtn.clicked += SelectHouse;
        if (buildBtn != null) buildBtn.clicked += OnBuildClicked;

        // Default text
        ShowDefaultInfo();

        root.style.display = DisplayStyle.Flex;

        // UI Hidden State
        menuRoot.pickingMode = PickingMode.Ignore;
        ApplyMarginTransition(slideDuration);
        SetMenuHiddenImmediate();
    }

    public void Toggle()
    {
        if (animating) return;

        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (menuRoot == null || animating) return;

        ShowDefaultInfo();

        animating = true;
        menuRoot.pickingMode = PickingMode.Ignore;

        ApplyMarginTransition(slideDuration);

        SetMenuHiddenImmediate();

        menuRoot.schedule.Execute(() =>
        {
            menuRoot.style.marginTop = 0f;

            menuRoot.schedule.Execute(() =>
            {
                animating = false;
                isOpen = true;
                menuRoot.pickingMode = PickingMode.Position;
            }).ExecuteLater((int)(slideDuration * 1000f) + 20);

        }).ExecuteLater(1);
    }

    public void Close()
    {
        if (menuRoot == null || animating) return;

        animating = true;
        menuRoot.pickingMode = PickingMode.Ignore;

        ApplyMarginTransition(slideDuration);

        // Slide up
        menuRoot.style.marginTop = hiddenMarginTop;

        menuRoot.schedule.Execute(() =>
        {
            animating = false;
            isOpen = false;
        }).ExecuteLater((int)(slideDuration * 1000f) + 20);
    }

    private void SetMenuHiddenImmediate()
    {
        menuRoot.style.marginTop = hiddenMarginTop;
    }

    private void ApplyMarginTransition(float seconds)
    {
        menuRoot.style.transitionProperty = new List<StylePropertyName>
        {
            new StylePropertyName("margin-top")
        };

        menuRoot.style.transitionDuration = new List<TimeValue>
        {
            new TimeValue(seconds, TimeUnit.Second)
        };

        menuRoot.style.transitionTimingFunction = new List<EasingFunction>
        {
            EasingMode.EaseOut
        };
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

    private void OnBuildClicked()
    {
        if (currentSelection == BuildSelection.None)
            return;

        BuildModePlacer.I?.BeginPlacingHouse();

        Close();
        ShowDefaultInfo();
    }
}
