using UnityEngine;

[RequireComponent(typeof(PenguinJobs))]
[RequireComponent(typeof(PenguinMover))]
[RequireComponent(typeof(PenguinAnimator))]
public class PenguinIdleWanderer : MonoBehaviour
{
    [Header("Wander Settings")]
    [Tooltip("Minimum distance for wander target (units)")]
    public float minWanderDistance = 2f;
    [Tooltip("Maximum distance for wander target (units)")]
    public float maxWanderDistance = 3f;
    [Tooltip("Time between wander movements (seconds)")]
    public float wanderInterval = 3f;
    [Tooltip("Random variance added to wander interval (seconds)")]
    public float wanderIntervalVariance = 1f;
    [Tooltip("How long penguin must be idle before wandering starts (seconds)")]
    public float minIdleTimeBeforeWander = 2f;

    [Header("Collision Avoidance")]
    [Tooltip("Radius to check for obstacles when picking wander point")]
    public float obstacleCheckRadius = 0.5f;
    [Tooltip("Max attempts to find a valid wander point")]
    public int maxAttempts = 8;

    [Header("Camera Bounds")]
    [Tooltip("Padding from camera edges (in world units)")]
    public float cameraPadding = 0.5f;

    private PenguinJobs jobs;
    private PenguinMover mover;
    private PenguinAnimator anim;
    private float wanderTimer;
    private bool isWandering;
    private LayerMask buildingsLayer;
    private Camera mainCamera;
    private float idleTimer;

    private void Awake()
    {
        jobs = GetComponent<PenguinJobs>();
        mover = GetComponent<PenguinMover>();
        anim = GetComponent<PenguinAnimator>();
        mainCamera = Camera.main;

        if (BuildModePlacer.I != null)
        {
            buildingsLayer = BuildModePlacer.I.blockingLayers;
        }
        else
        {
            buildingsLayer = LayerMask.GetMask("Buildings");
        }

        ResetWanderTimer();
    }

    private void Update()
    {
        bool isTrulyIdle = IsTrulyIdle();

        if (!isTrulyIdle)
        {
            idleTimer = 0f;
            isWandering = false;
            return;
        }

        idleTimer += Time.deltaTime;

        if (!isWandering && idleTimer > 0.5f && !IsWithinCameraBounds(transform.position))
        {
            TryReturnToCameraBounds();
            return;
        }

        if (idleTimer < minIdleTimeBeforeWander)
        {
            return;
        }

        wanderTimer -= Time.deltaTime;

        if (wanderTimer <= 0f)
        {
            TryStartWander();
            ResetWanderTimer();
        }
    }

    private bool IsTrulyIdle()
    {
        if (jobs == null || !jobs.IsIdle)
        {
            return false;
        }

        if (mover != null && mover.Velocity.sqrMagnitude > 0.01f)
        {
            return false;
        }

        return true;
    }

    private void TryStartWander()
    {
        if (isWandering) return;

        Vector2 currentPos = transform.position;
        Vector2? wanderPos = FindValidWanderPoint(currentPos);

        if (wanderPos.HasValue)
        {
            isWandering = true;
            anim?.SetWalking();
            anim?.FaceToward(wanderPos.Value, currentPos);
            mover.MoveTo(wanderPos.Value, OnWanderComplete);
        }
    }

    private Vector2? FindValidWanderPoint(Vector2 fromPos)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

            float distance = Random.Range(minWanderDistance, maxWanderDistance);

            Vector2 targetPos = fromPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

            if (IsValidWanderPoint(targetPos))
            {
                return targetPos;
            }
        }

        return null;
    }

    private bool IsValidWanderPoint(Vector2 pos)
    {
        Collider2D hit = Physics2D.OverlapCircle(pos, obstacleCheckRadius, buildingsLayer);
        if (hit != null) return false;

        return IsWithinCameraBounds(pos);
    }

    private bool IsWithinCameraBounds(Vector2 pos)
    {
        if (mainCamera == null) return true;

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(pos);

        float paddingViewport = cameraPadding / (mainCamera.orthographicSize * 2f);

        return viewportPos.x > paddingViewport &&
               viewportPos.x < (1f - paddingViewport) &&
               viewportPos.y > paddingViewport &&
               viewportPos.y < (1f - paddingViewport);
    }

    private void OnWanderComplete()
    {
        isWandering = false;
        anim?.SetIdle();
        idleTimer = 0f;
    }

    private void ResetWanderTimer()
    {
        wanderTimer = wanderInterval + Random.Range(-wanderIntervalVariance, wanderIntervalVariance);
        wanderTimer = Mathf.Max(0.5f, wanderTimer);
    }

    private void TryReturnToCameraBounds()
    {
        Vector2 currentPos = transform.position;
        Vector2? returnPos = FindPositionWithinCameraBounds(currentPos);

        if (returnPos.HasValue)
        {
            isWandering = true;
            anim?.SetWalking();
            anim?.FaceToward(returnPos.Value, currentPos);
            mover.MoveTo(returnPos.Value, OnWanderComplete);
        }
    }

    private Vector2? FindPositionWithinCameraBounds(Vector2 fromPos)
    {
        if (mainCamera == null) return null;

        Vector3 cameraCenter = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        Vector2 centerPos = new Vector2(cameraCenter.x, cameraCenter.y);

        Vector2 directionToCenter = (centerPos - fromPos).normalized;

        for (float distance = 0.5f; distance <= 5f; distance += 0.5f)
        {
            Vector2 testPos = fromPos + directionToCenter * distance;

            if (IsWithinCameraBounds(testPos))
            {
                Collider2D hit = Physics2D.OverlapCircle(testPos, obstacleCheckRadius, buildingsLayer);
                if (hit == null)
                {
                    return testPos;
                }
            }
        }

        if (IsWithinCameraBounds(centerPos))
        {
            Collider2D hit = Physics2D.OverlapCircle(centerPos, obstacleCheckRadius, buildingsLayer);
            if (hit == null)
            {
                return centerPos;
            }
        }

        for (int attempt = 0; attempt < maxAttempts * 2; attempt++)
        {
            float randomX = Random.Range(0.2f, 0.8f);
            float randomY = Random.Range(0.2f, 0.8f);
            Vector3 randomViewportPos = new Vector3(randomX, randomY, mainCamera.nearClipPlane);
            Vector3 randomWorldPos = mainCamera.ViewportToWorldPoint(randomViewportPos);
            Vector2 testPos = new Vector2(randomWorldPos.x, randomWorldPos.y);

            Collider2D hit = Physics2D.OverlapCircle(testPos, obstacleCheckRadius, buildingsLayer);
            if (hit == null)
            {
                return testPos;
            }
        }

        return null;
    }
}
