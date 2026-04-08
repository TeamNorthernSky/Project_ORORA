using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 그리드 데이터 관리 매니저. GameManager 하위에 배치.
/// 순수 데이터 레이어 — 시각적 표현은 담당하지 않음.
/// </summary>
public class PlayGridManager : MonoBehaviour
{
    public const int GridWidth = 64;
    public const int GridHeight = 64;
    public const float CellSize = 1f;

    private GridCell[] cells;
    private Dictionary<int, MapObject> mapObjects = new Dictionary<int, MapObject>();
    private int nextObjectId = 0;

    public void Initialize()
    {
        cells = new GridCell[GridWidth * GridHeight];
        for (int y = 0; y < GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                cells[y * GridWidth + x] = new GridCell(x, y);
            }
        }
        Debug.Log($"[PlayGridManager] {GridWidth}x{GridHeight} 그리드 초기화 완료");
    }

    // --- 셀 접근 ---

    public GridCell GetCell(int x, int y)
    {
        if (!IsInBounds(x, y)) return null;
        return cells[y * GridWidth + x];
    }

    public GridCell GetCell(Vector2Int pos) => GetCell(pos.x, pos.y);

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < GridWidth && y >= 0 && y < GridHeight;
    }

    public bool IsWalkable(int x, int y)
    {
        var cell = GetCell(x, y);
        return cell != null && cell.isWalkable && !cell.IsOccupied;
    }

    public bool IsWalkable(Vector2Int pos) => IsWalkable(pos.x, pos.y);

    // --- 좌표 변환 ---

    /// <summary>
    /// 그리드 좌표 → 월드 좌표 (셀 중심).
    /// 그리드 (0,0)은 월드 원점, X축 = 그리드 X, Z축 = 그리드 Y.
    /// </summary>
    public static Vector3 GridToWorld(int x, int y)
    {
        return new Vector3(
            x * CellSize + CellSize * 0.5f,
            0f,
            y * CellSize + CellSize * 0.5f
        );
    }

    public static Vector3 GridToWorld(Vector2Int pos) => GridToWorld(pos.x, pos.y);

    /// <summary>
    /// 월드 좌표 → 그리드 좌표.
    /// </summary>
    public static Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / CellSize),
            Mathf.FloorToInt(worldPos.z / CellSize)
        );
    }

    // --- 오브젝트 배치 ---

    /// <summary>
    /// MapObject를 그리드에 배치. 점유 셀들을 갱신.
    /// </summary>
    public bool PlaceObject(MapObject obj)
    {
        var occupiedCells = obj.GetOccupiedCells();

        // 배치 가능 여부 확인
        foreach (var pos in occupiedCells)
        {
            if (!IsInBounds(pos.x, pos.y))
            {
                Debug.LogWarning($"[PlayGridManager] 배치 실패: ({pos.x},{pos.y})가 범위 밖");
                return false;
            }
            var cell = GetCell(pos);
            if (cell.IsOccupied)
            {
                Debug.LogWarning($"[PlayGridManager] 배치 실패: ({pos.x},{pos.y})가 이미 점유됨 (ID:{cell.occupantId})");
                return false;
            }
        }

        // ID 부여 및 등록
        obj.id = nextObjectId++;
        mapObjects[obj.id] = obj;

        // 셀 점유 갱신
        foreach (var pos in occupiedCells)
        {
            var cell = GetCell(pos);
            cell.occupantId = obj.id;
            if (!obj.isWalkable)
                cell.isWalkable = false;
        }

        return true;
    }

    /// <summary>
    /// MapObject를 그리드에서 제거. 점유 셀들을 해제.
    /// </summary>
    public void RemoveObject(int objectId)
    {
        if (!mapObjects.TryGetValue(objectId, out var obj)) return;

        foreach (var pos in obj.GetOccupiedCells())
        {
            var cell = GetCell(pos);
            if (cell != null && cell.occupantId == objectId)
            {
                cell.occupantId = -1;
                cell.isWalkable = true;
            }
        }

        mapObjects.Remove(objectId);
    }

    public MapObject GetMapObject(int objectId)
    {
        return mapObjects.TryGetValue(objectId, out var obj) ? obj : null;
    }

    // --- 시야 ---

    /// <summary>
    /// 지정 위치 주변 시야 범위 내 셀을 Visible로 설정.
    /// </summary>
    public void RevealArea(Vector2Int center, int radius)
    {
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx * dx + dy * dy <= radius * radius)
                {
                    var cell = GetCell(center.x + dx, center.y + dy);
                    if (cell != null)
                        cell.visibility = GridCell.VisibilityState.Visible;
                }
            }
        }
    }

    /// <summary>
    /// 현재 Visible인 셀들을 Fogged로 전환 (턴 종료 시 등).
    /// </summary>
    public void FogAllVisible()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i].visibility == GridCell.VisibilityState.Visible)
                cells[i].visibility = GridCell.VisibilityState.Fogged;
        }
    }

    // --- 전체 셀 배열 접근 (시각화, 세이브 등) ---

    public GridCell[] GetAllCells() => cells;
}
