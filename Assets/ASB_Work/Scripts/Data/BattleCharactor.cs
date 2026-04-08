using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BattleCharactor
{
    public UnitData UnitData { get; private set; }
    public int Level { get; private set; }
    public StatBlock FinalStats { get; private set; }
    public int CurrentHp { get; private set; }
    public List<EquipmentData> EquippedEquipments { get; private set; }
    public List<SkillData> EquippedSkills { get; private set; }
    public bool IsPlayer { get; set; }
    public bool IsDead { get; private set; }

    public BattleCharactor(
        UnitData charactorData,
        int level
        //List<EquipmentData> equippedEquipments,
        //List<SkillData> equippedSkills
        )
    {
        UnitData = charactorData;
        Level = Mathf.Max(1, level);
        //EquippedEquipments = equippedEquipments ?? new List<EquipmentData>();
        //EquippedSkills = equippedSkills ?? new List<SkillData>();

        RecalculateStats();
        InitializeCurrentHpToMax();
    }

    public void RecalculateStats()
    {
        FinalStats = StatCalculator.CalculateFinalStats(UnitData, Level, EquippedEquipments);
        CurrentHp = Mathf.Clamp(CurrentHp, 0, FinalStats.HP);
    }

    public void InitializeCurrentHpToMax()
    {
        CurrentHp = FinalStats.HP;
    }

    public void LevelUp(int amount = 1)
    {
        Level = Mathf.Max(1, Level + Mathf.Max(1, amount));
        RecalculateStats();
    }

    public void TakeDamage(int amount)
    {
        int damage = Mathf.Max(0, amount);
        CurrentHp = Mathf.Max(0, CurrentHp - damage);
        if (CurrentHp <= 0)
        {
            IsDead = true;
        }
    }

    public void ReviveToFull()
    {
        IsDead = false;
        InitializeCurrentHpToMax();
    }
}
