using UnityEngine;

public class PenguinManager : MonoBehaviour
{
    public static PenguinManager I { get; private set; }

    [Header("Shared References")]
    public Transform dropoffPoint;
    public ResourcePile fishPilePrefab;
    public ResourcePile icePilePrefab;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
    }

    public void InitializePenguin(PenguinJobs penguin)
    {
        if (penguin == null) return;

        penguin.dropoffPoint = dropoffPoint;
        penguin.fishPilePrefab = fishPilePrefab;
        penguin.icePilePrefab = icePilePrefab;
    }
}