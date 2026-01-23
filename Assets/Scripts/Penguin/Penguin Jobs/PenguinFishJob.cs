using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PenguinFishJob : MonoBehaviour
{
    [Header("Sound Timing")]
    [Tooltip("Delay before playing the fishing sound after arriving at spot.")]
    public float fishingSoundDelay = 0f;

    private PenguinJobs jobs;
    private PenguinMover mover;
    private PenguinAnimator anim;
    private PenguinAudio penguinAudio;

    private Transform node;
    private ResourceNode resourceNode;
    private ResourcePile pile;
    private Coroutine routine;

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

        // Check if node can accept more workers
        if (resourceNode != null && !resourceNode.CanAcceptWorker)
        {
            // Node is full, can't work here
            jobs.SetStateIdle();
            return;
        }

        // Register this penguin with the node
        Transform workerPos = null;
        if (resourceNode != null)
        {
            resourceNode.TryRegisterWorker(jobs, out workerPos);
        }

        jobs.SetLookAt(node.position);

        pile = jobs.GetOrCreatePileAt(node.position, jobs.fishPilePrefab, jobs.fishPileOffset);

        // Determine position based on worker count
        Vector2 stand = GetWorkPosition();

        anim.SetWalking();
        anim.FaceToward(node.position, mover.Position);

        mover.MoveTo(stand, () =>
        {
            jobs.SetStateFishing();
            anim.SetFishingLoop();
            routine = StartCoroutine(FishLoop());
        });
    }

    private Vector2 GetWorkPosition()
    {
        // If there's already a worker at this node, position on opposite side
        if (resourceNode != null && !resourceNode.IsFirstWorker(jobs))
        {
            // Second worker - flip to opposite side
            Vector2 baseOffset = mover.fishingOffset;
            Vector2 flippedOffset = new Vector2(-baseOffset.x, baseOffset.y);
            return mover.GetStandPosition(node.position, flippedOffset);
        }
        else
        {
            // First worker - use normal position
            return mover.GetStandPosition(node.position, mover.fishingOffset);
        }
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

            // Play cast sound at start of cycle with optional delay
            if (fishingSoundDelay > 0f)
                yield return new WaitForSeconds(fishingSoundDelay);

            if (!jobs.IsFishingState) yield break;

            if (penguinAudio != null)
                penguinAudio.PlayFishingSound();

            float remainingWait = Mathf.Max(0f, jobs.fishInterval - fishingSoundDelay);
            if (remainingWait > 0f)
                yield return new WaitForSeconds(remainingWait);

            if (!jobs.IsFishingState) yield break;

            anim.TriggerCatchFish();

            if (jobs.fishCaughtAnimDuration > 0f)
                yield return new WaitForSeconds(jobs.fishCaughtAnimDuration);

            if (!jobs.IsFishingState) yield break;

            if (jobs.fishAddToPileDelay > 0f)
                yield return new WaitForSeconds(jobs.fishAddToPileDelay);

            if (!jobs.IsFishingState) yield break;

            if (pile != null)
            {
                pile.Add(1);

                if (AudioManager.I != null)
                    AudioManager.I.PlayPileFish();
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

        // Unregister from the node
        if (resourceNode != null && jobs != null)
        {
            resourceNode.UnregisterWorker(jobs);
        }

        node = null;
        resourceNode = null;
        pile = null;
    }
}