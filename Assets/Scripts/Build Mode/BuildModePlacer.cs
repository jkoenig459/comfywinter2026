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

    [Header("Placement Restrictions")]
    [Tooltip("Minimum distance above nodes where houses cannot be placed")]
    public float nodeAboveBuffer = 2f;
    [Tooltip("Minimum distance below the HQ where houses cannot be placed")]
    public float hqBelowBuffer = 2f;

    [Header("Ghost Rendering")]
    [Tooltip("Sorting layer name to force on the ghost.")]
    public string ghostSortingLayerName = "";
    [Tooltip("Sorting order to force on the ghost")]
    public int ghostSortingOrder = 1000;

    private Camera cam;
    private ResourceNode[] cachedNodes;

    private GameObject selectedPrefab;
    private GameObject selectedGhostPrefab;
    private int selectedCost;

    private GameObject ghost;
    private Collider2D ghostCollider;

    public bool IsPlacing => ghost != null;

    private void Awake()
    {
        I = this;
        cam = Camera.main;
        RefreshNodeCache();
    }

    private void RefreshNodeCache()
    {
        cachedNodes = FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
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

        // Refresh node cache in case nodes were added/removed
        RefreshNodeCache();

        ghost = Instantiate(selectedGhostPrefab);
        ghost.name = "BuildGhost";

        ForceGhostRenderOnTop(ghost);

        ghostCollider = ghost.GetComponent<Collider2D>();
        if (ghostCollider == null)
            ghostCollider = ghost.GetComponentInChildren<Collider2D>();
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

        // Check if any part of ghost is outside camera view
        if (!IsFullyInCameraView())
            return false;

        // Check if placement is too close above any node
        if (IsAboveNode())
            return false;

        // Check if placement is too close below the HQ
        if (IsBelowHQ())
            return false;

        bool wasEnabled = ghostCollider.enabled;
        if (!wasEnabled) ghostCollider.enabled = true;

        Collider2D[] hits;

        // Use appropriate overlap check based on collider type
        if (ghostCollider is BoxCollider2D boxCollider)
        {
            var center = (Vector2)boxCollider.bounds.center;
            var size = (Vector2)boxCollider.bounds.size;
            hits = Physics2D.OverlapBoxAll(center, size, 0f, blockingLayers);
        }
        else if (ghostCollider is CircleCollider2D circleCollider)
        {
            var center = (Vector2)circleCollider.bounds.center;
            var radius = circleCollider.radius;
            hits = Physics2D.OverlapCircleAll(center, radius, blockingLayers);
        }
        else
        {
            // Fallback for other collider types
            var center = (Vector2)ghostCollider.bounds.center;
            var size = (Vector2)ghostCollider.bounds.size;
            hits = Physics2D.OverlapBoxAll(center, size, 0f, blockingLayers);
        }

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

    private bool IsFullyInCameraView()
    {
        if (cam == null || ghostCollider == null) return true;

        Bounds bounds = ghostCollider.bounds;

        // Check all four corners of the bounds
        Vector3[] corners = new Vector3[4];
        corners[0] = new Vector3(bounds.min.x, bounds.min.y, 0); // bottom-left
        corners[1] = new Vector3(bounds.max.x, bounds.min.y, 0); // bottom-right
        corners[2] = new Vector3(bounds.min.x, bounds.max.y, 0); // top-left
        corners[3] = new Vector3(bounds.max.x, bounds.max.y, 0); // top-right

        foreach (var corner in corners)
        {
            Vector3 viewportPoint = cam.WorldToViewportPoint(corner);

            // Viewport coordinates: (0,0) is bottom-left, (1,1) is top-right
            if (viewportPoint.x < 0f || viewportPoint.x > 1f ||
                viewportPoint.y < 0f || viewportPoint.y > 1f)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsAboveNode()
    {
        if (cachedNodes == null || ghostCollider == null) return false;

        Vector2 ghostPos = ghost.transform.position;
        Bounds bounds = ghostCollider.bounds;

        foreach (var node in cachedNodes)
        {
            if (node == null) continue;

            Vector2 nodePos = node.transform.position;

            // Check if ghost is above the node (within nodeAboveBuffer units)
            // Ghost Y should be greater than node Y (above it)
            // And within the buffer distance
            float yDiff = ghostPos.y - nodePos.y;

            if (yDiff > 0 && yDiff <= nodeAboveBuffer)
            {
                // Check if there's horizontal overlap
                // Use a reasonable horizontal range based on the ghost bounds
                float halfWidth = bounds.extents.x + 0.5f; // add some margin
                float xDiff = Mathf.Abs(ghostPos.x - nodePos.x);

                if (xDiff < halfWidth)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsBelowHQ()
    {
        if (IglooHQ.Instance == null || ghostCollider == null) return false;

        Vector2 ghostPos = ghost.transform.position;
        Vector2 hqPos = IglooHQ.Instance.transform.position;
        Bounds bounds = ghostCollider.bounds;

        // Check if ghost is below the HQ (within hqBelowBuffer units)
        // Ghost Y should be less than HQ Y (below it)
        // And within the buffer distance
        float yDiff = hqPos.y - ghostPos.y;

        if (yDiff > 0 && yDiff <= hqBelowBuffer)
        {
            // Check if there's horizontal overlap
            // Use a reasonable horizontal range based on the ghost bounds
            float halfWidth = bounds.extents.x + 0.5f; // add some margin
            float xDiff = Mathf.Abs(ghostPos.x - hqPos.x);

            if (xDiff < halfWidth)
            {
                return true;
            }
        }

        return false;
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