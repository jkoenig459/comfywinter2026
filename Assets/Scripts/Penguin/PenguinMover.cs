using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PenguinMover : MonoBehaviour
{
    public float moveSpeed = 2f;

    [Header("Stand Offsets (world units)")]
    public Vector2 defaultWorkOffset = new Vector2(0.35f, -0.12f);
    public Vector2 fishingOffset = new Vector2(0.35f, -0.12f);
    public Vector2 iceOffset = new Vector2(0.30f, -0.05f);

    [Header("Obstacle Avoidance")]
    [SerializeField] private float detectionRadius = 1.5f;
    [SerializeField] private float avoidanceForce = 3.5f;
    [SerializeField] private float raycastDistance = 2f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float unstuckForce = 4f;
    [SerializeField] private float minDistanceToObstacle = 0.5f;
    [SerializeField] private float stuckTimeThreshold = 0.3f;
    [SerializeField] private float clearanceTime = 0.5f;
    [SerializeField] private int raycastAngles = 5;

    private Vector2 lastPosition;
    private float stuckTimer = 0f;
    private Vector2 stuckEscapeDirection;
    private int avoidanceAttempts = 0;
    private Vector2 navigationDirection = Vector2.zero; // Locked navigation direction
    private float clearanceTimer = 0f;
    private Collider2D currentObstacle = null;
    private bool isNavigatingAround = false;

    private Rigidbody2D rb;
    private Vector2 moveTarget;
    private bool hasTarget;
    private System.Action onArrive;

    public Vector2 Velocity { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        if (!hasTarget)
        {
            Velocity = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 pos = rb.position;
        float distanceToTarget = (moveTarget - pos).magnitude;
        Vector2 directionToTarget = (moveTarget - pos).normalized;

        // Check if penguin is stuck
        float movementThisFrame = (pos - lastPosition).magnitude;
        if (movementThisFrame < 0.01f)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= stuckTimeThreshold)
            {
                avoidanceAttempts++;
            }
        }
        else
        {
            if (movementThisFrame > 0.05f) // Good movement, reset
            {
                stuckTimer = 0f;
                avoidanceAttempts = 0;
            }
        }
        lastPosition = pos;

        // Check if there's an obstacle directly between us and the target
        RaycastHit2D targetRaycast = Physics2D.Raycast(pos, directionToTarget, distanceToTarget, obstacleLayer);
        bool obstacleBlockingTarget = targetRaycast.collider != null && targetRaycast.collider.transform != transform;

        // Calculate avoidance
        Vector2 avoidance = CalculateAvoidance(pos, directionToTarget, distanceToTarget, obstacleBlockingTarget);

        Vector2 desiredDirection;

        // If already navigating around, use ONLY the navigation direction (highest priority)
        if (isNavigatingAround && avoidance.sqrMagnitude > 0.01f)
        {
            // Pure avoidance direction, no blending
            desiredDirection = avoidance.normalized;
        }
        // If obstacle is blocking the direct path, use avoidance but blend slightly with target
        else if (obstacleBlockingTarget && avoidance.sqrMagnitude > 0.01f)
        {
            // Mostly avoidance, but tiny bit toward target to maintain progress
            desiredDirection = (avoidance.normalized * 4f + directionToTarget).normalized;
        }
        else
        {
            // Otherwise blend target direction with avoidance
            desiredDirection = (directionToTarget + avoidance).normalized;
        }

        Vector2 next = pos + desiredDirection * moveSpeed * Time.fixedDeltaTime;
        Velocity = (next - pos) / Time.fixedDeltaTime;

        rb.MovePosition(next);

        if ((pos - moveTarget).sqrMagnitude <= 0.01f)
        {
            hasTarget = false;
            stuckTimer = 0f;
            avoidanceAttempts = 0;
            navigationDirection = Vector2.zero;
            clearanceTimer = 0f;
            currentObstacle = null;
            isNavigatingAround = false;
            Velocity = Vector2.zero;
            rb.linearVelocity = Vector2.zero;

            var cb = onArrive;
            onArrive = null;
            cb?.Invoke();
        }
    }

    private Vector2 CalculateAvoidance(Vector2 pos, Vector2 moveDirection, float distanceToTarget, bool obstacleBlockingTarget)
    {
        Vector2 avoidanceVector = Vector2.zero;

        Collider2D[] obstacles = Physics2D.OverlapCircleAll(pos, detectionRadius, obstacleLayer);

        bool hasObstaclesNearby = false;
        bool isStuck = false;
        Vector2 escapeDirection = Vector2.zero;
        Collider2D nearestObstacle = null;
        float nearestDistance = float.MaxValue;

        // Check for obstacles in detection radius
        foreach (Collider2D obstacle in obstacles)
        {
            if (obstacle.transform == transform) continue;

            hasObstaclesNearby = true;
            Vector2 obstaclePos = obstacle.ClosestPoint(pos);
            Vector2 directionFromObstacle = pos - obstaclePos;
            float distance = directionFromObstacle.magnitude;

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestObstacle = obstacle;
            }

            // Very close or overlapping - definitely stuck
            if (distance < minDistanceToObstacle * 0.5f)
            {
                isStuck = true;
                Vector2 toObstacle = (obstaclePos - pos).normalized;
                Vector2 perpendicular = Vector2.Perpendicular(toObstacle);

                // Use navigation direction if we have one
                if (navigationDirection.sqrMagnitude > 0.01f)
                {
                    escapeDirection += navigationDirection;
                }
                else
                {
                    // Choose the perpendicular direction closer to target
                    Vector2 toTarget = (moveTarget - pos).normalized;
                    if (Vector2.Dot(perpendicular, toTarget) < 0)
                    {
                        perpendicular = -perpendicular;
                    }
                    escapeDirection += perpendicular;
                }

                // If stuck for too long, add backwards force
                if (avoidanceAttempts > 3)
                {
                    escapeDirection += -toObstacle * 0.5f;
                }
            }
            // Close enough to trigger strong avoidance (but only if not already navigating)
            else if (distance < minDistanceToObstacle && !isNavigatingAround)
            {
                float urgency = 1f - (distance / minDistanceToObstacle);
                avoidanceVector += directionFromObstacle.normalized * urgency * avoidanceForce * 3f;
            }
            // Normal avoidance (but only if not already navigating)
            else if (distance < detectionRadius && !isNavigatingAround)
            {
                float weight = 1f - (distance / detectionRadius);
                avoidanceVector += directionFromObstacle.normalized * weight * avoidanceForce;
            }
        }

        // Update clearance timer based on obstacles
        if (hasObstaclesNearby)
        {
            clearanceTimer = 0f; // Reset clearance timer while obstacles present
        }
        else
        {
            clearanceTimer += Time.fixedDeltaTime;
            if (clearanceTimer >= clearanceTime)
            {
                // Clear navigation state after being clear for the full duration
                navigationDirection = Vector2.zero;
                currentObstacle = null;
                isNavigatingAround = false;
            }
        }

        // If stuck, return strong escape direction
        if (isStuck)
        {
            if (escapeDirection.sqrMagnitude < 0.01f)
            {
                // Last resort: try different perpendicular directions
                if (avoidanceAttempts % 2 == 0)
                    escapeDirection = Vector2.Perpendicular(moveDirection);
                else
                    escapeDirection = -Vector2.Perpendicular(moveDirection);
            }

            // Set this as navigation direction if we don't have one
            if (navigationDirection.sqrMagnitude < 0.01f)
            {
                navigationDirection = escapeDirection.normalized;
                isNavigatingAround = true;
            }

            return escapeDirection.normalized * unstuckForce;
        }

        // If we're already navigating around, maintain the locked direction
        if (isNavigatingAround && navigationDirection.sqrMagnitude > 0.01f && hasObstaclesNearby)
        {
            // Use the locked navigation direction
            avoidanceVector = navigationDirection * avoidanceForce * 5f;

            // Add very gentle push away from nearest obstacle only if very close
            if (nearestObstacle != null && nearestDistance < minDistanceToObstacle * 0.75f)
            {
                Vector2 obstaclePos = nearestObstacle.ClosestPoint(pos);
                Vector2 awayFromObstacle = (pos - obstaclePos).normalized;
                float urgency = 1f - (nearestDistance / (minDistanceToObstacle * 0.75f));
                avoidanceVector += awayFromObstacle * urgency * avoidanceForce * 0.8f;
            }

            return avoidanceVector;
        }

        // Multi-angle raycast detection for choosing initial direction
        float angleStep = 45f / raycastAngles;
        float leftScore = 0f;
        float rightScore = 0f;
        bool obstacleAhead = false;
        RaycastHit2D centerHit = default;

        for (int i = -raycastAngles; i <= raycastAngles; i++)
        {
            float angle = i * angleStep;
            Vector2 dir = Quaternion.Euler(0, 0, angle) * moveDirection;
            RaycastHit2D hit = Physics2D.Raycast(pos, dir, raycastDistance, obstacleLayer);

            if (hit.collider != null && hit.collider.transform != transform)
            {
                if (i == 0)
                {
                    obstacleAhead = true;
                    centerHit = hit;
                }

                // Score based on angle and distance
                float angleWeight = 1f - (Mathf.Abs(angle) / 45f);
                float distanceWeight = 1f - (hit.distance / raycastDistance);
                float score = angleWeight * distanceWeight;

                if (i < 0)
                    leftScore += score; // Left is blocked
                else if (i > 0)
                    rightScore += score; // Right is blocked
            }
        }

        // Determine navigation direction if obstacle ahead and not already navigating
        if ((obstacleAhead || obstacleBlockingTarget) && !isNavigatingAround)
        {
            Vector2 perpendicular = Vector2.Perpendicular(moveDirection);
            Vector2 toTarget = (moveTarget - pos).normalized;
            int chosenSide = 0;

            // Choose the clearer side based on multi-angle raycasts
            if (leftScore < rightScore - 0.1f) // Left is clearer
            {
                chosenSide = 1; // Go left (positive perpendicular)
            }
            else if (rightScore < leftScore - 0.1f) // Right is clearer
            {
                chosenSide = -1; // Go right (negative perpendicular)
            }
            else // Both sides equally blocked or clear
            {
                // Pick based on which side angles more toward target
                float leftDot = Vector2.Dot(perpendicular, toTarget);

                if (leftDot > 0.1f)
                    chosenSide = 1;
                else if (leftDot < -0.1f)
                    chosenSide = -1;
                else
                    chosenSide = (Random.value > 0.5f) ? 1 : -1; // Random as last resort
            }

            // Lock in the navigation direction (perpendicular + small forward bias)
            // Small forward component helps maintain speed and progress
            navigationDirection = (perpendicular * chosenSide * 2.5f + moveDirection).normalized;
            isNavigatingAround = true;

            if (obstacleAhead && centerHit.collider != null)
            {
                currentObstacle = centerHit.collider;
            }
            else if (nearestObstacle != null)
            {
                currentObstacle = nearestObstacle;
            }

            // Apply the navigation direction
            avoidanceVector = navigationDirection * avoidanceForce * 5f;
        }

        return avoidanceVector;
    }

    public void MoveTo(Vector2 worldPos, System.Action arriveCallback = null)
    {
        moveTarget = worldPos;
        hasTarget = true;
        stuckTimer = 0f;
        avoidanceAttempts = 0;
        navigationDirection = Vector2.zero;
        clearanceTimer = 0f;
        currentObstacle = null;
        isNavigatingAround = false;
        onArrive = arriveCallback;
    }

    public void Stop()
    {
        hasTarget = false;
        onArrive = null;
        Velocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        navigationDirection = Vector2.zero;
        clearanceTimer = 0f;
        currentObstacle = null;
        isNavigatingAround = false;
    }

    public Vector2 GetStandPosition(Vector2 targetPos, Vector2 offset)
    {
        float dx = targetPos.x - rb.position.x;
        float xSign = (dx >= 0f) ? -1f : 1f;
        return targetPos + new Vector2(offset.x * xSign, offset.y);
    }

    public Vector2 GetStandPosition(Vector2 targetPos)
    {
        return GetStandPosition(targetPos, defaultWorkOffset);
    }

    public Vector2 Position => rb.position;
}
