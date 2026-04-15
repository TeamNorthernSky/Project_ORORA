using System;
using System.Collections.Generic;
using UnityEngine;

namespace Orora.TestMerge
{
    [CreateAssetMenu(fileName = "TMLevelData", menuName = "Orora/TestMerge/Level Data")]
    public class TMLevelData : ScriptableObject
    {
        [Header("Meta")]
        [SerializeField] private string levelId = "tm_level_001";
        [SerializeField] private string displayName = "TM Level";

        [Header("Grid")]
        [SerializeField] private Vector2Int gridSize = new Vector2Int(20, 20);

        [Header("Placement (양수 좌표계 기준)")]
        [SerializeField] private List<Vector2Int> obstacleCells = new List<Vector2Int>();
        [SerializeField] private List<TMItemPlacementData> itemPlacements = new List<TMItemPlacementData>();
        [SerializeField] private List<TMMinePlacementData> minePlacements = new List<TMMinePlacementData>();
        [SerializeField] private List<TMPartySpawnData> partySpawns = new List<TMPartySpawnData>();

        public string LevelId => levelId;
        public string DisplayName => displayName;
        public Vector2Int GridSize => gridSize;
        public Vector2Int GridMin => Vector2Int.zero;
        public Vector2Int GridMax => new Vector2Int(gridSize.x - 1, gridSize.y - 1);
        public IReadOnlyList<Vector2Int> ObstacleCells => obstacleCells;
        public IReadOnlyList<TMItemPlacementData> ItemPlacements => itemPlacements;
        public IReadOnlyList<TMMinePlacementData> MinePlacements => minePlacements;
        public IReadOnlyList<TMPartySpawnData> PartySpawns => partySpawns;

        public bool IsInsideGrid(Vector2Int grid)
            => grid.x >= 0 && grid.y >= 0 && grid.x < gridSize.x && grid.y < gridSize.y;

        public void SetObstacle(Vector2Int grid)
        {
            if (!IsInsideGrid(grid)) return;
            RemoveNonObstaclePlacementsAt(grid);
            if (!obstacleCells.Contains(grid)) obstacleCells.Add(grid);
        }

        public void SetItem(Vector2Int grid, TMResourceType resource, int amount)
        {
            if (!IsInsideGrid(grid)) return;
            RemoveAllPlacementsAt(grid);
            itemPlacements.Add(new TMItemPlacementData(grid, resource, amount));
        }

        public void SetMine(Vector2Int grid, TMResourceType resource, int resourcePerTurn, TMMineState state)
        {
            if (!IsInsideGrid(grid)) return;
            RemoveAllPlacementsAt(grid);
            minePlacements.Add(new TMMinePlacementData(grid, resource, resourcePerTurn, state));
        }

        public void SetPartySpawn(Vector2Int grid, string partyId)
        {
            if (!IsInsideGrid(grid)) return;
            RemoveAllPlacementsAt(grid);
            if (!string.IsNullOrWhiteSpace(partyId))
                partySpawns.RemoveAll(x => string.Equals(x.PartyId, partyId, StringComparison.Ordinal));
            partySpawns.Add(new TMPartySpawnData(partyId, grid));
        }

        public void EraseAt(Vector2Int grid)
        {
            obstacleCells.Remove(grid);
            itemPlacements.RemoveAll(x => x.GridPosition == grid);
            minePlacements.RemoveAll(x => x.GridPosition == grid);
            partySpawns.RemoveAll(x => x.GridPosition == grid);
        }

        private void RemoveNonObstaclePlacementsAt(Vector2Int grid)
        {
            itemPlacements.RemoveAll(x => x.GridPosition == grid);
            minePlacements.RemoveAll(x => x.GridPosition == grid);
            partySpawns.RemoveAll(x => x.GridPosition == grid);
        }

        private void RemoveAllPlacementsAt(Vector2Int grid)
        {
            obstacleCells.Remove(grid);
            itemPlacements.RemoveAll(x => x.GridPosition == grid);
            minePlacements.RemoveAll(x => x.GridPosition == grid);
            partySpawns.RemoveAll(x => x.GridPosition == grid);
        }

        private void OnValidate()
        {
            gridSize = new Vector2Int(Mathf.Max(1, gridSize.x), Mathf.Max(1, gridSize.y));
        }
    }

    [Serializable]
    public struct TMItemPlacementData
    {
        [SerializeField] private Vector2Int gridPosition;
        [SerializeField] private TMResourceType resourceType;
        [SerializeField] private int amount;

        public TMItemPlacementData(Vector2Int gridPosition, TMResourceType resourceType, int amount)
        {
            this.gridPosition = gridPosition;
            this.resourceType = resourceType;
            this.amount = amount;
        }

        public Vector2Int GridPosition => gridPosition;
        public TMResourceType ResourceType => resourceType;
        public int Amount => amount;
    }

    [Serializable]
    public struct TMMinePlacementData
    {
        [SerializeField] private Vector2Int gridPosition;
        [SerializeField] private TMResourceType resourceType;
        [SerializeField] private int resourcePerTurn;
        [SerializeField] private TMMineState initialState;

        public TMMinePlacementData(Vector2Int gridPosition, TMResourceType resourceType, int resourcePerTurn, TMMineState initialState)
        {
            this.gridPosition = gridPosition;
            this.resourceType = resourceType;
            this.resourcePerTurn = resourcePerTurn;
            this.initialState = initialState;
        }

        public Vector2Int GridPosition => gridPosition;
        public TMResourceType ResourceType => resourceType;
        public int ResourcePerTurn => resourcePerTurn;
        public TMMineState InitialState => initialState;
    }

    [Serializable]
    public struct TMPartySpawnData
    {
        [SerializeField] private string partyId;
        [SerializeField] private Vector2Int gridPosition;

        public TMPartySpawnData(string partyId, Vector2Int gridPosition)
        {
            this.partyId = partyId;
            this.gridPosition = gridPosition;
        }

        public string PartyId => partyId;
        public Vector2Int GridPosition => gridPosition;
    }
}
