using UnityEngine;

public class PenguinAnimator : MonoBehaviour
{
    [Header("References (optional - auto-find if empty)")]
    public Animator animator;
    public SpriteRenderer sprite;

    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private static readonly int IsIdle = Animator.StringToHash("IsIdle");
    private static readonly int IsFishing = Animator.StringToHash("IsFishing");
    private static readonly int IsCutting = Animator.StringToHash("IsCutting");
    private static readonly int IsCollecting = Animator.StringToHash("IsCollecting");
    private static readonly int IsCarrying = Animator.StringToHash("IsCarrying");
    private static readonly int CatchFish = Animator.StringToHash("CatchFish");
    private static readonly int FinishCut = Animator.StringToHash("FinishCut");

    // Idle facing memory
    private bool facingRight = false;

    private void Awake()
    {
        if (!sprite) sprite = GetComponentInChildren<SpriteRenderer>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        ApplyFacing();
    }

    public void FaceToward(Vector2 lookAtWorldPos, Vector2 selfWorldPos)
    {
        if (!sprite) return;

        float dx = lookAtWorldPos.x - selfWorldPos.x;

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

    public void SetIdle()
    {
        if (!animator) return;

        animator.SetBool(IsWalking, false);
        animator.SetBool(IsIdle, true);
        animator.SetBool(IsFishing, false);
        animator.SetBool(IsCutting, false);
        animator.SetBool(IsCollecting, false);
        animator.SetBool(IsCarrying, false);
    }

    public void SetWalking()
    {
        if (!animator) return;

        animator.SetBool(IsWalking, true);
        animator.SetBool(IsIdle, false);
        animator.SetBool(IsFishing, false);
        animator.SetBool(IsCutting, false);
        animator.SetBool(IsCollecting, false);
        animator.SetBool(IsCarrying, false);
    }

    public void SetFishingLoop()
    {
        if (!animator) return;

        animator.SetBool(IsWalking, false);
        animator.SetBool(IsIdle, false);
        animator.SetBool(IsFishing, true);
        animator.SetBool(IsCutting, false);
        animator.SetBool(IsCollecting, false);
        animator.SetBool(IsCarrying, false);
    }

    public void SetCuttingLoop()
    {
        if (!animator) return;

        animator.SetBool(IsWalking, false);
        animator.SetBool(IsIdle, false);
        animator.SetBool(IsFishing, false);
        animator.SetBool(IsCutting, true);
        animator.SetBool(IsCollecting, false);
        animator.SetBool(IsCarrying, false);
    }

    public void PlayCollecting()
    {
        if (!animator) return;

        animator.SetBool(IsWalking, false);
        animator.SetBool(IsIdle, false);
        animator.SetBool(IsFishing, false);
        animator.SetBool(IsCutting, false);
        animator.SetBool(IsCollecting, true);
        animator.SetBool(IsCarrying, false);
    }

    public void StopCollecting()
    {
        if (!animator) return;
        animator.SetBool(IsCollecting, false);
    }

    public void SetCarrying(bool carrying)
    {
        if (!animator) return;
        animator.SetBool(IsCarrying, carrying);
    }

    public void PlayWalkCarrying()
    {
        if (!animator) return;

        animator.SetBool(IsWalking, true);
        animator.SetBool(IsIdle, false);
        animator.SetBool(IsFishing, false);
        animator.SetBool(IsCutting, false);
        animator.SetBool(IsCollecting, false);
        animator.SetBool(IsCarrying, true);
    }

    // Triggers

    public void TriggerCatchFish()
    {
        if (!animator) return;
        animator.SetTrigger(CatchFish);
    }

    public void TriggerFinishCut()
    {
        if (!animator) return;
        animator.SetTrigger(FinishCut);
    }

    public Animator GetAnimator()
    {
        return animator;
    }
}
