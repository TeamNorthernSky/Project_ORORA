using UnityEngine;

namespace ASB.Work.Battle.SkillExecution
{
    /// <summary>
    /// 기본 힐 블록만 호출
    /// </summary>

    public sealed class HealSkillHandler : BaseSingleSkillHandler
    {
        protected override void ApplyHeal(BattleCharactor caster, BattleCharactor target, SkillData skillData)
        {
            float heal = SkillEffectHelper.ApplyStandardHeal(caster, target, skillData.skillValue);
            Debug.Log($"[Skill/DefaultHeal] {caster.UnitName} -> {target.UnitName} heal={heal:F1}");
        }
    }

    public sealed class TargetLowerHPMoreHeal : BaseSingleSkillHandler
    {
        protected override void ApplyHeal(BattleCharactor caster, BattleCharactor target, SkillData skillData)
        {
            // HP 비율을 20% 단위로 끊어서 15의 추가 힐량을 얻음
            float hpRatio = (1.0f - target.CurrentHp / (float)target.MaxHp);
            float snapped = Mathf.Round(hpRatio / 0.2f) * 0.2f;
            float totalSkillValue = Mathf.Clamp(snapped * 15 + 5, 5, 50);
            float heal = SkillEffectHelper.ApplyStandardHeal(caster, target, totalSkillValue);
            Debug.Log($"[Skill/DefaultHeal] {caster.UnitName} -> {target.UnitName} heal={heal:F1}");
        }
    }


    //부활 스킬
    public sealed class RebirthSkillHandler : BaseSingleSkillHandler
    {
        protected override void ApplyHeal(BattleCharactor caster, BattleCharactor target, SkillData skillData)
        {
            if (!target.IsDead)
            {
                return;
            }
            //float ratio = SkillEffectHelper.ResolveReviveHpRatio(skillData.skillValue);
            target.Revive(0.2f);
            Debug.Log($"[Skill/Rebirth] {caster.UnitName} -> {target.UnitName} hpRatio={0.2f:0.###}");
        }
    }

    // 타깃 + 랜덤 주변힐
    public sealed class HealTargetAroundRandomHandler : TargetAroundRandom
    {
        protected override void ApplyAdditionaDamage(BattleCharactor caster, BattleCharactor target, SkillData skillData)
        {
            float heal = SkillEffectHelper.ApplyStandardHeal(caster, target, skillData.skillValue);
            Debug.Log($"[Skill/DefaultHeal] {caster.UnitName} -> {target.UnitName} heal={heal:F1}");
        }
    }

    /// <summary>
    /// 광역 흡혈 스킬:
    /// - 범위 내 각 대상에게 데미지를 주고
    /// - 이번 스킬로 가한 총 데미지 합만큼 시전자를 회복합니다.
    /// </summary>
    public sealed class AoEVampiricDamageSkillHandler : BaseAoESkillHandler
    {
        private BattleCharactor trackingCaster;
        private float totalDamageDealt;
        private int processedCount;

        protected override void ApplyAdditionaDamage(BattleCharactor caster, BattleCharactor target, SkillData skillData, int Count)
        {
            if (trackingCaster != caster || processedCount >= Count)
            {
                trackingCaster = caster;
                totalDamageDealt = 0f;
                processedCount = 0;
            }

            float attackPower = caster.FinalStats.Atk;
            float defensePower = target.FinalStats.DEF;
            float multiplier = Mathf.Max(0f, skillData.skillValue);
            float rawDamage = (attackPower * multiplier) - defensePower;
            float finalDamage = Mathf.Max(0f, rawDamage);

            target.TakeDamage(finalDamage);
            totalDamageDealt += finalDamage;
            processedCount++;

            // TODO: 타겟 피격 VFX/SFX 연출 연결
            Debug.Log($"[Skill/AoEVampiric] hit {caster.UnitName} -> {target.UnitName} dmg={finalDamage:F1}");

            // 현재 실행 사이클의 마지막 타겟 처리 시, 누적 피해량만큼 시전자 회복
            if (processedCount >= Count)
            {
                caster.ApplyHeal(totalDamageDealt);
                // TODO: 시전자 흡혈 회복 VFX/SFX 연출 연결
                Debug.Log($"[Skill/AoEVampiric] heal caster={caster.UnitName} totalHeal={totalDamageDealt:F1}");

                totalDamageDealt = 0f;
                processedCount = 0;
                trackingCaster = null;
            }
        }
    }

}
