using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CSVLoader → 플레이어는 CharactorManager, 적은 EnemyManager로 분리 저장.
/// </summary>
public class DataManager : MonoBehaviour
{
    [SerializeField] private CSVDataLoad csvLoader;
    [SerializeField] private CharactorManager charactorManager;
    [SerializeField] private EnemyManager enemyManager;

    private void Awake()
    {
        if (csvLoader == null)
        {
            Debug.LogError("[DataManager] csvLoader가 할당되지 않았습니다.");
            return;
        }

        if (charactorManager == null)
        {
            Debug.LogError("[DataManager] charactorManager가 할당되지 않았습니다.");
            return;
        }

        if (enemyManager == null)
        {
            Debug.LogError("[DataManager] enemyManager가 할당되지 않았습니다.");
            return;
        }

        List<UnitData> playerUnits = csvLoader.LoadPlayerUnits();
        charactorManager.SetUnitData(playerUnits);
        Debug.Log($"[DataManager] 플레이어 유닛 {playerUnits.Count}건을 CharactorManager에 반영했습니다.");

        List<EnemyData> enemyUnits = csvLoader.LoadEnemyUnits();
        enemyManager.SetEnemyData(enemyUnits);
        Debug.Log($"[DataManager] 적 유닛 {enemyUnits.Count}건을 EnemyManager에 반영했습니다.");
    }
}
