using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PenguinMover : MonoBehaviour
{
    public float moveSpeed = 2f;

    [Header("Stand Offsets (world units)")]
    [Tooltip("Default work offset (used if no specific offset is chosen).")]
    public Vector2 defaultWorkOffset = new Vector2(0.35f, -0.12f);

    [Tooltip("Offset when fishing.")]
    public Vector2 fishingOffset = new Vector2(0.35f, -0.12f);

    [Tooltip("Offset when cutting ice.")]
    public Vector2 iceOffset = new Vector2(0.30f, -0.05f);

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
        Vector2 next = Vector2.MoveTowards(pos, moveTarget, moveSpeed * Time.fixedDeltaTime);
        Velocity = (next - pos) / Time.fixedDeltaTime;

        rb.MovePosition(next);

        if ((next - moveTarget).sqrMagnitude <= 0.0001f)
        {
            hasTarget = false;
            Velocity = Vector2.zero;
            rb.linearVelocity = Vector2.zero;

            var cb = onArrive;
            onArrive = null;
            cb?.Invoke();
        }
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
