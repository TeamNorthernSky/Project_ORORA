using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ClassSkillSheet CSV 행 데이터. skillIndex를 키로 SkillDataLoader에 보관합니다.
/// </summary>
/// 



//public class MappingSkill

[Serializable]
public class SkillData
{
    public int skillIndex;
    public string skillClass;
    public int acquireLevel;
    public string skillName;
    public string description;
    public int classSkillEffect;
    public int classSkillRange;
    public int classSkillTarget;
    public List<Vector2Int> boundary = new List<Vector2Int>();
    public int multiTargetType;
    public int multiTargetCount;
    public float skillValue;
    public float skillSubValue;
}
