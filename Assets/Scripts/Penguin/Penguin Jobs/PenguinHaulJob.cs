using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PenguinHaulJob : MonoBehaviour
{
    private PenguinJobs jobs;
    private PenguinMover mover;
    private PenguinAnimator anim;
    private PenguinCarryVisual carryVis;

    private ResourcePile pickupPile;
    private Coroutine routine;

    private int carryingAmount;
    private ResourceType carryingType = ResourceType.Food;

    public void Initialize(PenguinJobs jobs, PenguinMover mover, PenguinAnimator anim, PenguinCarryVisual carryVis)
    {
        this.jobs = jobs;
        this.mover = mover;
        this.anim = anim;
        this.carryVis = carryVis;
    }

    public void Begin(ResourcePile pile)
    {
        Cancel();

        pickupPile = pile;
        jobs.SetLookAt(pile.transform.position);

        Vector2 stand = mover.GetStandPosition(pile.transform.position, mover.defaultWorkOffset);

        anim.SetWalking();
        anim.FaceToward(pile.transform.position, mover.Position);

        mover.MoveTo(stand, () =>
        {
            jobs.SetStateCollecting();
            routine = StartCoroutine(CollectOneThenReturn());
        });
    }

    private IEnumerator CollectOneThenReturn()
    {
        anim.PlayCollecting();
        yield return new WaitForSeconds(jobs.collectHold);

        int taken = 0;
        if (pickupPile != null)
            pickupPile.TryTakeOne(out taken);

        if (taken > 0)
        {
            carryingAmount = taken;
            carryingType = pickupPile.type;

            carryVis.ShowCarried(pickupPile.carrySprite);
            anim.SetCarrying(true);
        }

        anim.StopCollecting();

        jobs.SetStateReturning();

        Vector2 drop = jobs.dropoffPoint ? (Vector2)jobs.dropoffPoint.position : Vector2.zero;
        jobs.SetLookAt(drop);

        if (carryingAmount > 0) anim.PlayWalkCarrying();
        else anim.SetWalking();

        mover.MoveTo(drop, () =>
        {
            if (carryingAmount > 0)
            {
                if (carryingType == ResourceType.Food) GameManager.I.AddFood(carryingAmount);
                else if (carryingType == ResourceType.Ice) GameManager.I.AddIce(carryingAmount);
            }

            carryingAmount = 0;
            pickupPile = null;

            carryVis.HideCarried();
            anim.SetCarrying(false);

            jobs.SetStateIdle();
        });
    }

    public void Cancel()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        carryingAmount = 0;
        carryingType = ResourceType.Food;
        pickupPile = null;

        if (carryVis != null) carryVis.HideCarried();
        if (anim != null) anim.SetCarrying(false);
    }
}
