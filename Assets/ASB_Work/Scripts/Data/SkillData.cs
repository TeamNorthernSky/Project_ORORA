using UnityEngine;



public enum SkillTargetType
{
    EnemySingle,
    EnemyFrontSingle,
    EnemyBackSingle,
    EnemyAll,
    AllySingle,
    AllyAll,
    Self,
    SameColumnEnemy,
    FrontRowEnemies,
    BackRowEnemies
}

public enum SkillEffectType
{
    Damage,
    Heal,
    BuffAttack
}

[CreateAssetMenu(fileName = "SkillData", menuName = "ASB/Data/SkillData")]
public class SkillData : ScriptableObject
{
    [SerializeField] private string skillId;
    [SerializeField] private string displayName;
    [TextArea]
    [SerializeField] private string description;
    [SerializeField] private int power;
    [SerializeField] private float coolTime;

    public string SkillId => skillId;
    public string DisplayName => displayName;
    public string Description => description;
    public int Power => power;
    public float CoolTime => coolTime;
}
