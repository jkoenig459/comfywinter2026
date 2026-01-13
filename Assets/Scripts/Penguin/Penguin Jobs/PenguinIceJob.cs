using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PenguinIceJob : MonoBehaviour
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

        pile = jobs.GetOrCreatePileAt(node.position, jobs.icePilePrefab, jobs.icePileOffset);

        Vector2 stand = mover.GetStandPosition(node.position, mover.iceOffset);

        anim.SetWalking();
        anim.FaceToward(node.position, mover.Position);

        mover.MoveTo(stand, () =>
        {
            jobs.SetStateCuttingIce();
            anim.SetCuttingLoop();
            routine = StartCoroutine(IceLoop());
        });
    }

    private IEnumerator IceLoop()
    {
        while (jobs.IsCuttingState)
        {
            if (pile != null && pile.IsFull)
            {
                yield return null;
                continue;
            }

            yield return new WaitForSeconds(jobs.iceInterval);
            if (!jobs.IsCuttingState) yield break;

            anim.TriggerFinishCut();

            if (jobs.iceFinishedAnimDuration > 0f)
                yield return new WaitForSeconds(jobs.iceFinishedAnimDuration);

            if (!jobs.IsCuttingState) yield break;

            anim.SetCuttingLoop();

            if (jobs.iceAddToPileDelay > 0f)
                yield return new WaitForSeconds(jobs.iceAddToPileDelay);

            if (!jobs.IsCuttingState) yield break;

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
