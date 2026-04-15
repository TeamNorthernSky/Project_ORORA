using System;
using System.Collections.Generic;
using UnityEngine;

namespace Orora.TestMerge
{
    /// <summary>
    /// 테스트 머지용 그리드 매니저. 권위(authority)는 flat array(TMGridCell[]).
    /// DH의 GridManager와 동일한 공개 API를 제공하여 TMAStarPathfinder·TMPartyGridMover가 그대로 사용 가능.
    /// 좌표계: [0, width-1] × [0, height-1] 양수 정규화.
    /// </summary>
    public class TMGridManager : MonoBehaviour
    {
        public const float CellSize = 1f;
        public const int MaxSupportedSize = 8192;

        public static readonly Vector2Int[] Directions8 =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
            new Vector2Int(1, 0),
            new Vector2Int(1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(-1, 1),
        };

        [Header("Grid 기준 오브젝트")]
        [SerializeField] private Transform landTransform;

        [Header("Grid Size (런타임에 TMLevelLoader가 결정)")]
        [SerializeField, Min(1)] private int width = 32;
        [SerializeField, Min(1)] private int height = 32;

        [Header("Fog/Movement Settings")]
        [SerializeField] private bool restrictMovementToVisibleCells = true;

        [Header("Debug Gizmos")]
        [SerializeField] private bool drawGizmos = true;
        [SerializeField, Range(0.01f, 0.5f)] private float debugCenterSphereRadius = 0.08f;

        private TMGridCell[] cells;
        private Vector3 gridOrigin = Vector3.zero;
        private bool initialized;

        private readonly Dictionary<Vector2Int, Component> cellObjects = new Dictionary<Vector2Int, Component>();

        private static readonly int GridWorldSizeId = Shader.PropertyToID("_GridWorldSize");

        public int Width => width;
        public int Height => height;
        public int CellCount => width * height;
        public Transform LandTransform => landTransform;
        public Vector3 GridOrigin => gridOrigin;
        public bool IsInitialized => initialized;

        public event Action<Vector2Int> CellVisibilityChanged;
        public event Action GridChanged;

        // ---------------------------------------------------------------------
        // 초기화
        // ---------------------------------------------------------------------

        private void Awake()
        {
            if (!initialized)
                Initialize(width, height, Vector3.zero);
        }

        /// <summary>
        /// 그리드 크기·원점을 확정하고 셀 배열을 할당. TMLevelLoader가 호출.
        /// origin은 음수 좌표계 LevelData를 양수 정규화하기 위한 월드 쉬프트 값.
        /// </summary>
        public void Initialize(int newWidth, int newHeight, Vector3 origin)
        {
            if (newWidth > MaxSupportedSize || newHeight > MaxSupportedSize)
            {
                Debug.LogError($"[TMGridManager] 그리드 크기 ({newWidth}x{newHeight})가 최대 {MaxSupportedSize}를 초과");
                newWidth = Mathf.Min(newWidth, MaxSupportedSize);
                newHeight = Mathf.Min(newHeight, MaxSupportedSize);
            }

            width = Mathf.Max(1, newWidth);
            height = Mathf.Max(1, newHeight);
            gridOrigin = origin;

            cells = new TMGridCell[width * height];
            for (int i = 0; i < cells.Length; i++)
                cells[i] = TMGridCell.CreateDefault();

            float worldW = width * CellSize;
            float worldH = height * CellSize;
            Shader.SetGlobalVector(GridWorldSizeId, new Vector4(worldW, worldH, 1f / worldW, 1f / worldH));

            initialized = true;
            GridChanged?.Invoke();
        }

        // ---------------------------------------------------------------------
        // 좌표 변환
        // ---------------------------------------------------------------------

        public Vector2Int WorldToGrid(Vector3 worldPosition)
        {
            float localX = worldPosition.x - gridOrigin.x;
            float localZ = worldPosition.z - gridOrigin.z;
            int x = Mathf.RoundToInt(localX / CellSize);
            int y = Mathf.RoundToInt(localZ / CellSize);
            return new Vector2Int(x, y);
        }

        public Vector3 GridToWorldCenter(Vector2Int grid)
        {
            float x = gridOrigin.x + CellSize * grid.x;
            float z = gridOrigin.z + CellSize * grid.y;
            return new Vector3(x, 0f, z);
        }

        public static int GridDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        public bool IsInBounds(Vector2Int grid)
        {
            return grid.x >= 0 && grid.x < width && grid.y >= 0 && grid.y < height;
        }

