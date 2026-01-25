using UnityEngine;

/// <summary>
/// Makes idle penguins wander to random nearby points.
/// Only wanders when the penguin has no assigned work.
/// </summary>
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

        // Get buildings layer from BuildModePlacer if available
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
        // Check if penguin is truly idle (no jobs, in idle animation, not moving)
        bool isTrulyIdle = IsTrulyIdle();

        if (!isTrulyIdle)
        {
            // Reset idle timer when not idle
            idleTimer = 0f;
            isWandering = false;
            return;
        }

        // Accumulate idle time
        idleTimer += Time.deltaTime;

        // Check if idle penguin is out of camera bounds and return them immediately
        // Only check after being idle for at least 0.5 seconds to avoid triggering on spawn
        if (!isWandering && idleTimer > 0.5f && !IsWithinCameraBounds(transform.position))
        {
            TryReturnToCameraBounds();
            return; // Skip normal wandering this frame
        }

        // Only start wandering if been idle long enough
        if (idleTimer < minIdleTimeBeforeWander)
        {
            return;
        }

        // Count down wander timer
        wanderTimer -= Time.deltaTime;

        if (wanderTimer <= 0f)
        {
            TryStartWander();
            ResetWanderTimer();
        }
    }

    private bool IsTrulyIdle()
    {
        // Must have no active jobs (check the actual job state, not just CanAcceptOrders)
        if (jobs == null || !jobs.IsIdle)
        {
            return false;
        }

        // Must not be moving
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
            // Pick a random angle
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

            // Pick a random distance within range
            float distance = Random.Range(minWanderDistance, maxWanderDistance);

            // Calculate target position
            Vector2 targetPos = fromPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

            // Check if position is valid (not overlapping buildings)
            if (IsValidWanderPoint(targetPos))
            {
                return targetPos;
            }
        }

        return null;
    }

    private bool IsValidWanderPoint(Vector2 pos)
    {
        // Check if there are any buildings at this position
        Collider2D hit = Physics2D.OverlapCircle(pos, obstacleCheckRadius, buildingsLayer);
        if (hit != null) return false;

        // Check if position is within camera bounds
        return IsWithinCameraBounds(pos);
    }

    private bool IsWithinCameraBounds(Vector2 pos)
    {
        if (mainCamera == null) return true; // Skip check if no camera

        // Convert world position to viewport position
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(pos);

        // Check if within bounds (0-1 for viewport, with padding)
        // orthographicSize is half-height, so full height is orthographicSize * 2
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
        // Reset idle timer so penguin must be idle again before next wander
        idleTimer = 0f;
    }

    private void ResetWanderTimer()
    {
        wanderTimer = wanderInterval + Random.Range(-wanderIntervalVariance, wanderIntervalVariance);
        // Ensure timer is never negative
        wanderTimer = Mathf.Max(0.5f, wanderTimer);
    }

    /// <summary>
    /// Tries to return the penguin to camera bounds when pushed out.
    /// Finds a safe position within camera bounds and walks there.
    /// </summary>
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

    /// <summary>
    /// Finds a valid position within camera bounds, starting from the penguin's current position
    /// and moving toward the center of the camera view.
    /// </summary>
    private Vector2? FindPositionWithinCameraBounds(Vector2 fromPos)
    {
        if (mainCamera == null) return null;

        // Get camera center in world space
        Vector3 cameraCenter = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        Vector2 centerPos = new Vector2(cameraCenter.x, cameraCenter.y);

        // Calculate direction from current position toward camera center
        Vector2 directionToCenter = (centerPos - fromPos).normalized;

        // Try positions moving toward the center
        for (float distance = 0.5f; distance <= 5f; distance += 0.5f)
        {
            Vector2 testPos = fromPos + directionToCenter * distance;

            // Check if this position is within camera bounds and not on a building
            if (IsWithinCameraBounds(testPos))
            {
                // Check for buildings
                Collider2D hit = Physics2D.OverlapCircle(testPos, obstacleCheckRadius, buildingsLayer);
                if (hit == null)
                {
                    return testPos;
                }
            }
        }

        // If no position found moving toward center, try the camera center itself
        if (IsWithinCameraBounds(centerPos))
        {
            Collider2D hit = Physics2D.OverlapCircle(centerPos, obstacleCheckRadius, buildingsLayer);
            if (hit == null)
            {
                return centerPos;
            }
        }

        // Last resort: try random positions within camera bounds
        for (int attempt = 0; attempt < maxAttempts * 2; attempt++)
        {
            // Generate random position within camera viewport
            float randomX = Random.Range(0.2f, 0.8f);
            float randomY = Random.Range(0.2f, 0.8f);
            Vector3 randomViewportPos = new Vector3(randomX, randomY, mainCamera.nearClipPlane);
            Vector3 randomWorldPos = mainCamera.ViewportToWorldPoint(randomViewportPos);
            Vector2 testPos = new Vector2(randomWorldPos.x, randomWorldPos.y);

            // Check if this position is valid
            Collider2D hit = Physics2D.OverlapCircle(testPos, obstacleCheckRadius, buildingsLayer);
            if (hit == null)
            {
                return testPos;
            }
        }

        return null;
    }
}
