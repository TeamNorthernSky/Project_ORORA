using System.Collections.Generic;
using UnityEngine;

public class BattleSceneManager : MonoBehaviour
{
    [Header("Manager References")]
    [SerializeField] private CharactorManager charactorManager;
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private PlayerSpawner unitSpawner;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private EquipmentManager equipmentManager;
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private BattleFlowManager battleFlowManager;

    [Header("Boot Option")]
    [SerializeField] private bool createOnStart = true;
    [SerializeField] private string fallbackCharactorId;

    [Header("Enemy Spawn (Optional)")]
    [SerializeField] private List<string> enemyUnitIds = new List<string>();
    [SerializeField] private int enemyStartGridNumber = 2;

    private BattleCharactor playerBattleCharactor;
    private readonly List<BattleCharactor> playerBattleCharactors = new List<BattleCharactor>();
    private readonly List<BattleCharactor> enemyBattleCharactors = new List<BattleCharactor>();

    public BattleCharactor PlayerBattleCharactor => playerBattleCharactor;

    /// <summary>디버그/멀티 플레이어 전투체. 첫 번째 플레이어는 <see cref="PlayerBattleCharactor"/>와 동일하게 유지.</summary>
    public IReadOnlyList<BattleCharactor> PlayerBattleCharactors => playerBattleCharactors;

    /// <summary>소환된 적 전투체 목록.</summary>
    public IReadOnlyList<BattleCharactor> EnemyBattleCharactors => enemyBattleCharactors;


    private void Awake()
    {

    }


    private void Start()
    {
        if (!createOnStart)
        {
            return;
        }

        // --- 기존 로직 보존 (디버그 리스트 기반 스폰으로 대체했을 때 참고용) ---
        /*
        string charactorId = fallbackCharactorId;
        if (charactorManager != null && !string.IsNullOrEmpty(charactorManager.GetSelectedCharactorId()))
        {
            charactorId = charactorManager.GetSelectedCharactorId();
        }

        // 1) 유닛 소환(지휘권)
        if (unitSpawner != null)
        {
            unitSpawner.SpawnUnit(charactorId, 1);

            for (int i = 0; i < enemyUnitIds.Count; i++)
            {
                string enemyId = enemyUnitIds[i];
                if (string.IsNullOrWhiteSpace(enemyId)) continue;

                int gridNumber = enemyStartGridNumber + i;
                unitSpawner.SpawnUnit(enemyId, gridNumber);
            }
        }

        playerBattleCharactor = CreateBattleCharactor(charactorId);
        */

        playerBattleCharactor = null;
        playerBattleCharactors.Clear();
        enemyBattleCharactors.Clear();

        if (unitSpawner != null && unitSpawner.debugSpawnRequests != null)
        {
            foreach (var req in unitSpawner.debugSpawnRequests)
            {
                if (string.IsNullOrWhiteSpace(req.unitId))
                {
                    continue;
                }

                unitSpawner.SpawnUnit(req.unitId, req.gridNumber);

                if (req.isPlayer)
                {
                    var battle = CreateBattleCharactor(req.unitId);
                    if (battle != null)
                    {
                        battle.IsPlayer = true;
                        playerBattleCharactors.Add(battle);
                        if (playerBattleCharactor == null)
                        {
                            playerBattleCharactor = battle;
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("[BattleSceneManager] PlayerSpawner 또는 debugSpawnRequests가 없어 플레이어 디버그 스폰을 건너뜁니다.");
        }

        if (enemySpawner != null && enemySpawner.debugSpawnRequests != null)
        {
            foreach (var req in enemySpawner.debugSpawnRequests)
            {
                if (string.IsNullOrWhiteSpace(req.unitId))
                {
                    continue;
                }

                enemySpawner.SpawnUnit(req.unitId, req.gridNumber);

                var enemyBattle = CreateBattleCharactor(req.unitId);
                if (enemyBattle != null)
                {
                    enemyBattle.IsPlayer = false;
                    enemyBattleCharactors.Add(enemyBattle);
                }
            }
        }
        else
        {
            Debug.LogWarning("[BattleSceneManager] EnemySpawner 또는 debugSpawnRequests가 없어 적 디버그 스폰을 건너뜁니다.");
        }

        if (battleFlowManager != null)
        {
            var allUnits = new List<BattleCharactor>(playerBattleCharactors.Count + enemyBattleCharactors.Count);
            allUnits.AddRange(playerBattleCharactors);
            allUnits.AddRange(enemyBattleCharactors);
            battleFlowManager.Initialize(allUnits);
        }
    }


    //처음 생성은 레벨 1, 유닛 1로 고정하여 생성, 이후 레벨업 추가시 추후 변경
    public BattleCharactor CreateBattleCharactor(
        string charactorId)
    {
        if (string.IsNullOrWhiteSpace(charactorId))
        {
            Debug.LogError("[BattleSceneManager] charactorId가 비어 있습니다.");
            return null;
        }

        if (charactorManager == null && enemyManager == null)
        {
            Debug.LogError("[BattleSceneManager] CharactorManager와 EnemyManager가 모두 없습니다.");
            return null;
        }

        UnitData unitData = null;
        int level = 1;
        int unitNum = 1;


        // 유닛도 추후에 추가하기
        if (charactorManager != null)
        {
            unitData = charactorManager.GetCharactorData(charactorId);
            if (unitData != null)
            {
                level = charactorManager.GetCharactorLevel(charactorId);
                Debug.Log($"[BattleSceneManager] 플레이어 데이터로 전투체 생성: id={charactorId}, Lv={level}");
            }
        }

        if (unitData == null && enemyManager != null)
        {
            var enemyData = enemyManager.GetEnemyData(charactorId);
            if (enemyData != null)
            {
                unitData = enemyData;
                level = enemyManager.GetEnemyLevel(charactorId);
                //Debug.Log($"[BattleSceneManager] 적 데이터로 전투체 생성: id={charactorId}, aiType={enemyData.aiType}");
            }
        }

        if (unitData == null)
        {
            Debug.LogError($"[BattleSceneManager] UnitData를 찾을 수 없습니다. id={charactorId}");
            return null;
        }

        var battleCharactor = new BattleCharactor(unitData, level, unitNum);
        battleCharactor.InitializeCurrentHpToMax();

        Debug.Log($"[BattleSceneManager] BattleCharactor 생성: {unitData.Name}, Lv.{level}, MaxHp={battleCharactor.FinalStats.HP}");
        return battleCharactor;
    }
}