        private int IndexOf(Vector2Int grid)
        {
            return grid.y * width + grid.x;
        }

        // ---------------------------------------------------------------------
        // 셀 접근
        // ---------------------------------------------------------------------

        public TMGridCell GetCell(Vector2Int grid)
        {
            if (!IsInBounds(grid) || cells == null)
                return default;
            return cells[IndexOf(grid)];
        }

        public bool TryGetCell(Vector2Int grid, out TMGridCell cell)
        {
            if (!IsInBounds(grid) || cells == null)
            {
                cell = default;
                return false;
            }
            cell = cells[IndexOf(grid)];
            return true;
        }

        public void SetTerrain(Vector2Int grid, TMTerrainType terrain)
        {
            if (!IsInBounds(grid)) return;
            cells[IndexOf(grid)].terrainType = terrain;
        }

        public void SetWalkable(Vector2Int grid, bool walkable)
        {
            if (!IsInBounds(grid)) return;
            cells[IndexOf(grid)].isWalkable = walkable;
        }

        public void SetOccupant(Vector2Int grid, int occupantId)
        {
            if (!IsInBounds(grid)) return;
            cells[IndexOf(grid)].occupantId = occupantId;
        }

        public void SetEvent(Vector2Int grid, int eventId)
        {
            if (!IsInBounds(grid)) return;
            cells[IndexOf(grid)].eventId = eventId;
        }

        // ---------------------------------------------------------------------
        // 시야
        // ---------------------------------------------------------------------

        public TMFogVisibilityState GetVisibility(Vector2Int grid)
        {
            if (!IsInBounds(grid) || cells == null)
                return TMFogVisibilityState.Unexplored;
            return cells[IndexOf(grid)].visibility;
        }

        public bool IsVisible(Vector2Int grid) => GetVisibility(grid) == TMFogVisibilityState.Visible;
        public bool IsExplored(Vector2Int grid) => GetVisibility(grid) != TMFogVisibilityState.Unexplored;

        public int GetLastRevealedDay(Vector2Int grid)
        {
            if (!IsInBounds(grid) || cells == null) return 0;
            return cells[IndexOf(grid)].lastRevealedDay;
        }

        /// <summary>중심으로부터 radius 범위 셀을 Visible 상태로 설정. day는 TMTurnManager.CurrentDay.</summary>
        public bool RevealArea(Vector2Int center, int radius, int day)
        {
            if (cells == null) return false;

            bool anyChanged = false;
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                for (int x = center.x - radius; x <= center.x + radius; x++)
                {
                    var g = new Vector2Int(x, y);
                    if (!IsInBounds(g)) continue;
                    int idx = IndexOf(g);
                    bool changed = cells[idx].visibility != TMFogVisibilityState.Visible || cells[idx].lastRevealedDay != day;
                    cells[idx].visibility = TMFogVisibilityState.Visible;
                    cells[idx].lastRevealedDay = day;
                    if (changed)
                    {
                        CellVisibilityChanged?.Invoke(g);
                        anyChanged = true;
                    }
                }
            }

            if (anyChanged) GridChanged?.Invoke();
            return anyChanged;
        }

