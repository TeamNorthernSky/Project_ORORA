using System.Collections.Generic;

public static class StatCalculator
{
    public static StatBlock CalculateFinalStats(
        UnitData charactorData,
        int level,
        List<EquipmentData> equippedEquipments)
    {
        if (charactorData == null)
        {
            return new StatBlock(
                hp: 1,
                atk: 1,
                def: 0,
                luck: 0,
                speed: 0f,
                criticalRate: 0.01f,
                counterRate: 0.01f,
                avoidRate: 0f);
        }

        StatBlock total = charactorData.baseStats;
        //charactorData.BaseStats + (charactorData.GrowthPerLevel * levelOffset);

        if (equippedEquipments != null)
        {
            for (int i = 0; i < equippedEquipments.Count; i++)
            {
                var equipment = equippedEquipments[i];
                if (equipment != null)
                {
                    total += equipment.StatBonus;
                }
            }
        }

        total.ClampToMinimumOne();
        return total;
    }



}
