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

    private State state = State.Idle;
    private ResourceNode targetNode;
    private Vector3 targetPos;

    private void Update()
    {
        // Pause freezes timeScale, so Update still runs, but movement timers won't advance
        // because we're using Time.deltaTime. That's good.
        switch (state)
        {
            case State.Idle:
                // Do nothing (later: wander)
                break;

            case State.MovingToTarget:
                MoveTowards(targetPos, onArrive: () =>
                {
                    state = State.Working;
                    workTimer = 0f;
                });
                break;

            case State.Working:
                if (targetNode == null) { state = State.Idle; break; }

                workTimer += Time.deltaTime;
                if (workTimer >= targetNode.workTime)
                {
                    carrying = targetNode.yieldAmount;
                    state = State.Returning;
                    targetPos = dropoffPoint != null ? dropoffPoint.position : Vector3.zero;
                }
                break;

            case State.Returning:
                MoveTowards(targetPos, onArrive: DepositAndIdle);
                break;
        }
    }

    private void MoveTowards(Vector3 dest, System.Action onArrive)
    {
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
    }

    public void AssignGather(ResourceNode node)
    {
        if (node == null) return;

        targetNode = node;
        targetPos = node.transform.position;
        state = State.MovingToTarget;
    }

    public void AssignMove(Vector3 worldPos)
    {
        targetNode = null;
        carrying = 0;
        targetPos = worldPos;
        state = State.MovingToTarget;
    }
}