        /// <summary>Visible 셀 중 day - lastRevealedDay >= delayDays 인 것을 Fogged로 되돌리고 리스트 반환.</summary>
        public List<Vector2Int> CollectCellsToRefog(int currentDay, int delayDays)
        {
            var result = new List<Vector2Int>();
            if (cells == null) return result;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int idx = y * width + x;
                    if (cells[idx].visibility != TMFogVisibilityState.Visible) continue;
                    if (currentDay - cells[idx].lastRevealedDay < delayDays) continue;
                    result.Add(new Vector2Int(x, y));
                }
            }

            return result;
        }

        public void SetVisibility(Vector2Int grid, TMFogVisibilityState state)
        {
            if (!IsInBounds(grid) || cells == null) return;
            int idx = IndexOf(grid);
            if (cells[idx].visibility == state) return;
            cells[idx].visibility = state;
            CellVisibilityChanged?.Invoke(grid);
        }

        // ---------------------------------------------------------------------
        // DH 호환 API (Physics 쿼리 없이 배열로만 판단)
        // ---------------------------------------------------------------------

        public bool HasObstacle(Vector2Int grid)
        {
            if (!IsInBounds(grid) || cells == null) return true;
            return !cells[IndexOf(grid)].isWalkable;
        }

        public bool HasOccupant(Vector2Int grid, int ignoreId = -1)
        {
            if (!IsInBounds(grid) || cells == null) return false;
            int id = cells[IndexOf(grid)].occupantId;
            return id >= 0 && id != ignoreId;
        }

        public bool HasEvent(Vector2Int grid)
        {
            if (!IsInBounds(grid) || cells == null) return false;
            return cells[IndexOf(grid)].eventId >= 0;
        }

        public bool IsVisibleCell(Vector2Int grid)
        {
            if (!restrictMovementToVisibleCells) return true;
            return IsVisible(grid);
        }

        public bool CanEnterCell(Vector2Int grid, Vector2Int destination, int ignoreOccupantId = -1)
        {
            if (!IsInBounds(grid)) return false;
            if (HasObstacle(grid)) return false;
            if (HasOccupant(grid, ignoreOccupantId)) return false;
            if (!IsVisibleCell(grid)) return false;

            if (grid == destination) return true;

            // 경유 중간 셀에 이벤트(아이템/광산/광맥 등 정지 유발)가 있으면 통과 불가
            return !HasEvent(grid);
        }

        /// <summary>
        /// 특정 셀에 배치된 오브젝트(아이템·광산 등)를 등록. TMLevelLoader가 스폰 시 호출.
        /// 동일 셀 재등록 시 기존 덮어씀.
        /// </summary>
        public void RegisterCellObject(Vector2Int grid, Component obj)
        {
            if (obj == null) return;
            cellObjects[grid] = obj;
        }

        public void UnregisterCellObject(Vector2Int grid)
        {
            cellObjects.Remove(grid);
        }

        public bool TryGetCellObject<T>(Vector2Int grid, out T obj) where T : Component
        {
            obj = null;
            if (!cellObjects.TryGetValue(grid, out var c) || c == null) return false;
            obj = c as T;
            if (obj == null) obj = (c as Component)?.GetComponent<T>();
            return obj != null;
        }

        public bool TryGetAdjacentCellWith<T>(Vector2Int grid, out Vector2Int foundGrid, out T obj) where T : Component
        {
            for (int i = 0; i < Directions8.Length; i++)
            {
                var candidate = grid + Directions8[i];
                if (TryGetCellObject<T>(candidate, out obj))
                {
                    foundGrid = candidate;
                    return true;
                }
            }
            foundGrid = grid;
            obj = null;
            return false;
        }

        public bool TryGetAdjacentGridWithEvent(Vector2Int grid, out Vector2Int eventGrid)
        {
            for (int i = 0; i < Directions8.Length; i++)
            {
                var candidate = grid + Directions8[i];
                if (HasEvent(candidate))
                {
                    eventGrid = candidate;
                    return true;
                }
            }
            eventGrid = grid;
            return false;
        }

        // ---------------------------------------------------------------------
        // 헬퍼
        // ---------------------------------------------------------------------

        public float GetLandSurfaceY()
        {
            if (landTransform == null) return 0.01f;
            if (landTransform.TryGetComponent<Collider>(out var col))
                return col.bounds.max.y + 0.01f;
            return landTransform.position.y + 0.01f;
        }

        // ---------------------------------------------------------------------
        // Gizmos
        // ---------------------------------------------------------------------

        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;
            if (width <= 0 || height <= 0) return;

            float y = GetLandSurfaceY();
            Vector3 origin = Application.isPlaying ? gridOrigin : Vector3.zero;
            float half = CellSize * 0.5f;

            Gizmos.color = Color.yellow;
            for (int gy = 0; gy < height; gy++)
            {
                for (int gx = 0; gx < width; gx++)
                {
                    Vector3 c = new Vector3(origin.x + gx * CellSize, y, origin.z + gy * CellSize);
                    Gizmos.DrawSphere(c, debugCenterSphereRadius);
                }
            }

            Gizmos.color = Color.cyan;
            Vector3 bl = new Vector3(origin.x - half, y, origin.z - half);
            Vector3 br = new Vector3(origin.x + (width - 1) * CellSize + half, y, origin.z - half);
            Vector3 tr = new Vector3(origin.x + (width - 1) * CellSize + half, y, origin.z + (height - 1) * CellSize + half);
            Vector3 tl = new Vector3(origin.x - half, y, origin.z + (height - 1) * CellSize + half);
            Gizmos.DrawLine(bl, br);
            Gizmos.DrawLine(br, tr);
            Gizmos.DrawLine(tr, tl);
            Gizmos.DrawLine(tl, bl);
        }
    }
}
