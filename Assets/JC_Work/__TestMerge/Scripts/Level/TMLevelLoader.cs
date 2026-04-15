using UnityEngine;

namespace Orora.TestMerge
{
    /// <summary>
    /// TMLevelData 로드 → 그리드 크기 결정 · 오브젝트 스폰 · 셀 bake · 파티 스냅.
    /// 좌표계는 이미 양수 정규화되어 있으므로 origin 시프트 없음 (levelData가 양수 좌표).
    /// </summary>
    public class TMLevelLoader : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private TMLevelData levelData;

        [Header("Runtime References")]
        [SerializeField] private TMGridManager gridManager;
        [SerializeField] private TMPartyRegistry partyRegistry;
        [SerializeField] private TMLevelPrefabRegistry prefabRegistry;

        [Header("Spawn Roots")]
        [SerializeField] private Transform obstacleRoot;
        [SerializeField] private Transform itemRoot;
        [SerializeField] private Transform mineRoot;

        [Header("Load Options")]
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private bool clearExistingBeforeLoad = true;

        public TMLevelData LevelData => levelData;

        private void Start()
        {
            if (loadOnStart) LoadLevel();
        }

        [ContextMenu("Load Level")]
        public void LoadLevel()
        {
            if (levelData == null || gridManager == null)
            {
                Debug.LogWarning("[TMLevelLoader] levelData 또는 gridManager 누락");
                return;
            }

            if (clearExistingBeforeLoad) ClearSpawnedObjects();

            // 1) 그리드 초기화
            // origin 결정 규칙: 씬 배치상 cell(0,0)이 월드 어디에 오는가.
            // 관행: cell (0,0)은 월드 (0,0,0)에 위치 → origin = Vector3.zero.
            // Land는 이 규약에 맞춰 중심이 ((w-1)/2, _, (h-1)/2) 에 배치되어야 함.
            Vector3 origin = Vector3.zero;
            gridManager.Initialize(levelData.GridSize.x, levelData.GridSize.y, origin);

            // 2) 장애물 스폰 + isWalkable=false 마킹
            SpawnObstacles();

            // 3) 아이템 스폰 + 셀 이벤트 등록
            SpawnItems();

            // 4) 광산 스폰 + 셀 이벤트 등록
            SpawnMines();

            // 5) 파티 스폰 위치 적용
            ApplyPartySpawns();
        }

        private void SpawnObstacles()
        {
            var obstaclePrefab = prefabRegistry != null ? prefabRegistry.ObstaclePrefab : null;
            var cells = levelData.ObstacleCells;
            for (int i = 0; i < cells.Count; i++)
            {
                var grid = cells[i];
                if (!levelData.IsInsideGrid(grid)) continue;

                gridManager.SetWalkable(grid, false);

                if (obstaclePrefab == null) continue;
                Vector3 world = GetWorldPosition(grid);
                Instantiate(obstaclePrefab, world, Quaternion.identity, obstacleRoot);
            }
        }

        private void SpawnItems()
        {
            if (prefabRegistry == null) return;
            var placements = levelData.ItemPlacements;
            int eventIdSeed = 10000;
            for (int i = 0; i < placements.Count; i++)
            {
                var p = placements[i];
                if (!levelData.IsInsideGrid(p.GridPosition)) continue;

                if (!prefabRegistry.TryGetItemPrefab(p.ResourceType, out var itemPrefab))
                {
                    Debug.LogWarning($"[TMLevelLoader] item prefab 미등록: {p.ResourceType}", this);
                    continue;
                }

                Vector3 world = GetWorldPosition(p.GridPosition);
                var item = Instantiate(itemPrefab, world, Quaternion.identity, itemRoot);
                item.ApplyInitialData(p.ResourceType, p.Amount);

                int eventId = eventIdSeed++;
                gridManager.SetEvent(p.GridPosition, eventId);
                gridManager.RegisterCellObject(p.GridPosition, item);
            }
        }

        private void SpawnMines()
        {
            if (prefabRegistry == null) return;
            var placements = levelData.MinePlacements;
            int eventIdSeed = 20000;
            for (int i = 0; i < placements.Count; i++)
            {
                var p = placements[i];
                if (!levelData.IsInsideGrid(p.GridPosition)) continue;

                if (!prefabRegistry.TryGetMinePrefab(p.ResourceType, out var minePrefab))
                {
                    Debug.LogWarning($"[TMLevelLoader] mine prefab 미등록: {p.ResourceType}", this);
                    continue;
                }

                Vector3 world = GetWorldPosition(p.GridPosition);
                var mine = Instantiate(minePrefab, world, Quaternion.identity, mineRoot);
                mine.ApplyInitialData(p.ResourceType, p.ResourcePerTurn, p.InitialState);

                int eventId = eventIdSeed++;
                gridManager.SetEvent(p.GridPosition, eventId);
                gridManager.RegisterCellObject(p.GridPosition, mine);
            }
        }

        private void ApplyPartySpawns()
        {
            if (partyRegistry == null) return;
            var spawns = levelData.PartySpawns;
            for (int i = 0; i < spawns.Count; i++)
            {
                var s = spawns[i];
                if (string.IsNullOrWhiteSpace(s.PartyId)) continue;
                if (!partyRegistry.TryGetPartyById(s.PartyId, out var mover))
                {
                    Debug.LogWarning($"[TMLevelLoader] 파티 미등록: {s.PartyId}", this);
                    continue;
                }
                mover.SnapToGridPosition(s.GridPosition);
            }
        }

        private void ClearSpawnedObjects()
        {
            ClearChildren(obstacleRoot);
            ClearChildren(itemRoot);
            ClearChildren(mineRoot);
        }

        private void ClearChildren(Transform root)
        {
            if (root == null) return;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        private Vector3 GetWorldPosition(Vector2Int grid)
        {
            var world = gridManager.GridToWorldCenter(grid);
            world.y = gridManager.GetLandSurfaceY();
            return world;
        }
    }
}
