using UnityEngine;

public class BuildMode : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject iglooPrefab;
    public GameObject iglooGhostPrefab;

    [Header("Costs")]
    public int iceCost = 5;

    [Header("Placement")]
    public LayerMask blockingLayers;

    private Camera cam;
    private bool isBuilding;
    private GameObject ghost;
    private BoxCollider2D ghostCollider;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
            ToggleBuildMode();

        if (!isBuilding) return;

        UpdateGhostPosition();

        if (Input.GetMouseButtonDown(0))
            TryPlace();

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            ExitBuildMode();
    }

    private void ToggleBuildMode()
    {
        isBuilding = !isBuilding;

        if (isBuilding)
            EnterBuildMode();
        else
            ExitBuildMode();
    }

    private void EnterBuildMode()
    {
        ghost = Instantiate(iglooGhostPrefab);
        ghost.name = "IglooGhost";

        ghostCollider = ghost.GetComponent<BoxCollider2D>();
    }

    private void ExitBuildMode()
    {
        isBuilding = false;

        if (ghost != null)
            Destroy(ghost);
    }

    private void UpdateGhostPosition()
    {
        Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);

        Vector2 snapped = new Vector2(
            Mathf.Round(mouseWorld.x),
            Mathf.Round(mouseWorld.y)
        );

        ghost.transform.position = snapped;

        bool canPlace = CanPlaceHere(snapped);
        bool hasIce = GameManager.I.ice >= iceCost;

        UpdateGhostVisual(canPlace, hasIce);
    }


    private bool CanPlaceHere(Vector2 position)
    {
        if (ghostCollider == null) return false;

        // Temporarily enable collider for overlap check
        ghostCollider.enabled = true;

        Collider2D hit = Physics2D.OverlapBox(
            ghostCollider.bounds.center,
            ghostCollider.bounds.size,
            0f,
            blockingLayers
        );

        ghostCollider.enabled = false;

        return hit == null;
    }

    private void UpdateGhostVisual(bool canPlace, bool hasIce)
    {
        var sr = ghost.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        bool valid = canPlace && hasIce;

        sr.color = valid
            ? new Color(0f, 1f, 0f, 0.5f)  // green
            : new Color(1f, 0f, 0f, 0.5f); // red
    }


    private void TryPlace()
    {
        Vector2 pos = ghost.transform.position;

        if (!CanPlaceHere(pos))
            return;

        if (GameManager.I.ice < iceCost)
            return;

        Instantiate(iglooPrefab, pos, Quaternion.identity);

        GameManager.I.ice -= iceCost;
    }
}
