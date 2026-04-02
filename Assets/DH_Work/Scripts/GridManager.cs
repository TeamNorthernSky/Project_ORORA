using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid 기준 오브젝트")]
    [SerializeField] private Transform landTransform;

    [Header("Grid Settings")]
    [SerializeField] private float cellSize = 1f;

    [Header("Obstacle Settings")]
    [Tooltip("이 레이어에 있는 콜라이더는 장애물로 간주합니다.")]
    [SerializeField] private LayerMask obstacleLayerMask;
    //[Tooltip("이 태그를 가진 오브젝트는 장애물로 간주합니다(선택). 비워두면 태그 필터 미사용.")]
    //[SerializeField] private string obstacleTag = "Obstacle";
    [Tooltip("셀 워커블 검사 시, 셀 크기 대비 체크 박스 비율(너무 크면 오탐, 너무 작으면 통과).")]
    [SerializeField, Range(0.1f, 1f)] private float obstacleCheckFill = 0.9f;

    // 전제 조건: Land 월드 중심 = (0, 0, 0)
    // 필요 시 인스펙터에서 원점 오프셋 확장 가능
    private Vector3 gridOrigin = Vector3.zero;

    public float CellSize => cellSize;
    public Transform LandTransform => landTransform;

    private void Awake()
    {
        if (cellSize <= 0f)
            cellSize = 1f;
    }

    public Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        int gx = Mathf.RoundToInt((worldPosition.x - gridOrigin.x) / cellSize);
        int gy = Mathf.RoundToInt((worldPosition.z - gridOrigin.z) / cellSize);
        return new Vector2Int(gx, gy);
    }

    public Vector3 GridToWorldCenter(Vector2Int grid)
    {
        float x = gridOrigin.x + (grid.x * cellSize);
        float z = gridOrigin.z + (grid.y * cellSize);
        return new Vector3(x, 0f, z);
    }

    public bool IsWalkable(Vector2Int grid)
    {
        // 장애물 레이어가 설정되지 않았으면 태그 기반만 사용(혹은 전부 통과)
        Vector3 center = GridToWorldCenter(grid);
        center.y = GetLandSurfaceY() + 0.5f; // 바닥 위에서 체크

        Vector3 halfExtents = new Vector3(cellSize * 0.5f * obstacleCheckFill, 0.45f, cellSize * 0.5f * obstacleCheckFill);

        Collider[] cols = Physics.OverlapBox(center, halfExtents, Quaternion.identity, obstacleLayerMask);
        for (int i = 0; i < cols.Length; i++)
        {
            var c = cols[i];
            if (c == null)
                continue;

            // Land 자체는 장애물로 치지 않음
            if (landTransform != null && (c.transform == landTransform || c.transform.IsChildOf(landTransform)))
                continue;

            //if (!string.IsNullOrEmpty(obstacleTag))
            //{
            //    if (c.CompareTag(obstacleTag) || c.transform.root.CompareTag(obstacleTag))
            //        return false;
            //}

            // 레이어로 잡힌 건 기본적으로 장애물로 처리
            return false;
        }

        // 레이어로는 못 잡았지만 태그 기반으로도 잡고 싶으면(레이어 0일 때):
        //if (!string.IsNullOrEmpty(obstacleTag))
        //{
        //    // OverlapBox가 레이어로 못 잡았을 수 있으니, 태그는 별도 체크가 필요하지만
        //    // 비용이 커서 기본은 레이어 기반 권장. 여기서는 "레이어가 0이면" 태그 검색도 수행.
        //    if (obstacleLayerMask.value == 0)
        //    {
        //        Collider[] all = Physics.OverlapBox(center, halfExtents, Quaternion.identity, ~0);
        //        for (int i = 0; i < all.Length; i++)
        //        {
        //            var c = all[i];
        //            if (c == null)
        //                continue;
        //            if (landTransform != null && (c.transform == landTransform || c.transform.IsChildOf(landTransform)))
        //                continue;
        //            if (c.CompareTag(obstacleTag) || c.transform.root.CompareTag(obstacleTag))
        //                return false;
        //        }
        //    }
        //}

        return true;
    }

    // Land의 y 높이에 맞춰 marker/player를 올려놓기 위한 헬퍼
    public float GetLandSurfaceY()
    {
        if (landTransform == null)
            return 0f;

        // BoxCollider가 있으면 bounds 상단 사용
        if (landTransform.TryGetComponent<Collider>(out var col))
            return col.bounds.max.y;

        return landTransform.position.y;
    }
}

