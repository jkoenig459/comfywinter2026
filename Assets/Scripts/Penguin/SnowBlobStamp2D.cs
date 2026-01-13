using UnityEngine;

public class SnowBlobStamps2D : MonoBehaviour
{
    [Header("Required")]
    [SerializeField] private ParticleSystem stampsPS;

    [Header("Stamping")]
    [Tooltip("Distance traveled between stamps. Smaller = smoother line, more particles.")]
    [SerializeField] private float spacing = 0.08f;

    [Tooltip("Ignore tiny jitter when idle.")]
    [SerializeField] private float minSpeed = 0.02f;

    [Header("Look")]
    [SerializeField] private float lifetime = 6f;
    [SerializeField] private float size = 0.22f;

    [Range(0f, 1f)]
    [SerializeField] private float alpha = 0.65f;

    [Tooltip("Small random scatter so it doesn't look like perfect beads.")]
    [SerializeField] private float jitter = 0.01f;

    private Vector3 lastPos;
    private float distAcc;

    private void Awake()
    {
        if (!stampsPS)
        {
            Debug.LogError("SnowBlobStamps2D: stampsPS not assigned.", this);
            enabled = false;
            return;
        }

        lastPos = transform.position;

        var main = stampsPS.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0f;

        var emission = stampsPS.emission;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 0f;
    }

    private void Update()
    {
        Vector3 pos = transform.position;
        Vector3 delta = pos - lastPos;

        float moved = delta.magnitude;
        float speed = moved / Mathf.Max(Time.deltaTime, 0.0001f);

        if (speed < minSpeed)
        {
            lastPos = pos;
            return;
        }

        distAcc += moved;
        lastPos = pos;

        while (distAcc >= spacing)
        {
            distAcc -= spacing;
            EmitOne(pos);
        }
    }

    private void EmitOne(Vector3 pos)
    {
        Vector2 j = (jitter > 0f) ? Random.insideUnitCircle * jitter : Vector2.zero;

        var ep = new ParticleSystem.EmitParams
        {
            position = pos + (Vector3)j,
            startLifetime = lifetime,
            startSize = size,
            startColor = new Color(1f, 1f, 1f, alpha),
            rotation = 0f
        };

        stampsPS.Emit(ep, 1);
    }
}
