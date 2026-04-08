using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{

    private UnitData enemyData;
    private StatBlock currentStats;

    //
    public UnitData Data
    {
        get => enemyData;
        set => enemyData = value;
    }


    public void Initialize(UnitData data)
    {
        enemyData = data;
        currentStats = data != null ? data.baseStats : default;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
