using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UnitDataDTO : IUnitData
{
    [SerializeField] private string _unitId;
    [SerializeField] private string _unitName;
    [SerializeField] private string _unitType;
    [SerializeField] private float _maxHp = 1f;
    [SerializeField] private float _attack = 1f;
    [SerializeField] private float _defense;
    [SerializeField] private float _speed;
    [SerializeField] private TeamType _teamType = TeamType.Player;
    [SerializeField] private int _level = 1;
    [SerializeField] private int _unitCount = 1;
    [SerializeField] private List<EquipmentData> _equipments = new List<EquipmentData>();

    public string unitId => _unitId;
    public string unitName => _unitName;
    public float maxHp => Mathf.Max(1f, _maxHp);
    public float attack => Mathf.Max(1f, _attack);
    public float defense => Mathf.Max(0f, _defense);
    public float speed => Mathf.Max(0f, _speed);
    public TeamType teamType => _teamType;

    public string UnitType => _unitType;
    public int Level => Mathf.Max(1, _level);
    public int UnitCount => Mathf.Max(1, _unitCount);
    public IReadOnlyList<EquipmentData> Equipments => _equipments;

    public static UnitDataDTO FromUnitData(UnitData data, TeamType team, int level = 1, int unitCount = 1, List<EquipmentData> equipments = null)
    {
        if (data == null)
        {
            return null;
        }

        return new UnitDataDTO
        {
            _unitId = data.Index,
            _unitName = string.IsNullOrWhiteSpace(data.Name) ? data.UnitType : data.Name,
            _unitType = data.UnitType,
            _maxHp = data.baseStats.HP,
            _attack = data.baseStats.Atk,
            _defense = data.baseStats.DEF,
            _speed = data.baseStats.Speed,
            _teamType = team,
            _level = Mathf.Max(1, level),
            _unitCount = Mathf.Max(1, unitCount),
            _equipments = equipments != null ? new List<EquipmentData>(equipments) : new List<EquipmentData>()
        };
    }
}
