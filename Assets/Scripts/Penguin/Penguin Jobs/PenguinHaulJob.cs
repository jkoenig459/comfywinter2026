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
    private Pebble pickupPebble;
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
        pickupPebble = null;
        jobs.SetLookAt(pile.transform.position);

        Vector2 stand = mover.GetStandPosition(pile.transform.position, mover.defaultWorkOffset);

        anim.SetWalking();
        anim.FaceToward(pile.transform.position, mover.Position);

        mover.MoveTo(stand, () =>
        {
            jobs.SetStateCollecting();
            routine = StartCoroutine(CollectFromPileThenReturn());
        });
    }

    public void BeginPebble(Pebble pebble)
    {
        Cancel();

        pickupPebble = pebble;
        pickupPile = null;
        jobs.SetLookAt(pebble.transform.position);

        Vector2 stand = mover.GetStandPosition(pebble.transform.position, mover.defaultWorkOffset);

        anim.SetWalking();
        anim.FaceToward(pebble.transform.position, mover.Position);

        mover.MoveTo(stand, () =>
        {
            jobs.SetStateCollecting();
            routine = StartCoroutine(CollectPebbleThenReturn());
        });
    }

    private IEnumerator CollectFromPileThenReturn()
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
            DepositCarried();
            jobs.SetStateIdle();
        });
    }

    private IEnumerator CollectPebbleThenReturn()
    {
        anim.PlayCollecting();
        yield return new WaitForSeconds(jobs.collectHold);

        Sprite carrySprite = null;
        bool picked = false;

        if (pickupPebble != null && !pickupPebble.IsPickedUp)
            picked = pickupPebble.TryPickup(out carrySprite);

        if (picked)
        {
            carryingAmount = 1;
            carryingType = ResourceType.Pebble;

            carryVis.ShowCarried(carrySprite);
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
            DepositCarried();
            jobs.SetStateIdle();
        });
    }

    private void DepositCarried()
    {
        if (carryingAmount > 0)
        {
            if (carryingType == ResourceType.Food)
                GameManager.I.AddFood(carryingAmount);
            else if (carryingType == ResourceType.Ice)
                GameManager.I.AddIce(carryingAmount);
            else if (carryingType == ResourceType.Pebble)
                GameManager.I.AddPebbles(carryingAmount);
        }

        carryingAmount = 0;
        pickupPile = null;
        pickupPebble = null;

        carryVis.HideCarried();
        anim.SetCarrying(false);
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
        pickupPebble = null;

        if (carryVis != null) carryVis.HideCarried();
        if (anim != null) anim.SetCarrying(false);
    }
}