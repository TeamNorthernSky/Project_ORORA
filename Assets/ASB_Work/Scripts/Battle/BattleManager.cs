using UnityEngine;

/// <summary>
/// BattleAction 및 플레이어 입력에 의한 전투 실행. 데미지는 항상 target.TakeDamage로 적용합니다.
/// </summary>
[DisallowMultipleComponent]
public class BattleManager : MonoBehaviour
{
    /// <summary>ClassSkillSheet 행의 skillValue(예: 1.2 = 120%)로 그리드 스킬 피해를 계산합니다.</summary>
    public bool ExecuteGridSkill(BattleCharactor actor, BattleCharactor target, SkillData classSkillRow)
    {
        if (classSkillRow == null || actor == null || target == null)
        {
            return false;
        }

        if (actor.IsDead || target.IsDead)
        {
            return false;
        }

        float percent = Mathf.Max(0.01f, classSkillRow.skillValue * 100f);
        ExecuteGridSkill(actor, target, percent);
        return true;
    }

    public void ExecuteGridSkill(BattleCharactor actor, BattleCharactor target, float skillPercent)
    {
        if (actor == null || target == null)
        {
            return;
        }

        float atk = actor.FinalStats.Atk;
        float def = target.FinalStats.DEF;
        float multiplier = Mathf.Max(0.01f, skillPercent / 100f);
        float dmg = Mathf.Max(1f, (atk * multiplier) - def);
        target.TakeDamage(dmg);

        Debug.Log($"[Battle] GridSkill: {GetLabel(actor)} -> {GetLabel(target)} dmg={dmg:F1} ({skillPercent:0.##}%)");
    }

    /// <summary>일반 공격: 피해 = max(1, 공격력 - 방어력) 후 HP 감소. 성공 시에만 true.</summary>
    public bool ExecuteBasicAttack(BattleCharactor actor, BattleCharactor target)
    {
        if (actor == null || target == null)
        {
            return false;
        }

        if (actor.IsDead || target.IsDead)
        {
            return false;
        }

        float dmg = CalculateBasicAttackDamage(actor, target);
        string actorName = actor.UnitName;
        string targetName = target.UnitName;
        Debug.Log($"{actorName}이 {targetName}에게 {dmg:F1}만큼 피해를 입혔습니다.");
        target.TakeDamage(dmg);

        if (target.IsDead)
        {
            Debug.Log($"[Battle] 처치: {GetLabel(target)}");
        }

        return true;
    }

    public void ExecuteAction(BattleAction action)
    {
        if (action == null)
        {
            Debug.LogWarning("[BattleManager] ExecuteAction: action이 null입니다.");
            return;
        }

        if (action.Actor == null)
        {
            Debug.LogWarning("[BattleManager] ExecuteAction: Actor가 null입니다.");
            return;
        }

        if (action.Actor.IsDead)
        {
            Debug.Log($"[BattleManager] 행동 취소: Actor가 사망 상태입니다. ({GetLabel(action.Actor)})");
            return;
        }

        switch (action.ActionType)
        {
            case BattleActionType.BasicAttack:
                ExecuteBasicAttack(action.Actor, action.Target);
                break;

            case BattleActionType.Skill:
                ExecuteSkill(action.Actor, action.Target, action.SkillData);
                break;

            default:
                Debug.LogWarning($"[BattleManager] 알 수 없는 ActionType: {action.ActionType}");
                break;
        }
    }

    private void ExecuteSkill(BattleCharactor actor, BattleCharactor target, SkillDataAsset skillData)
    {
        if (skillData == null)
        {
            Debug.LogWarning($"[Battle] 스킬 사용 실패: SkillDataAsset이 null입니다. actor={GetLabel(actor)}");
            return;
        }

        float dmg = CalculateSkillDamage(actor, target, skillData);
        Debug.Log($"[Battle] 스킬({skillData.DisplayName}): {GetLabel(actor)} -> {GetLabel(target)} dmg={dmg:F1}");
        target.TakeDamage(dmg);

        if (target.IsDead)
        {
            Debug.Log($"[Battle] 처치: {GetLabel(target)}");
        }
    }

    public float CalculateBasicAttackDamage(BattleCharactor actor, BattleCharactor target)
    {
        if (actor == null || target == null)
        {
            return 1f;
        }

        float atk = actor.FinalStats.Atk;
        float def = target.FinalStats.DEF;

        float raw = atk - def;
        return Mathf.Max(1.0f, raw);
    }

    public float CalculateSkillDamage(BattleCharactor actor, BattleCharactor target, SkillDataAsset skillData)
    {
        if (actor == null || target == null)
        {
            return 1f;
        }

        if (skillData == null)
        {
            return 1f;
        }

        float atk = actor.FinalStats.Atk;
        float def = target.FinalStats.DEF;

        float multiplier = Mathf.Max(0.01f, skillData.Power / 100f);
        float raw = (atk * multiplier) - def;
        return Mathf.Max(1.0f, raw);
    }

    private static string GetLabel(BattleCharactor unit)
    {
        if (unit == null)
        {
            return "null";
        }

        string side = unit.IsPlayer ? "P" : "E";
        string id = string.IsNullOrWhiteSpace(unit.UnitId) ? "Unknown" : unit.UnitId;
        string name = string.IsNullOrWhiteSpace(unit.UnitName) ? "Unknown" : unit.UnitName;
        return $"{side}:{id}:{name}";
    }
}
