using UnityEngine;

public class GridManager : MonoBehaviour
{
    private static readonly Vector2Int[] axialDirections6 =
    {
        new Vector2Int(1, 0),
        new Vector2Int(1, -1),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(-1, 1),
        new Vector2Int(0, 1)
    };

    private const float TwoThirds = 2f / 3f;
    private const float MinusOneThird = -1f / 3f;
    private const float ThreeHalves = 1.5f;
    private static readonly float sqrt3 = Mathf.Sqrt(3f);

    [Header("Grid 기준 오브젝트")]
    [SerializeField] private Transform landTransform;

    [Header("Grid Settings")]
    [SerializeField] private float hexRadius = 1f;

    [Header("Obstacle Settings")]
    [Tooltip("이 레이어에 있는 콜라이더는 장애물로 간주합니다.")]
    [SerializeField] private LayerMask obstacleLayerMask;
    [SerializeField] private LayerMask itemLayerMask;
    [Tooltip("셀 워커블 검사 시, 셀 크기 대비 체크 박스 비율(너무 크면 오탐, 너무 작으면 통과).")]
    [SerializeField, Range(0.1f, 1f)] private float obstacleCheckFill = 0.9f;

    [Header("Debug Gizmos")]
    [SerializeField] private int debugQMin = -5;
    [SerializeField] private int debugQMax = 5;
    [SerializeField] private int debugRMin = -5;
    [SerializeField] private int debugRMax = 5;
    [SerializeField, Range(0.01f, 0.5f)] private float debugCenterSphereRadius = 0.08f;

    // 전제 조건: Land 월드 중심 = (0, 0, 0)
    // 필요 시 인스펙터에서 원점 오프셋 확장 가능
    private Vector3 gridOrigin = Vector3.zero;

    public float HexRadius => hexRadius;
    public Transform LandTransform => landTransform;
    public static Vector2Int[] AxialDirections6 => axialDirections6;

    private void Awake()
    {
        if (hexRadius <= 0f)
            hexRadius = 1f;
    }

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        float localX = worldPosition.x - gridOrigin.x;
        float localZ = worldPosition.z - gridOrigin.z;

        float q = (TwoThirds * localX) / hexRadius;
        float r = (MinusOneThird * localX + sqrt3 / 3f * localZ) / hexRadius;

        return HexRound(q, r);
    }

    private Vector2Int HexRound(float q, float r)
    {
        float s = -q - r;

        int rq = Mathf.RoundToInt(q);
        int rr = Mathf.RoundToInt(r);
        int rs = Mathf.RoundToInt(s);

        float qDiff = Mathf.Abs(rq - q);
        float rDiff = Mathf.Abs(rr - r);
        float sDiff = Mathf.Abs(rs - s);

        if (qDiff > rDiff && qDiff > sDiff)
            rq = -rr - rs;
        else if (rDiff > sDiff)
            rr = -rq - rs;

        return new Vector2Int(rq, rr);
    }

    public Vector3 GridToWorldCenter(Vector2Int grid)
    {
        int q = grid.x;
        int r = grid.y;

        float x = gridOrigin.x + hexRadius * ThreeHalves * q;
        float z = gridOrigin.z + hexRadius * sqrt3 * (r + q * 0.5f);

        return new Vector3(x, 0f, z);
    }

    public static int HexDistance(Vector2Int a, Vector2Int b)
    {
        int aq = a.x;
        int ar = a.y;
        int asCoord = -aq - ar;

        int bq = b.x;
        int br = b.y;
        int bsCoord = -bq - br;

        return (Mathf.Abs(aq - bq) + Mathf.Abs(ar - br) + Mathf.Abs(asCoord - bsCoord)) / 2;
    }

    public bool IsWalkable(Vector2Int grid)
    {
        return !HasBlockingCollider(grid, obstacleLayerMask);
    }

    public bool HasItem(Vector2Int grid)
    {
        return HasBlockingCollider(grid, itemLayerMask);
    }

    public bool CanEnterCell(Vector2Int grid, Vector2Int destination)
    {
        if (!IsWalkable(grid))
            return false;

        if (grid == destination)
            return true;

        return !HasItem(grid);
    }

    private bool HasBlockingCollider(Vector2Int grid, LayerMask layerMask)
    {
        Vector3 center = GridToWorldCenter(grid);
        center.y = GetLandSurfaceY() + 0.5f;

        float checkRadius = hexRadius * 0.5f * obstacleCheckFill;
        Collider[] cols = Physics.OverlapSphere(center, checkRadius, layerMask);

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

                DrawHexOutlineGizmo(center);
            }
        }
    }

    private void DrawHexOutlineGizmo(Vector3 center)
    {
        Gizmos.color = Color.cyan;

        Vector3 first = GetFlatTopCorner(center, 0);
        Vector3 previous = first;

        for (int i = 1; i < 6; i++)
        {
            Vector3 current = GetFlatTopCorner(center, i);
            Gizmos.DrawLine(previous, current);
            previous = current;
        }

        Gizmos.DrawLine(previous, first);
    }

    private Vector3 GetFlatTopCorner(Vector3 center, int cornerIndex)
    {
        float angleDeg = 60f * cornerIndex;
        float angleRad = angleDeg * Mathf.Deg2Rad;

        float x = center.x + hexRadius * Mathf.Cos(angleRad);
        float z = center.z + hexRadius * Mathf.Sin(angleRad);

        return new Vector3(x, center.y, z);
    }
}

