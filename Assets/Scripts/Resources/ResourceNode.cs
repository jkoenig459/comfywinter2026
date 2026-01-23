using UnityEngine;
using System.Collections.Generic;

public enum ResourceType { Ice, Food, Pebble }

public class ResourceNode : MonoBehaviour
{
    public ResourceType type;
    public float workTime = 2f;   // seconds to gather
    public int yieldAmount = 1;   // per trip

    [Header("Worker Limits")]
    public int maxWorkers = 2;

    [Header("Ice Node Worker Positions (Optional)")]
    [Tooltip("Child transform for left worker position. Used for ice nodes.")]
    public Transform leftWorker;
    [Tooltip("Child transform for right worker position. Used for ice nodes.")]
    public Transform rightWorker;

    private List<PenguinJobs> activeWorkers = new List<PenguinJobs>();
    private Dictionary<PenguinJobs, Transform> workerPositions = new Dictionary<PenguinJobs, Transform>();

    public bool CanAcceptWorker => activeWorkers.Count < maxWorkers;
    public int WorkerCount => activeWorkers.Count;

    public bool TryRegisterWorker(PenguinJobs penguin, out Transform workerPosition)
    {
        workerPosition = null;

        if (!CanAcceptWorker || penguin == null)
            return false;

        if (!activeWorkers.Contains(penguin))
        {
            activeWorkers.Add(penguin);

            // Assign worker position if ice node has worker transforms
            if (type == ResourceType.Ice && leftWorker != null && rightWorker != null)
            {
                // Check which position is available
                bool leftOccupied = workerPositions.ContainsValue(leftWorker);
                bool rightOccupied = workerPositions.ContainsValue(rightWorker);

                if (!leftOccupied)
                {
                    workerPosition = leftWorker;
                    workerPositions[penguin] = leftWorker;
                }
                else if (!rightOccupied)
                {
                    workerPosition = rightWorker;
                    workerPositions[penguin] = rightWorker;
                }
            }
        }

        return true;
    }

    public void UnregisterWorker(PenguinJobs penguin)
    {
        if (penguin != null)
        {
            activeWorkers.Remove(penguin);
            workerPositions.Remove(penguin);
        }
    }

    public bool IsFirstWorker(PenguinJobs penguin)
    {
        return activeWorkers.Count > 0 && activeWorkers[0] == penguin;
    }

    public PenguinJobs GetFirstWorker()
    {
        return activeWorkers.Count > 0 ? activeWorkers[0] : null;
    }

    private void Update()
    {
        // Clean up null references (if penguins were destroyed)
        activeWorkers.RemoveAll(w => w == null);

        // Clean up null keys in dictionary
        var nullKeys = new List<PenguinJobs>();
        foreach (var kvp in workerPositions)
        {
            if (kvp.Key == null)
                nullKeys.Add(kvp.Key);
        }
        foreach (var key in nullKeys)
        {
            workerPositions.Remove(key);
        }
    }
}