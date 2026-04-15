using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class CsvUnitProvider : MonoBehaviour, IBattleUnitProvider
{
    [System.Serializable]
    public class SpawnRequest
    {
        public string unitId;
        public int gridNumber = 1;
    }

    [SerializeField] private DataManager dataManager;
    [SerializeField] private List<SpawnRequest> playerRequests = new List<SpawnRequest>();
    [SerializeField] private List<SpawnRequest> enemyRequests = new List<SpawnRequest>();

    public List<UnitSpawnData> GetUnits()
    {
        if (dataManager == null)
        {
            Debug.LogError("[CsvUnitProvider] DataManager가 할당되지 않았습니다.");
            return new List<UnitSpawnData>();
        }

        var playersById = dataManager.PlayerUnits.ToDictionary(x => x.Index, x => x);
        var enemiesById = dataManager.EnemyUnits.ToDictionary(x => x.Index, x => (UnitData)x);

        var playerUnits = playerRequests
            .Where(r => r != null && !string.IsNullOrWhiteSpace(r.unitId) && playersById.ContainsKey(r.unitId))
            .Select(r => new UnitSpawnData
            {
                Data = UnitDataDTO.FromUnitData(playersById[r.unitId], TeamType.Player),
                Team = TeamType.Player,
                Prefab = LoadPrefab(playersById[r.unitId]),
                GridNumber = r.gridNumber
            });

        var enemyUnits = enemyRequests
            .Where(r => r != null && !string.IsNullOrWhiteSpace(r.unitId) && enemiesById.ContainsKey(r.unitId))
            .Select(r => new UnitSpawnData
            {
                Data = UnitDataDTO.FromUnitData(enemiesById[r.unitId], TeamType.Enemy),
                Team = TeamType.Enemy,
                Prefab = LoadPrefab(enemiesById[r.unitId]),
                GridNumber = r.gridNumber
            });

        return playerUnits.Concat(enemyUnits).Where(u => u.IsValid).ToList();
    }

    private static GameObject LoadPrefab(UnitData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.UnitType))
        {
            return null;
        }

        return Resources.Load<GameObject>($"prefab/Unit_{data.UnitType}");
    }
}
