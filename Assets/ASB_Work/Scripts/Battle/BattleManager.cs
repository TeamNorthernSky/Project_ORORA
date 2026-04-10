using UnityEngine;

/// <summary>
/// BattleAction을 실행하는 전투 실행기.
/// - BattleCharactor(상태)는 수정하지 않고 사용
/// - 행동 분기/데미지 계산/로그만 담당
/// </summary>
[DisallowMultipleComponent]
public class BattleManager : MonoBehaviour
{
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

    private void ExecuteBasicAttack(BattleCharactor actor, BattleCharactor target)
    {
        if (target == null)
        {
            Debug.LogWarning("[BattleManager] 기본공격 실패: Target이 null입니다.");
            return;
        }

        if (target.IsDead)
        {
            Debug.LogWarning("[BattleManager] 기본공격 실패: Target이 이미 사망 상태입니다.");
            return;
        }

        float dmg = CalculateBasicAttackDamage(actor, target);
        string actorName = actor != null && actor.UnitData != null ? actor.UnitData.Name : "Unknown";
        string targetName = target.UnitData != null ? target.UnitData.Name : "Unknown";
        Debug.Log($"{actorName}이 {targetName}에게 {dmg:F1}만큼 피해를 입혔습니다.");
        target.TakeDamage(dmg);

        if (target.IsDead)
        {
            Debug.Log($"[Battle] 처치: {GetLabel(target)}");
        }
    }

    private void ExecuteSkill(BattleCharactor actor, BattleCharactor target, SkillData skillData)
    {
        if (skillData == null)
        {
            Debug.LogWarning($"[Battle] 스킬 사용 실패: SkillData가 null입니다. actor={GetLabel(actor)}");
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
        if (actor == null || target == null) return 1f;

        float atk = actor.FinalStats.Atk;
        float def = target.FinalStats.DEF;

        float raw = atk - def;
        return Mathf.Max(1.0f, raw);
    }

    public float CalculateSkillDamage(BattleCharactor actor, BattleCharactor target, SkillData skillData)
    {
        if (actor == null || target == null) return 1f;
        if (skillData == null) return 1f;

        float atk = actor.FinalStats.Atk;
        float def = target.FinalStats.DEF;

        // 가정: SkillData.Power가 "배율(%)" 값이라고 가정 (실제 의미가 다르면 여기만 교체하면 됨)
        float multiplier = Mathf.Max(0.01f, skillData.Power / 100f);
        float raw = (atk * multiplier) - def;
        return Mathf.Max(1.0f, raw);
    }

    private static string GetLabel(BattleCharactor unit)
    {
        if (unit == null || unit.UnitData == null) return "null";
        string side = unit.IsPlayer ? "P" : "E";
        string id = unit.UnitData.Index ?? "Unknown";
        string name = string.IsNullOrWhiteSpace(unit.UnitData.Name) ? "Unknown" : unit.UnitData.Name;
        return $"{side}:{id}:{name}";
    }
}
