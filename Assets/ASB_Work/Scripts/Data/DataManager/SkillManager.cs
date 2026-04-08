using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    [System.Serializable]
    public class CharactorSkillLoadout
    {
        public string charactorId;
        public List<string> equippedSkillIds = new List<string>();
    }

    [Header("Master Data")]
    [SerializeField] private List<SkillData> skillDatabase = new List<SkillData>();

    [Header("Skill Equip State")]
    [SerializeField] private List<CharactorSkillLoadout> loadouts = new List<CharactorSkillLoadout>();

    public SkillData GetSkillData(string skillId)
    {
        return skillDatabase.Find(x => x != null && x.SkillId == skillId);
    }

    public List<SkillData> GetEquippedSkills(string charactorId)
    {
        var result = new List<SkillData>();
        var loadout = loadouts.Find(x => x.charactorId == charactorId);
        if (loadout == null)
        {
            return result;
        }

        for (int i = 0; i < loadout.equippedSkillIds.Count; i++)
        {
            string skillId = loadout.equippedSkillIds[i];
            var data = GetSkillData(skillId);
            if (data != null)
            {
                result.Add(data);
            }
        }

        return result;
    }
}
