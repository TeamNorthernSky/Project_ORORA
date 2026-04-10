using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharactorScript : MonoBehaviour, IUnitIdentifier
{
    //툴 사용 하지 않을 때, private로 변경
    public UnitData charactorData;
    public StatBlock currentStats;
   [SerializeField] public int Level;
   [SerializeField] public int UnitNumber;


    public UnitData Data
    {
        get => charactorData;
        set => charactorData = value;
    }

    public string UnitID
    {
        get
        {
            if (charactorData == null || string.IsNullOrWhiteSpace(charactorData.Index))
            {
                return string.Empty;
            }

            return charactorData.Index.Trim();
        }
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

}
