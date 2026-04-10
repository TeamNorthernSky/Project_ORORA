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
                hp: 1f,
                atk: 1f,
                def: 0f,
                luck: 0f,
                speed: 0f,
                criticalRate: 0.01f,
                counterRate: 0.01f,
                avoidRate: 0f);
        }

        StatBlock total =  charactorData.baseStats;

        // 추후에 인스펙터 창을 보정치 조절할 수 있도록 변경하기
        total.Atk *= 1.1f;
        total.DEF *= 0.5f;
        total.HP *= 1.0f;


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
