using ASB.Work.Battle.Core;
using System.Collections.Generic;
using UnityEngine;

namespace ASB.Work.Battle.SkillExecution
{
    // 데미지 스킬 핸들러
    //싱글 공격일 경우 : BaseSingleSkillHandler
    //광역 공격일 경우 : BaseAoESkillHandler


    //---------------------- 단일 공격!!!


    // 기본 데미지 스킬 핸들러 (스킬값 무시, 단순 데미지 블록만 호출)
    public sealed class DefaultDamageSkillHandler : BaseSingleSkillHandler
    {
        private BattleManager battleManager;

        protected override void ApplyAdditionaDamage(BattleCharactor caster, BattleCharactor target, SkillData skillData)
        {
            float dmg = SkillEffectHelper.ApplyStandardDamage(caster, target, 1.0f);
            Debug.Log($"[Skill/DefaultDamage] {caster.UnitName} -> {target.UnitName} dmg={dmg:F1}");
        }
    }




    // 단일 공격
    public sealed class DamageSkillHandler : BaseSingleSkillHandler
    {
        protected override void ApplyAdditionaDamage(BattleCharactor caster, BattleCharactor target, SkillData skillData)
        {
            // 입력 단계에서 이미 유효 타겟 판정이 끝났으므로, 여기서는 순수 데미지 적용만 수행합니다.
            float attackPower = caster.FinalStats.Atk;
            float defensePower = target.FinalStats.DEF;
            float multiplier = Mathf.Max(0f, skillData.skillValue);
            float rawDamage = (attackPower * multiplier) - defensePower;
            float finalDamage = Mathf.Max(0f, rawDamage);

            target.TakeDamage(finalDamage);

            // TODO: 공격 애니메이션/VFX/SFX 트리거 연결
            //Debug.Log($"[Skill/Damage] {caster.UnitName} -> {target.UnitName} dmg={finalDamage:F1} (atk={attackPower:F1}, mult={multiplier:F2}, def={defensePower:F1})");
        }
    }


    // 대상이 높은 체력일수록 데미지 증가
    public sealed class TargetMoreHPMoreDmg : BaseSingleSkillHandler
    {
        protected override void ApplyAdditionaDamage(BattleCharactor caster, BattleCharactor target, SkillData skillData)
        {
            // HP 비율을 20% 단위로 끊어서 스킬값에 더함
            float hpRatio = (target.CurrentHp / (float)target.MaxHp);
            float snapped = Mathf.Round(hpRatio / 0.2f) * 0.2f;
            float totalSkillValue = snapped + skillData.skillValue;

            float dmg = SkillEffectHelper.ApplyStandardDamage(caster, target, totalSkillValue);
            Debug.Log($"[Skill/DefaultDamage] {caster.UnitName} -> {target.UnitName} dmg={dmg:F1}");
        }
    }



    // HP가 낮을수록 데미지가 증가하는 스킬 핸들러
    public sealed class CasterLowHPMoreDmg : BaseSingleSkillHandler
    {
        protected override void ApplyAdditionaDamage(BattleCharactor caster, BattleCharactor target, SkillData skillData)
        {
            // HP 비율을 20% 단위로 끊어서 스킬값에 더함
            float hpRatio = (1.0f - caster.CurrentHp / (float)caster.MaxHp);
            float snapped = Mathf.Round(hpRatio / 0.2f) * 0.2f;
            float totalSkillValue = snapped + skillData.skillValue;

            float dmg = SkillEffectHelper.ApplyStandardDamage(caster, target, totalSkillValue);
            Debug.Log($"[Skill/DefaultDamage] {caster.UnitName} -> {target.UnitName} dmg={dmg:F1}");
        }
    }

    // 전방 위치에 있을수록 데미지가 증가하는 스킬 핸들러
    public sealed class TargetFrontPosMoreDmg : BaseSingleSkillHandler
    {
        protected override void ApplyAdditionaDamage(BattleCharactor caster, BattleCharactor target, SkillData skillData)
        {
            float totalSkillValue = skillData.skillValue;

            if (target.IsInFrontRow)
                totalSkillValue += 0.2f; // 예: 전방 위치에 있을 경우 50% 추가 데미지

            float dmg = SkillEffectHelper.ApplyStandardDamage(caster, target, totalSkillValue);
            Debug.Log($"[Skill/DefaultDamage] {caster.UnitName} -> {target.UnitName} dmg={dmg:F1}");
        }
    }



    public sealed class TargetBackPosMoreDmg : BaseSingleSkillHandler
    {
        protected override void ApplyAdditionaDamage(BattleCharactor caster, BattleCharactor target, SkillData skillData)
        {
            float totalSkillValue = skillData.skillValue;

            if (!target.IsInFrontRow)
                totalSkillValue  = skillData.skillSubValue; // 예: 전방 위치에 있을 경우 50% 추가 데미지

            float dmg = SkillEffectHelper.ApplyStandardDamage(caster, target, totalSkillValue);
            Debug.Log($"[Skill/DefaultDamage] {caster.UnitName} -> {target.UnitName} dmg={dmg:F1}");
        }
    }




    ///////////////////////광역공격////////////////////////////////

    // 광역 공격
    public sealed class AoEDamageSkillHandler : BaseAoESkillHandler
    {
        protected override void ApplyAdditionaDamage(BattleCharactor caster, BattleCharactor target, SkillData skillData, int Count)
        {
            float dmg = SkillEffectHelper.ApplyStandardDamage(caster, target, skillData.skillValue);
            Debug.Log($"[Skill/DefaultDamage] {caster.UnitName} -> {target.UnitName} dmg={dmg:F1}");
        }
    }


    // 피격된 적 수에 따라 데미지 감소
    public sealed class HitNumLowerDamageHandler : BaseAoESkillHandler
    {

        protected override void ApplyAdditionaDamage(BattleCharactor caster, BattleCharactor target, SkillData skillData, int Count)
        {
            float totalSkillValue = skillData.skillValue + 0.2f * Count;
            float dmg = SkillEffectHelper.ApplyStandardDamage(caster, target, totalSkillValue);
            Debug.Log($"[Skill/DefaultDamage] {caster.UnitName} -> {target.UnitName} dmg={dmg:F1}");
        }
    }



    //------------------- 단일 + 랜덤
    public sealed class HitTargetAroundRandomHandler : TargetAroundRandom
    {

        protected override void ApplyAdditionaDamage(BattleCharactor caster, BattleCharactor target, SkillData skillData)
        {
           
            float dmg = SkillEffectHelper.ApplyStandardDamage(caster, target, skillData.skillValue);
            Debug.Log($"[Skill/DefaultDamage] {caster.UnitName} -> {target.UnitName} dmg={dmg:F1}");
        }
    }

}
