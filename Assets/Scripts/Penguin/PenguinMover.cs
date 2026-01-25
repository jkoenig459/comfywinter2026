using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class PenguinMover : MonoBehaviour
{
    public float moveSpeed = 2f;

    [Header("Stand Offsets (world units)")]
    public Vector2 defaultWorkOffset = new Vector2(0.35f, -0.12f);
    public Vector2 fishingOffset = new Vector2(0.35f, -0.12f);
    public Vector2 iceOffset = new Vector2(0.30f, -0.05f);

    [Header("Pathfinding Settings")]
    [Tooltip("Only objects on this layer will be pathed around (e.g., Buildings)")]
    [SerializeField] private LayerMask buildingsLayer;
    [SerializeField] private float gridCellSize = 0.4f;
    [SerializeField] private float penguinRadius = 0.25f;
    [SerializeField] private float pathRecalculateInterval = 0.3f;
    [SerializeField] private float gridPadding = 8f;
    [SerializeField] private int maxIterations = 2000;

    private bool ignoreCollisions = false;

    private Rigidbody2D rb;
    private Vector2 moveTarget;
    private bool hasTarget;
    private System.Action onArrive;
    private List<Vector2> currentPath = new List<Vector2>();
    private int currentWaypointIndex;
    private float pathRecalculateTimer;

    public Vector2 Velocity { get; private set; }

    #region A* Pathfinding

    private class PathNode
    {
        public int x, y;
        public bool walkable;
        public float gCost;
        public float hCost;
        public float fCost => gCost + hCost;
        public PathNode parent;
        public Vector2 worldPosition;

        public PathNode(int x, int y, bool walkable, Vector2 worldPos)
        {
            this.x = x;
            this.y = y;
            this.walkable = walkable;
            this.worldPosition = worldPos;
            this.gCost = float.MaxValue;
            this.hCost = 0;
            this.parent = null;
        }
    }

    private class MinHeap
    {
        private List<PathNode> nodes = new List<PathNode>();
        private Dictionary<PathNode, int> nodeIndices = new Dictionary<PathNode, int>();

        public int Count => nodes.Count;

        public void Add(PathNode node)
        {
            nodes.Add(node);
            nodeIndices[node] = nodes.Count - 1;
            BubbleUp(nodes.Count - 1);
        }

        public PathNode RemoveMin()
        {
            if (nodes.Count == 0) return null;

            PathNode min = nodes[0];
            nodeIndices.Remove(min);

            if (nodes.Count > 1)
            {
                nodes[0] = nodes[nodes.Count - 1];
                nodeIndices[nodes[0]] = 0;
                nodes.RemoveAt(nodes.Count - 1);
                BubbleDown(0);
            }
            else
            {
                nodes.RemoveAt(0);
            }

            return min;
        }

        public bool Contains(PathNode node) => nodeIndices.ContainsKey(node);

        public void UpdatePriority(PathNode node)
        {
            if (nodeIndices.TryGetValue(node, out int index))
            {
                BubbleUp(index);
                BubbleDown(index);
            }
        }

        private void BubbleUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (nodes[index].fCost >= nodes[parent].fCost) break;
                Swap(index, parent);
                index = parent;
            }
        }

        private void BubbleDown(int index)
        {
            while (true)
            {
                int left = 2 * index + 1;
                int right = 2 * index + 2;
                int smallest = index;

                if (left < nodes.Count && nodes[left].fCost < nodes[smallest].fCost)
                    smallest = left;
                if (right < nodes.Count && nodes[right].fCost < nodes[smallest].fCost)
                    smallest = right;

                if (smallest == index) break;
                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            PathNode temp = nodes[a];
            nodes[a] = nodes[b];
            nodes[b] = temp;
            nodeIndices[nodes[a]] = a;
            nodeIndices[nodes[b]] = b;
        }
    }

    #endregion

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        if (!hasTarget)
        {
            Velocity = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 pos = rb.position;
        float distanceToTarget = (moveTarget - pos).magnitude;

        if ((pos - moveTarget).sqrMagnitude <= 0.01f)
        {
            hasTarget = false;
            Velocity = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            currentPath.Clear();

            var cb = onArrive;
            onArrive = null;
            cb?.Invoke();
            return;
        }

        pathRecalculateTimer -= Time.fixedDeltaTime;
        bool pathBlocked = IsCurrentPathBlockedByBuilding();

        if (pathRecalculateTimer <= 0f || pathBlocked)
        {
            pathRecalculateTimer = pathRecalculateInterval;
            RecalculatePath();
        }

        FollowPath();
    }

    private bool IsCurrentPathBlockedByBuilding()
    {
        if (currentPath == null || currentPath.Count == 0 || currentWaypointIndex >= currentPath.Count)
            return false;

        Vector2 nextWaypoint = currentPath[currentWaypointIndex];
        return !HasClearPathFromBuildings(rb.position, nextWaypoint);
    }

    private bool HasClearPathFromBuildings(Vector2 from, Vector2 to)
    {
        if (ignoreCollisions) return true;

        Vector2 direction = to - from;
        float distance = direction.magnitude;

        if (distance < 0.01f) return true;

        RaycastHit2D hit = Physics2D.CircleCast(from, penguinRadius * 0.8f, direction.normalized, distance, buildingsLayer);
        return hit.collider == null;
    }

    private void FollowPath()
    {
        Vector2 pos = rb.position;
        Vector2 directionToTarget = (moveTarget - pos).normalized;

        if (currentPath == null || currentPath.Count == 0 || currentWaypointIndex >= currentPath.Count)
        {
            Vector2 nextPos = pos + directionToTarget * moveSpeed * Time.fixedDeltaTime;
            Velocity = (nextPos - pos) / Time.fixedDeltaTime;
            transform.position = new Vector3(nextPos.x, nextPos.y, transform.position.z);
            return;
        }

        for (int i = currentPath.Count - 1; i > currentWaypointIndex; i--)
        {
            if (HasClearPathFromBuildings(pos, currentPath[i]))
            {
                currentWaypointIndex = i;
                break;
            }
        }

        if (HasClearPathFromBuildings(pos, moveTarget))
        {
            currentPath.Clear();
            Vector2 nextPos = pos + directionToTarget * moveSpeed * Time.fixedDeltaTime;
            Velocity = (nextPos - pos) / Time.fixedDeltaTime;
            transform.position = new Vector3(nextPos.x, nextPos.y, transform.position.z);
            return;
        }

        if (currentWaypointIndex >= currentPath.Count)
        {
            Vector2 nextPos = pos + directionToTarget * moveSpeed * Time.fixedDeltaTime;
            Velocity = (nextPos - pos) / Time.fixedDeltaTime;
            transform.position = new Vector3(nextPos.x, nextPos.y, transform.position.z);
            return;
        }

        Vector2 currentWaypoint = currentPath[currentWaypointIndex];
        float distanceToWaypoint = Vector2.Distance(pos, currentWaypoint);

        if (distanceToWaypoint <= 0.2f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= currentPath.Count)
            {
                Vector2 nextPos = pos + directionToTarget * moveSpeed * Time.fixedDeltaTime;
                Velocity = (nextPos - pos) / Time.fixedDeltaTime;
                transform.position = new Vector3(nextPos.x, nextPos.y, transform.position.z);
                return;
            }
            currentWaypoint = currentPath[currentWaypointIndex];
        }

        Vector2 direction = (currentWaypoint - pos).normalized;
        Vector2 next = pos + direction * moveSpeed * Time.fixedDeltaTime;
        Velocity = (next - pos) / Time.fixedDeltaTime;
        transform.position = new Vector3(next.x, next.y, transform.position.z);
    }

    private void RecalculatePath()
    {
        if (HasClearPathFromBuildings(rb.position, moveTarget))
        {
            currentPath.Clear();
            currentWaypointIndex = 0;
            return;
        }

        currentPath = FindPath(rb.position, moveTarget);
        currentWaypointIndex = 0;
    }

    private List<Vector2> FindPath(Vector2 startPos, Vector2 endPos)
    {
        float minX = Mathf.Min(startPos.x, endPos.x) - gridPadding;
        float maxX = Mathf.Max(startPos.x, endPos.x) + gridPadding;
        float minY = Mathf.Min(startPos.y, endPos.y) - gridPadding;
        float maxY = Mathf.Max(startPos.y, endPos.y) + gridPadding;

        Vector2 gridOrigin = new Vector2(minX, minY);
        int gridWidth = Mathf.CeilToInt((maxX - minX) / gridCellSize) + 1;
        int gridHeight = Mathf.CeilToInt((maxY - minY) / gridCellSize) + 1;

        gridWidth = Mathf.Min(gridWidth, 150);
        gridHeight = Mathf.Min(gridHeight, 150);

        PathNode[,] grid = new PathNode[gridWidth, gridHeight];
        float checkRadius = penguinRadius + gridCellSize * 0.25f;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2 worldPos = gridOrigin + new Vector2(x * gridCellSize, y * gridCellSize);
                bool walkable = ignoreCollisions || !Physics2D.OverlapCircle(worldPos, checkRadius, buildingsLayer);
                grid[x, y] = new PathNode(x, y, walkable, worldPos);
            }
        }

        PathNode startNode = GetNodeFromWorld(grid, startPos, gridOrigin, gridWidth, gridHeight);
        PathNode endNode = GetNodeFromWorld(grid, endPos, gridOrigin, gridWidth, gridHeight);

        if (startNode == null || endNode == null)
            return new List<Vector2>();

        startNode.walkable = true;
        endNode.walkable = true;

        List<Vector2> path = AStar(grid, startNode, endNode, gridWidth, gridHeight);

        if (path.Count > 0)
        {
            path = SmoothPath(path);
        }

        return path;
    }

    private PathNode GetNodeFromWorld(PathNode[,] grid, Vector2 worldPos, Vector2 origin, int width, int height)
    {
        int x = Mathf.RoundToInt((worldPos.x - origin.x) / gridCellSize);
        int y = Mathf.RoundToInt((worldPos.y - origin.y) / gridCellSize);

        x = Mathf.Clamp(x, 0, width - 1);
        y = Mathf.Clamp(y, 0, height - 1);

        return grid[x, y];
    }

    private List<Vector2> AStar(PathNode[,] grid, PathNode startNode, PathNode endNode, int gridWidth, int gridHeight)
    {
        MinHeap openSet = new MinHeap();
        HashSet<PathNode> closedSet = new HashSet<PathNode>();

        startNode.gCost = 0;
        startNode.hCost = Heuristic(startNode, endNode);
        openSet.Add(startNode);

        int iterations = 0;

        int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
        int[] dy = { 1, 1, 0, -1, -1, -1, 0, 1 };
        float[] costs = { 1f, 1.414f, 1f, 1.414f, 1f, 1.414f, 1f, 1.414f };

        while (openSet.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            PathNode current = openSet.RemoveMin();

            if (current == endNode)
            {
                return RetracePath(startNode, endNode);
            }

            closedSet.Add(current);

            for (int i = 0; i < 8; i++)
            {
                int nx = current.x + dx[i];
                int ny = current.y + dy[i];

                if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight)
                    continue;

                PathNode neighbor = grid[nx, ny];

                if (!neighbor.walkable || closedSet.Contains(neighbor))
                    continue;

                // Prevent corner cutting
                if (i % 2 == 1)
                {
                    bool cardinalX = grid[current.x + dx[i], current.y].walkable;
                    bool cardinalY = grid[current.x, current.y + dy[i]].walkable;
                    if (!cardinalX || !cardinalY)
                        continue;
                }

                float newGCost = current.gCost + costs[i] * gridCellSize;

                if (newGCost < neighbor.gCost)
                {
                    neighbor.gCost = newGCost;
                    neighbor.hCost = Heuristic(neighbor, endNode);
                    neighbor.parent = current;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                    else
                    {
                        openSet.UpdatePriority(neighbor);
                    }
                }
            }
        }

        return new List<Vector2>();
    }

    private float Heuristic(PathNode a, PathNode b)
    {
        float dx = Mathf.Abs(a.worldPosition.x - b.worldPosition.x);
        float dy = Mathf.Abs(a.worldPosition.y - b.worldPosition.y);
        return Mathf.Max(dx, dy) + (1.414f - 1f) * Mathf.Min(dx, dy);
    }

    private List<Vector2> RetracePath(PathNode startNode, PathNode endNode)
    {
        List<Vector2> path = new List<Vector2>();
        PathNode current = endNode;

        while (current != startNode && current != null)
        {
            path.Add(current.worldPosition);
            current = current.parent;
        }

        path.Reverse();
        return path;
    }

    private List<Vector2> SmoothPath(List<Vector2> path)
    {
        if (path.Count <= 2)
            return path;

        List<Vector2> smoothed = new List<Vector2>();
        int currentIndex = 0;

        for (int i = path.Count - 1; i >= 0; i--)
        {
            if (HasClearPathFromBuildings(rb.position, path[i]))
            {
                currentIndex = i;
                break;
            }
        }

        smoothed.Add(path[currentIndex]);

        while (currentIndex < path.Count - 1)
        {
            Vector2 from = path[currentIndex];
            int furthestVisible = currentIndex + 1;

            for (int i = path.Count - 1; i > currentIndex + 1; i--)
            {
                if (HasClearPathFromBuildings(from, path[i]))
                {
                    furthestVisible = i;
                    break;
                }
            }

            smoothed.Add(path[furthestVisible]);
            currentIndex = furthestVisible;
        }

        return smoothed;
    }

    public void MoveTo(Vector2 worldPos, System.Action arriveCallback = null)
    {
        Collider2D buildingAtTarget = Physics2D.OverlapCircle(worldPos, 0.1f, buildingsLayer);

        if (buildingAtTarget != null)
        {
            worldPos = FindNearestWalkablePosition(worldPos, buildingAtTarget);
        }

        moveTarget = worldPos;
        hasTarget = true;
        onArrive = arriveCallback;
        pathRecalculateTimer = 0f;
        RecalculatePath();
    }

    private Vector2 FindNearestWalkablePosition(Vector2 targetPos, Collider2D building)
    {
        float searchRadius = 0.5f;
        int searchSteps = 16;

        for (int ring = 1; ring <= 5; ring++)
        {
            float currentRadius = searchRadius * ring;

            for (int i = 0; i < searchSteps; i++)
            {
                float angle = (i / (float)searchSteps) * 360f * Mathf.Deg2Rad;
                Vector2 testPos = targetPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * currentRadius;

                if (!Physics2D.OverlapCircle(testPos, penguinRadius, buildingsLayer))
                {
                    return testPos;
                }
            }
        }

        return targetPos;
    }

    public void Stop()
    {
        hasTarget = false;
        onArrive = null;
        Velocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        currentPath.Clear();
    }

    public Vector2 GetStandPosition(Vector2 targetPos, Vector2 offset)
    {
        float dx = targetPos.x - rb.position.x;
        float xSign = (dx >= 0f) ? -1f : 1f;
        return targetPos + new Vector2(offset.x * xSign, offset.y);
    }

    public Vector2 GetStandPosition(Vector2 targetPos)
    {
        return GetStandPosition(targetPos, defaultWorkOffset);
    }

    public Vector2 Position => rb.position;

    public void SetIgnoreCollisions(bool ignore)
    {
        ignoreCollisions = ignore;
    }

    public void LockPhysics()
    {
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    public void UnlockPhysics()
    {
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.green;
            Vector2 prev = rb.position;

            for (int i = currentWaypointIndex; i < currentPath.Count; i++)
            {
                Gizmos.DrawLine(prev, currentPath[i]);
                prev = currentPath[i];
            }

            if (hasTarget)
            {
                Gizmos.DrawLine(prev, moveTarget);
            }

            Gizmos.color = Color.cyan;
            for (int i = currentWaypointIndex; i < currentPath.Count; i++)
            {
                Gizmos.DrawWireSphere(currentPath[i], 0.1f);
            }
        }

        if (hasTarget)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(moveTarget, 0.15f);
        }
    }
}
