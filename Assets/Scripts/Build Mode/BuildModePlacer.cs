using UnityEngine;

public class BuildModePlacer : MonoBehaviour
{
    public static BuildModePlacer I { get; private set; }

    [Header("House")]
    public GameObject housePrefab;
    public GameObject houseGhostPrefab;
    public int houseIceCost = 5;

    [Header("Placement")]
    public LayerMask blockingLayers;
    public float gridSnap = 1f;

    [Header("Ghost Rendering")]
    [Tooltip("Sorting layer name to force on the ghost.")]
    public string ghostSortingLayerName = "";
    [Tooltip("Sorting order to force on the ghost")]
    public int ghostSortingOrder = 1000;

    private Camera cam;

    private GameObject selectedPrefab;
    private GameObject selectedGhostPrefab;
    private int selectedCost;

    private GameObject ghost;
    private BoxCollider2D ghostCollider;

    public bool IsPlacing => ghost != null;

    private void Awake()
    {
        I = this;
        cam = Camera.main;
    }

    private void Update()
    {
        if (ghost == null) return;

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            Cancel();
            return;
        }

        UpdateGhostPosition();

        if (Input.GetMouseButtonDown(0))
            TryPlace();
    }

    public void BeginPlacingHouse()
    {
        selectedPrefab = housePrefab;
        selectedGhostPrefab = houseGhostPrefab;
        selectedCost = houseIceCost;

        StartGhost();
    }

    private void StartGhost()
    {
        Cancel();

        if (selectedGhostPrefab == null || selectedPrefab == null)
        {
            Debug.LogWarning("BuildModePlacer: Missing prefab/ghost prefab assignment.");
            return;
        }

        ghost = Instantiate(selectedGhostPrefab);
        ghost.name = "BuildGhost";

        ForceGhostRenderOnTop(ghost);

        ghostCollider = ghost.GetComponent<BoxCollider2D>();
        if (ghostCollider == null)
            ghostCollider = ghost.GetComponentInChildren<BoxCollider2D>();
    }

    private void ForceGhostRenderOnTop(GameObject ghostObj)
    {
        if (ghostObj == null) return;

        var srs = ghostObj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
        {
            if (!string.IsNullOrEmpty(ghostSortingLayerName))
                sr.sortingLayerName = ghostSortingLayerName;

            sr.sortingOrder = ghostSortingOrder;
        }
    }

    private void Cancel()
    {
        if (ghost != null)
            Destroy(ghost);

        ghost = null;
        ghostCollider = null;
    }

    private void UpdateGhostPosition()
    {
        Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);

        Vector2 snapped = new Vector2(
            Mathf.Round(mouseWorld.x / gridSnap) * gridSnap,
            Mathf.Round(mouseWorld.y / gridSnap) * gridSnap
        );

        ghost.transform.position = snapped;

        bool canPlace = CanPlaceHere();
        bool hasIce = GameManager.I != null && GameManager.I.ice >= selectedCost;

        UpdateGhostVisual(canPlace, hasIce);
    }

    private bool CanPlaceHere()
    {
        if (ghostCollider == null) return false;

        bool wasEnabled = ghostCollider.enabled;
        if (!wasEnabled) ghostCollider.enabled = true;

        var center = (Vector2)ghostCollider.bounds.center;
        var size = (Vector2)ghostCollider.bounds.size;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, blockingLayers);

        if (!wasEnabled) ghostCollider.enabled = false;

        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit == null) continue;

            if (ghost != null && hit.transform.IsChildOf(ghost.transform))
                continue;

            if (hit.isTrigger)
                continue;

            return false;
        }

        return true;
    }

    private void UpdateGhostVisual(bool canPlace, bool hasIce)
    {
        if (ghost == null) return;

        var srs = ghost.GetComponentsInChildren<SpriteRenderer>(true);
        if (srs == null || srs.Length == 0) return;

        bool valid = canPlace && hasIce;
        Color c = valid ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f);

        foreach (var sr in srs)
            sr.color = c;
    }

    private void TryPlace()
    {
        if (!CanPlaceHere()) return;
        if (GameManager.I == null || GameManager.I.ice < selectedCost) return;

        Instantiate(selectedPrefab, ghost.transform.position, Quaternion.identity);
        GameManager.I.ice -= selectedCost;

        if (AudioManager.I != null)
            AudioManager.I.PlayPlaceIgloo();

        Cancel();
    }
}