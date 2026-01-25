using UnityEngine;

/// <summary>
/// Temporary component added to penguins when they spawn from a house.
/// Moves the penguin to an open area (not overlapping with any building),
/// then exits spawning mode and destroys itself.
/// </summary>
public class PenguinSpawnMover : MonoBehaviour
{
    [SerializeField] private LayerMask buildingsLayer;
    [SerializeField] private float checkRadius = 0.5f;
    [SerializeField] private float searchRadius = 3f;
    [SerializeField] private int searchSteps = 16;

    private YSorter ySorter;
    private PenguinMover mover;
    private PenguinAnimator anim;
    private bool initialized;
    private bool movingToOpenArea;

    public void Initialize(YSorter sorter, PenguinMover penguinMover, PenguinAnimator penguinAnim)
    {
        ySorter = sorter;
        mover = penguinMover;
        anim = penguinAnim;
        initialized = true;

        // Get the buildings layer from BuildModePlacer (same layer used for placement validation)
        if (BuildModePlacer.I != null)
        {
            buildingsLayer = BuildModePlacer.I.blockingLayers;
        }
        else
        {
            // Fallback to common layer name
            buildingsLayer = LayerMask.GetMask("Buildings");
        }

        // Start moving to open area
        StartMoveToOpenArea();
    }

    private void StartMoveToOpenArea()
    {
        if (mover == null) return;

        Vector2 currentPos = transform.position;

        // Check if already in open area
        if (IsInOpenArea(currentPos))
        {
            FinishSpawning();
            return;
        }

        // Find nearest open area
        Vector2? openPos = FindNearestOpenArea(currentPos);

        if (openPos.HasValue)
        {
            movingToOpenArea = true;
            anim?.SetWalking();
            anim?.FaceToward(openPos.Value, currentPos);
            mover.MoveTo(openPos.Value, OnArrivedAtOpenArea);
        }
        else
        {
            // No open area found, just finish spawning
            FinishSpawning();
        }
    }

    private void OnArrivedAtOpenArea()
    {
        movingToOpenArea = false;
        FinishSpawning();
    }

    private void FinishSpawning()
    {
        if (ySorter != null)
        {
            ySorter.ExitSpawningMode();
        }

        if (anim != null)
        {
            anim.SetIdle();
        }

        // Destroy this temporary component
        Destroy(this);
    }

    private bool IsInOpenArea(Vector2 pos)
    {
        // Check if there are any buildings overlapping this position
        Collider2D hit = Physics2D.OverlapCircle(pos, checkRadius, buildingsLayer);
        return hit == null;
    }

    private Vector2? FindNearestOpenArea(Vector2 fromPos)
    {
        // Search in expanding rings for an open position
        for (int ring = 1; ring <= 5; ring++)
        {
            float currentRadius = searchRadius * ring * 0.5f;

            for (int i = 0; i < searchSteps; i++)
            {
                float angle = (i / (float)searchSteps) * 360f * Mathf.Deg2Rad;
                Vector2 testPos = fromPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * currentRadius;

                if (IsInOpenArea(testPos))
                {
                    return testPos;
                }
            }
        }

        return null;
    }

    private void Update()
    {
        if (!initialized) return;

        // If we're moving and we reach an open area, finish early
        if (movingToOpenArea && IsInOpenArea(transform.position))
        {
            mover?.Stop();
            FinishSpawning();
        }
    }
}
