using System.Collections.Generic;
using UnityEngine;



namespace EnemyAI
{
    public sealed class EAI_20001 : IEnemyAI
    {
        public int Index => 20001;

        public EnemyActionDecision DecideAction(BattleCharactor self, List<BattleCharactor> targets)
        {
            if (self == null || targets == null || targets.Count == 0)
            {
                //Debug.LogWarning(
                //    $"[EnemyAI/Debug] DecideAction early return: selfNull={self == null}, targetsNull={targets == null}, targetsCount={(targets == null ? -1 : targets.Count)}");
                return null;
            }

            BattleCharactor tauntSource = self.GetTauntSource();
            bool tauntInTargets = tauntSource != null && targets.Contains(tauntSource);
            //Debug.Log(
            //    $"[EnemyAI/Debug] Taunt check: self={self.UnitName}, tauntSource={(tauntSource != null ? tauntSource.UnitName : "null")}, tauntDead={(tauntSource != null && tauntSource.IsDead)}, inTargets={tauntInTargets}");
            if (tauntSource != null
                && !tauntSource.IsDead
                && tauntInTargets)
            {
                Debug.Log(
                    $"[EnemyAI/Debug] Taunt forced target selected: self={self.UnitName} -> target={tauntSource.UnitName}");
                return BuildDecision(self, tauntSource);
            }

            // 타겟: 생존 + 반대 진영 중 현재 HP가 가장 낮은 유닛
            BattleCharactor chosenTarget = null;
            for (int i = 0; i < targets.Count; i++)
            {
                BattleCharactor candidate = targets[i];
                if (candidate == null || candidate.IsDead)
                {
                    continue;
                }

                if (candidate.IsPlayer == self.IsPlayer)
                {
                    continue;
                }

                if (chosenTarget == null || candidate.CurrentHp < chosenTarget.CurrentHp)
                {
                    chosenTarget = candidate;
                }
            }

            if (chosenTarget == null)
            {
                //Debug.LogWarning(
                //    $"[EnemyAI/Debug] No chosen target after scan: self={self.UnitName}, targetsCount={targets.Count}");
                return null;
            }

            Debug.Log(
                $"[EnemyAI/Debug] Lowest HP target selected: self={self.UnitName} -> target={chosenTarget.UnitName}, hp={chosenTarget.CurrentHp:0.#}");
            return BuildDecision(self, chosenTarget);
        }

        private static EnemyActionDecision BuildDecision(BattleCharactor self, BattleCharactor chosenTarget)
        {
            EnemyActionType actionType = EnemyActionType.BasicAttack;
            SkillData selectedSkill = null;
            if (self.SelectedSkillData != null)
            {
                actionType = EnemyActionType.ClassSkill;
                selectedSkill = self.SelectedSkillData;
            }
            else if (self.EquippedWeaponData != null)
            {
                actionType = EnemyActionType.WeaponSkill;
            }

            return new EnemyActionDecision
            {
                Target = chosenTarget,
                ActionType = actionType,
                SelectedSkill = selectedSkill
            };
        }
    }
}
