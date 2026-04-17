using System.Collections.Generic;



namespace EnemyAI
{
    public sealed class EAI_20001 : IEnemyAI
    {
        public int Index => 20001;

        public EnemyActionDecision DecideAction(BattleCharactor self, List<BattleCharactor> targets)
        {
            if (self == null || targets == null || targets.Count == 0)
            {
                return null;
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
                return null;
            }

            // 행동 우선순위:
            // 1) 클래스 스킬(선택 스킬이 있으면)
            // 2) 무기 스킬(무기가 있으면)
            // 3) 기본 공격
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
