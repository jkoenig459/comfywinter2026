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
    private Collider2D ghostCollider;

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

        bool wasEnabled = ghostCollider.enabled;
        if (!wasEnabled) ghostCollider.enabled = true;

        // First check specifically for houses (Buildings layer, even if trigger)
        int buildingsLayer = LayerMask.GetMask("Buildings");
        Collider2D[] buildingHits;

        if (ghostCollider is BoxCollider2D boxCol)
        {
            var center = (Vector2)boxCol.bounds.center;
            var size = (Vector2)boxCol.bounds.size;
            buildingHits = Physics2D.OverlapBoxAll(center, size, 0f, buildingsLayer);
        }
        else if (ghostCollider is CircleCollider2D circleCol)
        {
            var center = (Vector2)circleCol.bounds.center;
            var radius = circleCol.radius;
            buildingHits = Physics2D.OverlapCircleAll(center, radius, buildingsLayer);
        }
        else
        {
            // Fallback for other collider types
            var center = (Vector2)ghostCollider.bounds.center;
            var size = (Vector2)ghostCollider.bounds.size;
            buildingHits = Physics2D.OverlapBoxAll(center, size, 0f, buildingsLayer);
        }

        foreach (var hit in buildingHits)
        {
            // Skip if it's the ghost itself or child of ghost
            if (hit.transform.IsChildOf(ghost.transform) || hit.transform == ghost.transform)
                continue;

            // Found a building collider - placement invalid
            if (!wasEnabled) ghostCollider.enabled = false;
            return false;
        }

        // Then check blocking layers for non-trigger colliders
        Collider2D[] hits;

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
                continue; // Keep existing logic for other triggers

            return false;
        }

        // Check if placement would block resource access
        if (WouldBlockResourceAccess(ghost.transform.position))
            return false;

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

    private bool WouldBlockResourceAccess(Vector2 position)
    {
        // Find all resource nodes and piles in scene
        ResourceNode[] nodes = FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
        ResourcePile[] piles = FindObjectsByType<ResourcePile>(FindObjectsSortMode.None);

        // Get house collider bounds for temporary simulation
        float houseRadius = 0.5f; // Approximate house collision radius
        if (ghostCollider is CircleCollider2D circleCol)
            houseRadius = circleCol.radius;

        float penguinClearance = 0.5f; // Space needed for penguin to work

        // Check each resource node
        foreach (var node in nodes)
        {
            if (node == null) continue;

            Vector2 nodePos = node.transform.position;

            // First check: don't allow houses too close to the node itself
            float nodeMinDistance = 1.8f; // Minimum distance from node center
            if (Vector2.Distance(position, nodePos) < nodeMinDistance)
            {
                return true; // Too close to resource node
            }

            // Additional check: prevent houses from being placed directly above nodes
            // This is critical for ice nodes where piles spawn
            float xDistance = Mathf.Abs(position.x - nodePos.x);
            float yDistance = position.y - nodePos.y; // Positive if house is above node

            // If house is within 1 unit horizontally and above the node, block it
            if (xDistance < 1.0f && yDistance > -0.5f && yDistance < 1.5f)
            {
                return true; // Blocking area above/around node
            }

            // Check for child worker position transforms (used by ice nodes)
            if (node.leftWorker != null)
            {
                Vector2 leftPos = node.leftWorker.position;
                if (Vector2.Distance(position, leftPos) < houseRadius + penguinClearance)
                    return true;
            }

            if (node.rightWorker != null)
            {
                Vector2 rightPos = node.rightWorker.position;
                if (Vector2.Distance(position, rightPos) < houseRadius + penguinClearance)
                    return true;
            }

            // If no child transforms, check based on resource type offsets
            if (node.leftWorker == null || node.rightWorker == null)
            {
                Vector2[] standPositions;

                if (node.type == ResourceType.Ice)
                {
                    // Ice nodes use different offsets (0.30f, -0.05f)
                    standPositions = new Vector2[]
                    {
                        nodePos + new Vector2(0.30f, -0.05f),   // Right side
                        nodePos + new Vector2(-0.30f, -0.05f)   // Left side
                    };
                }
                else // Fish and other resources
                {
                    // Default fishing offset (0.35f, -0.12f)
                    standPositions = new Vector2[]
                    {
                        nodePos + new Vector2(0.35f, -0.12f),   // Right side
                        nodePos + new Vector2(-0.35f, -0.12f)   // Left side
                    };
                }

                foreach (var standPos in standPositions)
                {
                    if (Vector2.Distance(position, standPos) < houseRadius + penguinClearance)
                        return true; // Would block worker position
                }
            }

            // Check pile positions (created at node + pile offset)
            // Use larger clearance for pile to ensure penguins can access it
            Vector2 pileOffset = new Vector2(0.15f, -0.20f);
            Vector2 pilePos = nodePos + pileOffset;
            float pileClearance = 1.2f; // Larger clearance for pile access
            if (Vector2.Distance(position, pilePos) < houseRadius + pileClearance)
            {
                return true; // Would block pile
            }

            // Check if house blocks direct path from storage to resource node
            if (Storage.I != null)
            {
                Vector2 storagePos = Storage.I.transform.position;
                if (IsPointNearLine(position, storagePos, nodePos, houseRadius))
                {
                    return true; // House blocks path
                }
            }
        }

        // Check existing resource piles in scene
        foreach (var pile in piles)
        {
            if (pile == null || pile.Count <= 0) continue;

            Vector2 pilePos = pile.transform.position;
            if (Vector2.Distance(position, pilePos) < houseRadius + penguinClearance)
            {
                return true;
            }
        }

        return false; // No resources blocked
    }

    private bool IsPointNearLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd, float maxDistance)
    {
        Vector2 lineDir = lineEnd - lineStart;
        float lineLength = lineDir.magnitude;

        if (lineLength < 0.01f) return false;

        lineDir /= lineLength;

        Vector2 pointToStart = point - lineStart;
        float projection = Vector2.Dot(pointToStart, lineDir);

        // Clamp to line segment
        projection = Mathf.Clamp(projection, 0f, lineLength);

        Vector2 closestPoint = lineStart + lineDir * projection;
        float distance = Vector2.Distance(point, closestPoint);

        return distance < maxDistance;
    }

    private void DestroyPebblesAtLocation(Vector2 position)
    {
        // Get house collider bounds
        float searchRadius = 0.5f;

        if (ghostCollider is CircleCollider2D circleCol)
            searchRadius = circleCol.radius;
        else if (ghostCollider is BoxCollider2D boxCol)
            searchRadius = Mathf.Max(boxCol.size.x, boxCol.size.y) * 0.5f;

        // Find all colliders at placement location
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, searchRadius);

        foreach (var hit in hits)
        {
            // Check if it's a pebble
            var pebble = hit.GetComponent<Pebble>();
            if (pebble == null)
                pebble = hit.GetComponentInParent<Pebble>();

            if (pebble != null)
            {
                Debug.Log($"BuildModePlacer: Destroying pebble at {pebble.transform.position} to make room for house");
                Destroy(pebble.gameObject);
            }
        }
    }

    private void TryPlace()
    {
        if (!CanPlaceHere()) return;
        if (GameManager.I == null || GameManager.I.ice < selectedCost) return;

        // Destroy pebbles at placement location
        DestroyPebblesAtLocation(ghost.transform.position);

        Instantiate(selectedPrefab, ghost.transform.position, Quaternion.identity);
        GameManager.I.ice -= selectedCost;

        if (AudioManager.I != null)
            AudioManager.I.PlayPlaceIgloo();

        Cancel();
    }
}