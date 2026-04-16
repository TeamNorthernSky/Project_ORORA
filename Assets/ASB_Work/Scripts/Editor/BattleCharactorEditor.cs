using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BattleCharactor))]
public class BattleCharactorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var battleCharactor = target as BattleCharactor;
        if (battleCharactor == null)
        {
            return;
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("CSV Skill Selection", EditorStyles.boldLabel);

        var enemyScript = battleCharactor.GetComponent<EnemyScript>();
        if (enemyScript != null)
        {
            string enemyName = enemyScript.Data != null ? enemyScript.Data.Name : string.Empty;
            if (!string.IsNullOrWhiteSpace(enemyName))
            {
                battleCharactor.SetUnitNameForSkillMatching(enemyName);
            }
        }

        battleCharactor.RefreshAvailableSkillsForInspector();
        List<SkillData> available = battleCharactor.availableSkills;
        if (available == null || available.Count == 0)
        {
            string resolvedUnitName = battleCharactor.UnitName;
            EditorGUILayout.HelpBox(
                $"해당 이름(unitName)과 매칭되는 스킬이 없습니다.\nunitName='{resolvedUnitName}'\n" +
                "SkillDataLoader 참조 및 skillClass 값을 확인하세요.",
                MessageType.Info);
        }
        else
        {
            int currentSkillIndex = Mathf.Clamp(battleCharactor.SelectedSkillIndex, 0, available.Count - 1);
            string[] skillOptions = BuildSkillOptionLabels(available);
            int nextSkillIndex = EditorGUILayout.Popup("Selected Skill", currentSkillIndex, skillOptions);
            if (nextSkillIndex != battleCharactor.SelectedSkillIndex)
            {
                Undo.RecordObject(battleCharactor, "Change Selected Skill");
                battleCharactor.SetSelectedSkillIndex(nextSkillIndex);
                battleCharactor.ResolveSelectedSkill(false);
                EditorUtility.SetDirty(battleCharactor);
            }

            SkillData selectedSkill = battleCharactor.SelectedSkillData;
            if (selectedSkill == null)
            {
                battleCharactor.ResolveSelectedSkill(false);
                selectedSkill = battleCharactor.SelectedSkillData;
            }

            if (selectedSkill != null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.IntField("Skill Index", selectedSkill.skillIndex);
                    EditorGUILayout.TextField("Skill Name", selectedSkill.skillName);
                    EditorGUILayout.FloatField("Skill Value", selectedSkill.skillValue);
                }
            }
        }

        GUILayout.Space(10f);
        EditorGUILayout.LabelField("Weapon Settings", EditorStyles.boldLabel);

        battleCharactor.RefreshAvailableWeapons();
        battleCharactor.ResolveEquippedWeapon(false);
        List<WeaponData> weapons = battleCharactor.availableWeapons;
        if (weapons == null || weapons.Count == 0)
        {
            EditorGUILayout.HelpBox("해당 직업(unitName)에 장착 가능한 무기가 없습니다.", MessageType.Info);
            return;
        }

        string[] weaponOptions = BuildWeaponOptionLabels(weapons);
        int currentWeaponIndex = Mathf.Clamp(battleCharactor.EquippedWeaponIndex, 0, weapons.Count - 1);

        EditorGUI.BeginChangeCheck();
        int nextWeaponIndex = EditorGUILayout.Popup("Equipped Weapon", currentWeaponIndex, weaponOptions);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(battleCharactor, "Change Weapon");
            battleCharactor.SetEquippedWeaponIndex(nextWeaponIndex);
            EditorUtility.SetDirty(battleCharactor);
        }

        WeaponData equipped = battleCharactor.EquippedWeaponData;
        if (equipped != null)
        {
            EditorGUILayout.Space(4f);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.LabelField("Weapon", EditorStyles.boldLabel);
                EditorGUILayout.TextField("Name", equipped.WeaponName ?? string.Empty);
                string desc = string.IsNullOrWhiteSpace(equipped.WeaponDescription)
                    ? "—"
                    : equipped.WeaponDescription;
                EditorGUILayout.TextField("Description", desc);
                string skillName = string.IsNullOrWhiteSpace(equipped.WeaponSkillName)
                    ? "—"
                    : equipped.WeaponSkillName;
                EditorGUILayout.TextField("Weapon Skill", skillName);
            }
        }
    }

    private static string[] BuildSkillOptionLabels(List<SkillData> skills)
    {
        var labels = new string[skills.Count];
        for (int i = 0; i < skills.Count; i++)
        {
            var s = skills[i];
            if (s == null)
            {
                labels[i] = "(null)";
                continue;
            }

            string name = string.IsNullOrWhiteSpace(s.skillName) ? "Unnamed" : s.skillName;
            labels[i] = $"{s.skillIndex} - {name}";
        }

        return labels;
    }

    private static string[] BuildWeaponOptionLabels(List<WeaponData> weapons)
    {
        var labels = new string[weapons.Count];
        for (int i = 0; i < weapons.Count; i++)
        {
            labels[i] = BuildWeaponDropdownLabel(i, weapons[i]);
        }

        return labels;
    }

    private static string BuildWeaponDropdownLabel(int localIndex, WeaponData w)
    {
        if (w == null)
        {
            return $"[{localIndex}] (null)";
        }

        string displayName = string.IsNullOrWhiteSpace(w.WeaponName) ? "Unnamed" : w.WeaponName;
        var sb = new StringBuilder();
        sb.Append('[').Append(localIndex).Append("] ").Append(displayName);

        string bonus = FormatWeaponBonusInline(w);
        if (!string.IsNullOrEmpty(bonus))
        {
            sb.Append(" (").Append(bonus).Append(')');
        }

        return sb.ToString();
    }

    private static string FormatWeaponBonusInline(WeaponData w)
    {
        var parts = new List<string>(4);
        AppendBonus(parts, "HP", w.BonusHP);
        AppendBonus(parts, "ATK", w.BonusATK);
        AppendBonus(parts, "DEF", w.BonusDEF);
        AppendBonus(parts, "SPD", w.BonusSpeed);
        if (parts.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(", ", parts);
    }

    private static void AppendBonus(List<string> parts, string label, float value)
    {
        if (Mathf.Approximately(value, 0f))
        {
            return;
        }

        string sign = value > 0f ? "+" : string.Empty;
        parts.Add($"{label}:{sign}{value:0.##}");
    }
}
