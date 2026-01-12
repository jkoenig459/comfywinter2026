using UnityEngine;

public class PenguinUnit : MonoBehaviour
{
    private enum State { Idle, MovingToTarget, Working, Returning }

    [Header("Movement")]
    public float moveSpeed = 2.0f;
    public Transform dropoffPoint;

    [Header("Working")]
    public float workTimer;
    public int carrying;

    [Header("Animation")]
    public Animator animator;

    private State state = State.Idle;
    private ResourceNode targetNode;
    private Vector3 targetPos;

    private SpriteRenderer sprite;

    // Idle facing memory
    private bool facingRight = false;

    // Animator
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private static readonly int IsIdle = Animator.StringToHash("IsIdle");
    private static readonly int IsFishing = Animator.StringToHash("IsFishing");
    private static readonly int IsCutting = Animator.StringToHash("IsCutting");
    private static readonly int IsCollecting = Animator.StringToHash("IsCollecting");

    private void Awake()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        facingRight = false;
        ApplyFacing();

        SetIdleAnim();
    }

    private void Update()
    {
        switch (state)
        {
            case State.Idle:
                SetIdleAnim();
                break;

            case State.MovingToTarget:
                SetWalkAnim();
                MoveTowards(targetPos, onArrive: () =>
                {
                    state = State.Working;
                    workTimer = 0f;
                });
                break;

            case State.Working:
                if (targetNode == null)
                {
                    state = State.Idle;
                    SetIdleAnim();
                    break;
                }

                SetWorkingAnimForTarget();

                workTimer += Time.deltaTime;
                if (workTimer >= targetNode.workTime)
                {
                    carrying = targetNode.yieldAmount;
                    state = State.Returning;
                    targetPos = dropoffPoint != null ? dropoffPoint.position : Vector3.zero;
                }
                break;

            case State.Returning:
                SetWalkAnim();
                MoveTowards(targetPos, onArrive: DepositAndIdle);
                break;
        }
    }

    private void MoveTowards(Vector3 dest, System.Action onArrive)
    {
        UpdateFacingFromDestination(dest);

        Vector3 pos = transform.position;
        Vector3 next = Vector3.MoveTowards(pos, dest, moveSpeed * Time.deltaTime);
        transform.position = next;

        if ((next - dest).sqrMagnitude <= 0.0001f)
            onArrive?.Invoke();
    }

    private void DepositAndIdle()
    {
        if (carrying > 0 && targetNode != null)
        {
            if (targetNode.type == ResourceType.Ice) GameManager.I.AddIce(carrying);
            if (targetNode.type == ResourceType.Food) GameManager.I.AddFood(carrying);
        }

        carrying = 0;
        targetNode = null;
        state = State.Idle;

        SetIdleAnim();
    }

    public void AssignGather(ResourceNode node)
    {
        if (node == null) return;

        targetNode = node;
        targetPos = node.transform.position;
        state = State.MovingToTarget;

        UpdateFacingFromDestination(targetPos);
        SetWalkAnim();
    }

    public void AssignMove(Vector3 worldPos)
    {
        targetNode = null;
        carrying = 0;
        targetPos = worldPos;
        state = State.MovingToTarget;

        UpdateFacingFromDestination(targetPos);
        SetWalkAnim();
    }

    private void UpdateFacingFromDestination(Vector3 destination)
    {
        if (!sprite) return;

        float dx = destination.x - transform.position.x;

        if (dx > 0.01f)
        {
            facingRight = true;
            ApplyFacing();
        }
        else if (dx < -0.01f)
        {
            facingRight = false;
            ApplyFacing();
        }
    }

    private void ApplyFacing()
    {
        if (!sprite) return;
        sprite.flipX = facingRight;
    }

    private void SetIdleAnim()
    {
        if (!animator) return;

        animator.SetBool(IsWalking, false);
        animator.SetBool(IsIdle, true);

        animator.SetBool(IsFishing, false);
        animator.SetBool(IsCutting, false);
        animator.SetBool(IsCollecting, false);
    }

    private void SetWalkAnim()
    {
        if (!animator) return;

        animator.SetBool(IsWalking, true);
        animator.SetBool(IsIdle, false);

        animator.SetBool(IsFishing, false);
        animator.SetBool(IsCutting, false);
        animator.SetBool(IsCollecting, false);
    }

    private void SetWorkingAnimForTarget()
    {
        if (!animator || targetNode == null) return;

        animator.SetBool(IsWalking, false);
        animator.SetBool(IsIdle, false);

        bool isIce = targetNode.type == ResourceType.Ice;
        bool isFood = targetNode.type == ResourceType.Food;

        animator.SetBool(IsCutting, isIce);
        animator.SetBool(IsFishing, isFood);

        animator.SetBool(IsCollecting, false);
    }
}
