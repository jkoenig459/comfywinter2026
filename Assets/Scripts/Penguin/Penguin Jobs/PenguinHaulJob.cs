using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PenguinHaulJob : MonoBehaviour
{
    private PenguinJobs jobs;
    private PenguinMover mover;
    private PenguinAnimator anim;
    private PenguinCarryVisual carryVis;
    private GameObject droppedResourcePrefab;

    private ResourcePile pickupPile;
    private Pebble pickupPebble;
    private DroppedResource pickupDropped;
    private Coroutine routine;

    private int carryingAmount;
    private ResourceType carryingType = ResourceType.Food;
    private Sprite carryingSprite;

    public void Initialize(PenguinJobs jobs, PenguinMover mover, PenguinAnimator anim, PenguinCarryVisual carryVis, GameObject droppedPrefab)
    {
        this.jobs = jobs;
        this.mover = mover;
        this.anim = anim;
        this.carryVis = carryVis;
        this.droppedResourcePrefab = droppedPrefab;
    }

    public void Begin(ResourcePile pile)
    {
        Cancel();

        pickupPile = pile;
        pickupPebble = null;
        pickupDropped = null;
        jobs.SetLookAt(pile.transform.position);

        Vector2 stand = mover.GetStandPosition(pile.transform.position, mover.defaultWorkOffset);

        anim.SetWalking();
        anim.FaceToward(pile.transform.position, mover.Position);

        mover.MoveTo(stand, () =>
        {
            jobs.SetStateCollecting();
            routine = StartCoroutine(CollectFromPileThenReturnContinuous());
        });
    }

    public void BeginPebble(Pebble pebble)
    {
        Cancel();

        pickupPebble = pebble;
        pickupPile = null;
        pickupDropped = null;
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

    public void BeginDropped(DroppedResource dropped)
    {
        Cancel();

        pickupDropped = dropped;
        pickupPebble = null;
        pickupPile = null;
        jobs.SetLookAt(dropped.transform.position);

        Vector2 stand = mover.GetStandPosition(dropped.transform.position, mover.defaultWorkOffset);

        anim.SetWalking();
        anim.FaceToward(dropped.transform.position, mover.Position);

        mover.MoveTo(stand, () =>
        {
            jobs.SetStateCollecting();
            routine = StartCoroutine(CollectDroppedThenReturn());
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
            carryingSprite = pickupPile.carrySprite;

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
            carryingSprite = carrySprite;

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

    private IEnumerator CollectFromPileThenReturnContinuous()
    {
        while (pickupPile != null && !pickupPile.IsEmpty)
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
                carryingSprite = pickupPile.carrySprite;

                carryVis.ShowCarried(pickupPile.carrySprite);
                anim.SetCarrying(true);
            }

            anim.StopCollecting();

            jobs.SetStateReturning();

            Vector2 drop = jobs.dropoffPoint ? (Vector2)jobs.dropoffPoint.position : Vector2.zero;
            jobs.SetLookAt(drop);

            if (carryingAmount > 0) anim.PlayWalkCarrying();
            else anim.SetWalking();

            bool reachedDropoff = false;
            mover.MoveTo(drop, () => { reachedDropoff = true; });

            // Wait for the penguin to reach dropoff
            while (!reachedDropoff)
                yield return null;

            // Don't clear pile reference when depositing during continuous collection
            DepositCarried(clearReferences: false);

            // Check if pile still has items before looping
            if (pickupPile == null || pickupPile.IsEmpty)
            {
                pickupPile = null;
                jobs.SetStateIdle();
                break;
            }

            // Move back to pile for next collection
            jobs.SetStateCollecting();
            Vector2 stand = mover.GetStandPosition(pickupPile.transform.position, mover.defaultWorkOffset);
            jobs.SetLookAt(pickupPile.transform.position);
            anim.SetWalking();
            anim.FaceToward(pickupPile.transform.position, mover.Position);

            bool reachedPile = false;
            mover.MoveTo(stand, () => { reachedPile = true; });

            // Wait for the penguin to reach the pile
            while (!reachedPile)
                yield return null;
        }

        pickupPile = null;
        jobs.SetStateIdle();
    }

    private IEnumerator CollectDroppedThenReturn()
    {
        anim.PlayCollecting();
        yield return new WaitForSeconds(jobs.collectHold);

        Sprite carrySprite = null;
        ResourceType resourceType = ResourceType.Food;
        bool picked = false;

        if (pickupDropped != null && !pickupDropped.IsPickedUp)
            picked = pickupDropped.TryPickup(out carrySprite, out resourceType);

        if (picked)
        {
            carryingAmount = 1;
            carryingType = resourceType;
            carryingSprite = carrySprite;

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

    private void DepositCarried(bool clearReferences = true)
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

        if (clearReferences)
        {
            pickupPile = null;
            pickupPebble = null;
            pickupDropped = null;
        }

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

        // Drop the item if carrying something
        if (carryingAmount > 0 && droppedResourcePrefab != null)
        {
            DropCarriedItem();
        }

        carryingAmount = 0;
        carryingType = ResourceType.Food;
        carryingSprite = null;
        pickupPile = null;
        pickupPebble = null;
        pickupDropped = null;

        if (carryVis != null) carryVis.HideCarried();
        if (anim != null) anim.SetCarrying(false);
    }

    private void DropCarriedItem()
    {
        if (carryingAmount <= 0 || droppedResourcePrefab == null)
            return;

        Vector2 dropPosition = mover.Position;
        GameObject droppedObj = Instantiate(droppedResourcePrefab, dropPosition, Quaternion.identity);

        // Make sure it's on the default layer so raycasts can hit it
        droppedObj.layer = 0;

        // Ensure it has a Rigidbody2D for collider detection
        var rb = droppedObj.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = droppedObj.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        // Ensure collider is set as trigger so penguin can walk over it
        var collider = droppedObj.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        var droppedResource = droppedObj.GetComponent<DroppedResource>();
        if (droppedResource != null)
        {
            droppedResource.Initialize(carryingType, carryingSprite);
        }
    }
}