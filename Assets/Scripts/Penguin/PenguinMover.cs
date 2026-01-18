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
    [SerializeField] private float detectionRadius = 0.8f;
    [SerializeField] private float avoidanceForce = 1.5f;
    [SerializeField] private float raycastDistance = 1.2f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float unstuckForce = 3f;
    [SerializeField] private float ignoreAvoidanceDistance = 0.5f;

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

        Vector2 avoidance = Vector2.zero;
        if (distanceToTarget > ignoreAvoidanceDistance)
        {
            avoidance = CalculateAvoidance(pos, directionToTarget);
        }

        Vector2 desiredDirection = (directionToTarget + avoidance).normalized;

        Vector2 next = pos + desiredDirection * moveSpeed * Time.fixedDeltaTime;
        Velocity = (next - pos) / Time.fixedDeltaTime;

        rb.MovePosition(next);

        if ((pos - moveTarget).sqrMagnitude <= 0.01f)
        {
            hasTarget = false;
            Velocity = Vector2.zero;
            rb.linearVelocity = Vector2.zero;

            var cb = onArrive;
            onArrive = null;
            cb?.Invoke();
        }
    }

    private Vector2 CalculateAvoidance(Vector2 pos, Vector2 moveDirection)
    {
        Vector2 avoidanceVector = Vector2.zero;

        Collider2D[] obstacles = Physics2D.OverlapCircleAll(pos, detectionRadius, obstacleLayer);

        bool isStuck = false;
        Vector2 escapeDirection = Vector2.zero;

        foreach (Collider2D obstacle in obstacles)
        {
            if (obstacle.transform == transform) continue;

            Vector2 obstaclePos = obstacle.ClosestPoint(pos);
            Vector2 directionFromObstacle = pos - obstaclePos;
            float distance = directionFromObstacle.magnitude;

            if (distance < 0.01f)
            {
                isStuck = true;
                escapeDirection = -moveDirection;
                continue;
            }

            if (distance < detectionRadius * 0.5f)
            {
                isStuck = true;
                escapeDirection += directionFromObstacle.normalized;
            }

            float weight = 1f - (distance / detectionRadius);
            avoidanceVector += directionFromObstacle.normalized * weight * avoidanceForce;
        }

        if (isStuck)
        {
            return escapeDirection.normalized * unstuckForce;
        }

        RaycastHit2D hit = Physics2D.Raycast(pos, moveDirection, raycastDistance, obstacleLayer);
        if (hit.collider != null && hit.collider.transform != transform)
        {
            Vector2 perpendicular = Vector2.Perpendicular(moveDirection);

            Vector2 leftCheck = pos + perpendicular * detectionRadius * 0.5f;
            Vector2 rightCheck = pos - perpendicular * detectionRadius * 0.5f;

            RaycastHit2D leftHit = Physics2D.Raycast(leftCheck, moveDirection, raycastDistance, obstacleLayer);
            RaycastHit2D rightHit = Physics2D.Raycast(rightCheck, moveDirection, raycastDistance, obstacleLayer);

            if (!leftHit.collider || (rightHit.collider && leftHit.distance > rightHit.distance))
            {
                avoidanceVector += perpendicular * avoidanceForce * 2f;
            }
            else
            {
                avoidanceVector -= perpendicular * avoidanceForce * 2f;
            }
        }

        return avoidanceVector;
    }

    public void MoveTo(Vector2 worldPos, System.Action arriveCallback = null)
    {
        moveTarget = worldPos;
        hasTarget = true;
        onArrive = arriveCallback;
    }

    public void Stop()
    {
        hasTarget = false;
        onArrive = null;
        Velocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
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