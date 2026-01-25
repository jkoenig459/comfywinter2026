using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PenguinIceJob : MonoBehaviour
{
    [Header("Sound Timing")]
    [Tooltip("Delay before playing the ice breaking sound after arriving at spot.")]
    public float iceBreakingSoundDelay = 0f;

    private PenguinJobs jobs;
    private PenguinMover mover;
    private PenguinAnimator anim;
    private PenguinAudio penguinAudio;

    private Transform node;
    private ResourceNode resourceNode;
    private ResourcePile pile;
    private Coroutine routine;
    private Transform assignedWorkerPosition;

    public void Initialize(PenguinJobs jobs, PenguinMover mover, PenguinAnimator anim)
    {
        this.jobs = jobs;
        this.mover = mover;
        this.anim = anim;
        this.penguinAudio = GetComponent<PenguinAudio>();
    }

    public void Begin(Transform nodeTransform)
    {
        Cancel();

        node = nodeTransform;
        resourceNode = node.GetComponent<ResourceNode>();

        if (resourceNode != null && !resourceNode.CanAcceptWorker)
        {
            jobs.SetStateIdle();
            return;
        }

        Transform workerPos = null;
        if (resourceNode != null)
        {
            resourceNode.TryRegisterWorker(jobs, out workerPos);
            assignedWorkerPosition = workerPos;
        }

        jobs.SetLookAt(node.position);

        pile = jobs.GetOrCreatePileAt(node.position, jobs.icePilePrefab, jobs.icePileOffset);

        Vector2 stand = GetWorkPosition();

        anim.SetWalking();
        anim.FaceToward(node.position, mover.Position);

        mover.MoveTo(stand, () =>
        {
            jobs.SetStateCuttingIce();
            anim.SetCuttingLoop();
            routine = StartCoroutine(IceLoop());
        });
    }

    private Vector2 GetWorkPosition()
    {
        if (assignedWorkerPosition != null)
        {
            return assignedWorkerPosition.position;
        }

        if (resourceNode != null && !resourceNode.IsFirstWorker(jobs))
        {
            Vector2 baseOffset = mover.iceOffset;
            Vector2 flippedOffset = new Vector2(-baseOffset.x, baseOffset.y);
            return mover.GetStandPosition(node.position, flippedOffset);
        }
        else
        {
            return mover.GetStandPosition(node.position, mover.iceOffset);
        }
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

            if (iceBreakingSoundDelay > 0f)
                yield return new WaitForSeconds(iceBreakingSoundDelay);

            if (!jobs.IsCuttingState) yield break;

            if (penguinAudio != null)
                penguinAudio.PlayIceBreakingSound();

            float remainingWait = Mathf.Max(0f, jobs.iceInterval - iceBreakingSoundDelay);
            if (remainingWait > 0f)
                yield return new WaitForSeconds(remainingWait);

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
            {
                pile.Add(1);

                if (AudioManager.I != null)
                    AudioManager.I.PlayPileIce();
            }
        }
    }

    public void Cancel()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (resourceNode != null && jobs != null)
        {
            resourceNode.UnregisterWorker(jobs);
        }

        node = null;
        resourceNode = null;
        pile = null;
        assignedWorkerPosition = null;
    }
}