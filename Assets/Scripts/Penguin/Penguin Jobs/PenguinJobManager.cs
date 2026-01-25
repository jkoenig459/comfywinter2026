using UnityEngine;

[RequireComponent(typeof(PenguinMover))]
[RequireComponent(typeof(PenguinAnimator))]
[RequireComponent(typeof(PenguinCarryVisual))]
[DisallowMultipleComponent]
public class PenguinJobs : MonoBehaviour
{
    private enum JobState
    {
        Idle,

        MovingToFishSpot,
        Fishing,

        MovingToIceSpot,
        CuttingIce,

        MovingToPile,
        MovingToPebble,
        CollectingFromPile,
        ReturningToDropoff
    }

    [Header("References")]
    public Transform dropoffPoint;

    [Header("Output Piles")]
    public ResourcePile fishPilePrefab;
    public ResourcePile icePilePrefab;

    [Header("Fishing Timers")]
    public float fishInterval = 2.5f;
    public float fishCaughtAnimDuration = 0.55f;
    public float fishAddToPileDelay = 0.10f;
    public Vector2 fishPileOffset = new Vector2(0.15f, -0.20f);

    [Header("Ice Cutting Timers")]
    public float iceInterval = 2.0f;
    public float iceFinishedAnimDuration = 0.55f;
    public float iceAddToPileDelay = 0.10f;
    public Vector2 icePileOffset = new Vector2(0.15f, -0.20f);

    [Header("Hauling")]
    public float collectHold = 0.45f;
    public GameObject droppedResourcePrefab;

    [Header("Nearest Pile")]
    public float pileReuseRadius = 0.35f;

    private PenguinMover mover;
    private PenguinAnimator anim;
    private PenguinCarryVisual carryVis;

    private JobState state = JobState.Idle;
    private Vector2 lookAtPos;

    private PenguinFishJob fishJob;
    private PenguinIceJob iceJob;
    private PenguinHaulJob haulJob;

    public bool CanAcceptOrders => true;
    public bool IsIdle => state == JobState.Idle;

    private void Awake()
    {
        mover = GetComponent<PenguinMover>();
        anim = GetComponent<PenguinAnimator>();
        carryVis = GetComponent<PenguinCarryVisual>();

        fishJob = GetComponent<PenguinFishJob>() ?? gameObject.AddComponent<PenguinFishJob>();
        iceJob = GetComponent<PenguinIceJob>() ?? gameObject.AddComponent<PenguinIceJob>();
        haulJob = GetComponent<PenguinHaulJob>() ?? gameObject.AddComponent<PenguinHaulJob>();

        fishJob.Initialize(this, mover, anim);
        iceJob.Initialize(this, mover, anim);
        haulJob.Initialize(this, mover, anim, carryVis, droppedResourcePrefab);

        anim.SetIdle();
        carryVis.HideCarried();
        anim.SetCarrying(false);
        state = JobState.Idle;
        mover.UnlockPhysics();
    }

    private void Update()
    {
        if (state == JobState.Fishing ||
            state == JobState.MovingToFishSpot ||
            state == JobState.CuttingIce ||
            state == JobState.MovingToIceSpot ||
            state == JobState.MovingToPile ||
            state == JobState.MovingToPebble ||
            state == JobState.CollectingFromPile ||
            state == JobState.ReturningToDropoff)
        {
            anim.FaceToward(lookAtPos, mover.Position);
        }
    }


    public void AssignMove(Vector2 worldPos)
    {
        if (!CanAcceptOrders) return;

        CancelWork();
        state = JobState.Idle;

        lookAtPos = worldPos;
        anim.SetWalking();
        anim.FaceToward(worldPos, mover.Position);

        mover.MoveTo(worldPos, () =>
        {
            state = JobState.Idle;
            anim.SetIdle();
        });
    }

    public void AssignFish(ResourceNode node)
    {
        if (!CanAcceptOrders) return;
        if (node == null) return;

        CancelWork();

        lookAtPos = node.transform.position;
        state = JobState.MovingToFishSpot;

        fishJob.Begin(node.transform);
    }

    public void AssignCutIce(ResourceNode node)
    {
        if (!CanAcceptOrders) return;
        if (node == null) return;

        CancelWork();

        lookAtPos = node.transform.position;
        state = JobState.MovingToIceSpot;

        iceJob.Begin(node.transform);
    }

    public void AssignPickup(ResourcePile pile)
    {
        if (!CanAcceptOrders) return;
        if (pile == null || pile.IsEmpty) return;

        CancelWork();

        lookAtPos = pile.transform.position;
        state = JobState.MovingToPile;

        haulJob.Begin(pile);
    }

    public void AssignPickupPebble(Pebble pebble)
    {
        if (!CanAcceptOrders) return;
        if (pebble == null || pebble.IsPickedUp) return;

        CancelWork();

        lookAtPos = pebble.transform.position;
        state = JobState.MovingToPebble;

        haulJob.BeginPebble(pebble);
    }

    public void AssignPickupDropped(DroppedResource dropped)
    {
        if (!CanAcceptOrders) return;
        if (dropped == null || dropped.IsPickedUp) return;

        CancelWork();

        lookAtPos = dropped.transform.position;
        state = JobState.MovingToPile;

        haulJob.BeginDropped(dropped);
    }



    internal void SetLookAt(Vector2 pos) => lookAtPos = pos;

    internal void SetStateFishing()
    {
        state = JobState.Fishing;
        mover.LockPhysics();
    }

    internal void SetStateCuttingIce()
    {
        state = JobState.CuttingIce;
        mover.LockPhysics();
    }

    internal void SetStateCollecting()
    {
        state = JobState.CollectingFromPile;
        mover.UnlockPhysics();
    }

    internal void SetStateReturning()
    {
        state = JobState.ReturningToDropoff;
        mover.UnlockPhysics();
    }

    internal void SetStateIdle()
    {
        state = JobState.Idle;
        anim.SetIdle();
        mover.UnlockPhysics();
    }

    internal bool IsFishingState => state == JobState.Fishing;
    internal bool IsCuttingState => state == JobState.CuttingIce;
    internal bool IsCollectingState => state == JobState.CollectingFromPile;
    internal bool IsReturningState => state == JobState.ReturningToDropoff;


    internal ResourcePile GetOrCreatePileAt(Vector2 nodePos, ResourcePile prefab, Vector2 offset)
    {
        if (prefab == null) return null;

        Vector2 pilePos = nodePos + offset;

        var existing = FindExistingPileNear(pilePos, prefab.type, pileReuseRadius);
        if (existing != null)
            return existing;

        return Instantiate(prefab, pilePos, Quaternion.identity);
    }

    private ResourcePile FindExistingPileNear(Vector2 pos, ResourceType type, float radius)
    {
        var hits = Physics2D.OverlapCircleAll(pos, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            var pile = hits[i].GetComponentInParent<ResourcePile>();
            if (pile == null) continue;

            if (pile.type != type) continue;
            if (!pile.gameObject.activeInHierarchy) continue;

            return pile;
        }
        return null;
    }

    private void CancelWork()
    {
        fishJob?.Cancel();
        iceJob?.Cancel();
        haulJob?.Cancel();

        mover.Stop();

        carryVis.HideCarried();
        anim.SetCarrying(false);

        state = JobState.Idle;
        anim.SetIdle();
        mover.UnlockPhysics();
    }
}