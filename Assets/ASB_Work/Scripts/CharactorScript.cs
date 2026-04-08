using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharactorScript : MonoBehaviour, IAttackable
{
    private UnitData charactorData;
    private StatBlock currentStats;

    //
    public UnitData Data
    {
        get => charactorData;
        set => charactorData = value;
    }


    public void Initialize(UnitData data)
    {
        charactorData = data;
        currentStats = data != null ? data.baseStats : default;
    }

    void Start()
    {
        
    }

    
    void Update()
    {
        
    }

    void IAttackable.Attack()
    {
        Debug.Log("CharactorScript: Attack!");
    }
}
