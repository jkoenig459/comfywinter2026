using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PenguinFishJob : MonoBehaviour
{
    private PenguinJobs jobs;
    private PenguinMover mover;
    private PenguinAnimator anim;

    private Transform node;
    private ResourcePile pile;
    private Coroutine routine;

    public void Initialize(PenguinJobs jobs, PenguinMover mover, PenguinAnimator anim)
    {
        this.jobs = jobs;
        this.mover = mover;
        this.anim = anim;
    }

    public void Begin(Transform nodeTransform)
    {
        Cancel();

        node = nodeTransform;
        jobs.SetLookAt(node.position);

        pile = jobs.GetOrCreatePileAt(node.position, jobs.fishPilePrefab, jobs.fishPileOffset);

        Vector2 stand = mover.GetStandPosition(node.position, mover.fishingOffset);

        anim.SetWalking();
        anim.FaceToward(node.position, mover.Position);

        mover.MoveTo(stand, () =>
        {
            jobs.SetStateFishing();
            anim.SetFishingLoop();
            routine = StartCoroutine(FishLoop());
        });
    }

    private IEnumerator FishLoop()
    {
        while (jobs.IsFishingState)
        {
            if (pile != null && pile.IsFull)
            {
                yield return null;
                continue;
            }

            yield return new WaitForSeconds(jobs.fishInterval);
            if (!jobs.IsFishingState) yield break;

            anim.TriggerCatchFish();

            if (jobs.fishCaughtAnimDuration > 0f)
                yield return new WaitForSeconds(jobs.fishCaughtAnimDuration);

            if (!jobs.IsFishingState) yield break;

            if (jobs.fishAddToPileDelay > 0f)
                yield return new WaitForSeconds(jobs.fishAddToPileDelay);

            if (!jobs.IsFishingState) yield break;

            if (pile != null)
                pile.Add(1);
        }
    }

    public void Cancel()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        node = null;
        pile = null;
    }
}
