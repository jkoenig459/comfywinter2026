using UnityEngine;

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

        if (BuildModePlacer.I != null)
        {
            buildingsLayer = BuildModePlacer.I.blockingLayers;
        }
        else
        {
            buildingsLayer = LayerMask.GetMask("Buildings");
        }

        StartMoveToOpenArea();
    }

    private void StartMoveToOpenArea()
    {
        if (mover == null) return;

        Vector2 currentPos = transform.position;

        if (IsInOpenArea(currentPos))
        {
            FinishSpawning();
            return;
        }

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

        Destroy(this);
    }

    private bool IsInOpenArea(Vector2 pos)
    {
        Collider2D hit = Physics2D.OverlapCircle(pos, checkRadius, buildingsLayer);
        return hit == null;
    }

    private Vector2? FindNearestOpenArea(Vector2 fromPos)
    {
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

        if (movingToOpenArea && IsInOpenArea(transform.position))
        {
            mover?.Stop();
            FinishSpawning();
        }
    }
}
