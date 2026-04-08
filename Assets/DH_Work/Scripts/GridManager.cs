using UnityEngine;

public class GridManager : MonoBehaviour
{
    private static readonly Vector2Int[] directions8 =
    {
        new Vector2Int(0, 1),
        new Vector2Int(1, 1),
        new Vector2Int(1, 0),
        new Vector2Int(1, -1),
        new Vector2Int(0, -1),
        new Vector2Int(-1, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(-1, 1)
    };

    [Header("Grid 기준 오브젝트")]
    [SerializeField] private Transform landTransform;

    [Header("Grid Settings")]
    [SerializeField] private float hexRadius = 1f;

    [Header("Obstacle Settings")]
    [Tooltip("이 레이어에 있는 콜라이더는 장애물로 간주합니다.")]
    [SerializeField] private LayerMask obstacleLayerMask;
    [SerializeField] private LayerMask itemLayerMask;
    [SerializeField] private LayerMask factoryLayerMask;
    [Tooltip("셀 워커블 검사 시, 셀 크기 대비 체크 박스 비율(너무 크면 오탐, 너무 작으면 통과).")]
    [SerializeField, Range(0.1f, 1f)] private float obstacleCheckFill = 0.9f;

    [Header("Debug Gizmos")]
    [SerializeField] private int debugQMin = -15;
    [SerializeField] private int debugQMax = 15;
    [SerializeField] private int debugRMin = -15;
    [SerializeField] private int debugRMax = 15;
    [SerializeField, Range(0.01f, 0.5f)] private float debugCenterSphereRadius = 0.08f;

    // 전제 조건: Land 월드 중심 = (0, 0, 0)
    // 필요 시 인스펙터에서 원점 오프셋 확장 가능
    private Vector3 gridOrigin = Vector3.zero;

    public float HexRadius => hexRadius;
    public Transform LandTransform => landTransform;
    public static Vector2Int[] Directions8 => directions8;

    private void Awake()
    {
        if (hexRadius <= 0f)
            hexRadius = 1f;
    }

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        float localX = worldPosition.x - gridOrigin.x;
        float localZ = worldPosition.z - gridOrigin.z;

        int x = Mathf.RoundToInt(localX / hexRadius);
        int y = Mathf.RoundToInt(localZ / hexRadius);
        return new Vector2Int(x, y);
    }

    public Vector3 GridToWorldCenter(Vector2Int grid)
    {
        float x = gridOrigin.x + hexRadius * grid.x;
        float z = gridOrigin.z + hexRadius * grid.y;

        return new Vector3(x, 0f, z);
    }

    public static int GridDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
    }

    public bool IsWalkable(Vector2Int grid)
    {
        return !HasObstacle(grid) && !HasItem(grid) && !HasFactory(grid);
    }

    public bool HasObstacle(Vector2Int grid)
    {
        return HasBlockingCollider(grid, obstacleLayerMask);
    }

    public bool HasItem(Vector2Int grid)
    {
        return HasBlockingCollider(grid, itemLayerMask);
    }

    public bool HasFactory(Vector2Int grid)
    {
        return HasBlockingCollider(grid, factoryLayerMask);
    }

    public bool HasItemOrFactory(Vector2Int grid)
    {
        return HasItem(grid) || HasFactory(grid);
    }
    public bool TryGetAdjacentItemGrid(Vector2Int grid, out Vector2Int itemGrid)
    {
        for (int i = 0; i < directions8.Length; i++)
        {
            Vector2Int candidate = grid + directions8[i];
            if (HasItem(candidate))
            {
                itemGrid = candidate;
                return true;
            }
        }

        itemGrid = grid;
        return false;
    }

    public bool TryGetAdjacentFactoryGrid(Vector2Int grid, out Vector2Int factoryGrid)
    {
        for (int i = 0; i < directions8.Length; i++)
        {
            Vector2Int candidate = grid + directions8[i];
            if (HasFactory(candidate))
            {
                factoryGrid = candidate;
                return true;
            }
        }

        factoryGrid = grid;
        return false;
    }

    public bool CanEnterCell(Vector2Int grid, Vector2Int destination)
    {
        if (HasObstacle(grid))
            return false;

        if (grid == destination)
            return true;

        return !HasItemOrFactory(grid);
    }

    private bool HasBlockingCollider(Vector2Int grid, LayerMask layerMask)
    {
        Vector3 center = GridToWorldCenter(grid);
        center.y = GetLandSurfaceY() + 0.5f;

        Vector3 halfExtents = new Vector3(hexRadius * 0.5f * obstacleCheckFill, 0.5f, hexRadius * 0.5f * obstacleCheckFill);
        Collider[] cols = Physics.OverlapBox(center, halfExtents, Quaternion.identity, layerMask);

        for (int i = 0; i < cols.Length; i++)
        {
            var c = cols[i];
            if (c == null)
                continue;

            if (landTransform != null && (c.transform == landTransform || c.transform.IsChildOf(landTransform)))
                continue;

            return true;
        }

        return false;
    }

    // Land의 y 높이에 맞춰 marker/player를 올려놓기 위한 헬퍼
    public float GetLandSurfaceY()
    {
        if (landTransform == null)
            return 0.01f;

        // BoxCollider가 있으면 bounds 상단 사용
        if (landTransform.TryGetComponent<Collider>(out var col))
            return (col.bounds.max.y+0.01f);

        return landTransform.position.y + 0.01f;
    }

    private void OnDrawGizmos()
    {
        if (hexRadius <= 0f)
            return;

        float y = GetLandSurfaceY();

        for (int q = debugQMin; q <= debugQMax; q++)
        {
            for (int r = debugRMin; r <= debugRMax; r++)
            {
                Vector3 center = GridToWorldCenter(new Vector2Int(q, r));
                center.y = y;

                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(center, debugCenterSphereRadius);

                DrawSquareOutlineGizmo(center);
            }
        }
    }

    private void DrawSquareOutlineGizmo(Vector3 center)
    {
        Gizmos.color = Color.cyan;
        float half = hexRadius * 0.5f;
        Vector3 a = new Vector3(center.x - half, center.y, center.z - half);
        Vector3 b = new Vector3(center.x + half, center.y, center.z - half);
        Vector3 c = new Vector3(center.x + half, center.y, center.z + half);
        Vector3 d = new Vector3(center.x - half, center.y, center.z + half);

        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }
}

