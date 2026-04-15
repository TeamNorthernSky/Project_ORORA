using UnityEngine;

/// <summary>
/// CSV 기반 적 데이터 보관. Combat tuning은 BattleCharactor에만 반영하고,
/// 에디터 OnValidate에서 BattleCharactor를 덮어쓰지 않습니다.
/// </summary>
public class EnemyScript : MonoBehaviour, IUnitIdentifier
{
    public UnitData enemyData;

    public StatBlock currentStats;

    [Header("Stat Weights (CSV Initialize 시 BattleCharactor로 전달)")]
    [SerializeField] private StatWeights unitWeight = new StatWeights(1f, 1f, 1f);

    [SerializeField] private StatWeights levelWeight = new StatWeights(1f, 1f, 1f);

    [SerializeField] private StatWeights classWeight = new StatWeights(1f, 1f, 1f);

    [Range(1, 100)] public int level = 1;

    [Range(1, 50)] public int unitCount = 1;

    public StatWeights UnitWeight => unitWeight;

    public StatWeights LevelWeight => levelWeight;

    public StatWeights ClassWeight => classWeight;

    public UnitData Data
    {
        get => enemyData;
        set => enemyData = value;
    }

    public string UnitID
    {
        get
        {
            if (enemyData == null || string.IsNullOrWhiteSpace(enemyData.Index))
            {
                return string.Empty;
            }

            return enemyData.Index.Trim();
        }
    }

    public void Initialize(UnitData data)
    {
        enemyData = data;
        currentStats = data != null ? data.baseStats : default;

        var battle = GetComponent<BattleCharactor>();
        if (battle == null)
        {
            return;
        }

        if (data != null)
        {
            battle.SetBaseStats(data.baseStats);
            battle.SetUnitNameForSkillMatching(data.Name);
        }

        battle.ApplyCombatTuning(level, unitCount, unitWeight, levelWeight, classWeight);
        battle.RecalculateStats();
        battle.ResolveSelectedSkill();
        battle.InitializeCurrentHpToMax();
        battle.MarkInitializedFromDataPipeline();

        string id = UnitID;
        string nm = data != null ? data.Name : "null";
        Debug.Log($"[EnemyScript] Initialize: name={nm}, id='{id}'");
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            return;
        }

        level = Mathf.Clamp(level, 1, 100);
        unitCount = Mathf.Clamp(unitCount, 1, 50);

        var battle = GetComponent<BattleCharactor>();
        if (battle == null)
        {
            return;
        }

        if (enemyData == null || string.IsNullOrWhiteSpace(enemyData.Name))
        {
            return;
        }

        battle.SetUnitNameForSkillMatching(enemyData.Name);
        battle.ResolveSelectedSkill(false);
    }
}
