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
        buildBtn = root.Q<Button>("BuildBtn");

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

        if (!currentHouse.CanCreatePenguin)
        {
            if (!currentHouse.CanUpgrade)
            {
                if (itemDesc != null) itemDesc.text = $"This house has reached the maximum of {currentHouse.MaxPenguins} penguins!";
                if (itemCost != null) itemCost.text = "";
            }
            else
            {
                if (itemDesc != null) itemDesc.text = "This house is full! Upgrade the house to create more penguins.";
                if (itemCost != null) itemCost.text = "";
            }
            return;
        }

        if (itemDesc != null) itemDesc.text = $"Spawn a new penguin at this house. ({currentHouse.PenguinsCreated}/{currentHouse.MaxPenguins} created)";
        if (itemCost != null) itemCost.text = $"{currentHouse.penguinFishCost} Fish, {currentHouse.penguinPebbleCost} Pebble";
    }

    private void OnBuildClicked(ClickEvent evt)
    {
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
            return false;
        }

        if (!currentHouse.CanUpgrade)
        {
            return false;
        }

        int cost = currentHouse.UpgradeCost;

        if (GameManager.I == null || GameManager.I.ice < cost)
        {
            return false;
        }

        if (currentHouse.TryUpgrade())
        {
            return true;
        }

        return false;
    }

    private bool TryAddPenguin()
    {
        if (currentHouse == null)
        {
            return false;
        }

        if (!currentHouse.CanCreatePenguin)
        {
            return false;
        }

        if (penguinPrefab == null)
        {
            return false;
        }

        int fishCost = currentHouse.penguinFishCost;
        int pebbleCost = currentHouse.penguinPebbleCost;

        if (GameManager.I == null)
        {
            return false;
        }

        if (GameManager.I.food < fishCost || GameManager.I.pebbles < pebbleCost)
        {
            return false;
        }

        GameManager.I.food -= fishCost;
        GameManager.I.pebbles -= pebbleCost;

        Vector3 spawnPos = currentHouseObject.transform.position + (Vector3)spawnOffset + new Vector3(0f, -1f, 0f);
        GameObject newPenguin = Instantiate(penguinPrefab, spawnPos, Quaternion.identity);

        if (PenguinManager.I != null)
        {
            var jobs = newPenguin.GetComponent<PenguinJobs>();
            if (jobs != null)
                PenguinManager.I.InitializePenguin(jobs);
        }

        var mover = newPenguin.GetComponent<PenguinMover>();
        var anim = newPenguin.GetComponent<PenguinAnimator>();
        var ySorter = newPenguin.GetComponent<YSorter>();

        if (mover != null)
        {
            Vector2 openPos = FindOpenSpawnPosition(spawnPos);

            if (anim != null)
            {
                anim.SetWalking();
                anim.FaceToward(openPos, spawnPos);
            }

            if (ySorter != null)
            {
                ySorter.sortingOrderOffset = -10000;
            }

            mover.SetIgnoreCollisions(true);

            float houseY = currentHouseObject.transform.position.y;

            mover.MoveTo(openPos, () => {
                if (anim != null)
                    anim.SetIdle();

                mover.SetIgnoreCollisions(false);

                if (ySorter != null)
                {
                    float penguinY = newPenguin.transform.position.y;
                    float yDifference = houseY - penguinY;

                    if (yDifference >= 1.0f)
                    {
                        ySorter.sortingOrderOffset = 0;
                    }
                    else
                    {
                        StartCoroutine(DelayedSortingReset(ySorter, newPenguin.transform, houseY));
                    }
                }
            });
        }

        currentHouse.IncrementPenguinCount();

        if (GameManager.I != null)
            GameManager.I.AddPenguin();

        return true;
    }

    private System.Collections.IEnumerator DelayedSortingReset(YSorter ySorter, Transform penguin, float houseY)
    {
        for (int i = 0; i < 20; i++)
        {
            yield return new WaitForSeconds(0.1f);

            if (ySorter == null || penguin == null) yield break;

            float penguinY = penguin.position.y;
            float yDifference = houseY - penguinY;

            if (yDifference >= 1.0f)
            {
                ySorter.sortingOrderOffset = 0;
                yield break;
            }
        }

        if (ySorter != null)
            ySorter.sortingOrderOffset = 0;
    }

    private Vector2 FindOpenSpawnPosition(Vector3 startPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return (Vector2)startPos + Vector2.down * 2f;

        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;
        Vector2 camCenter = cam.transform.position;

        float margin = 0.5f;
        float minX = camCenter.x - (camWidth / 2f) + margin;
        float maxX = camCenter.x + (camWidth / 2f) - margin;
        float minY = camCenter.y - (camHeight / 2f) + margin;
        float maxY = camCenter.y + (camHeight / 2f) - margin;

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

                testPos.x = Mathf.Clamp(testPos.x, minX, maxX);
                testPos.y = Mathf.Clamp(testPos.y, minY, maxY);

                if (!Physics2D.OverlapCircle(testPos, 0.3f, buildingsLayer))
                    return testPos;
            }
        }

        Vector2 fallback = (Vector2)startPos + Vector2.down * 2f;
        fallback.x = Mathf.Clamp(fallback.x, minX, maxX);
        fallback.y = Mathf.Clamp(fallback.y, minY, maxY);
        return fallback;
    }
}
