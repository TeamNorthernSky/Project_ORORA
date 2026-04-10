using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyScript : MonoBehaviour, IUnitIdentifier
{
    //툴 사용 하지 않을 때, private로 변경
    public UnitData enemyData;
    public StatBlock currentStats;

    public UnitData Data
    {
        get => enemyData;
        set => enemyData = value;
    }

    public string UnitID
    {
        get
        {
            if (enemyData == null || string.IsNullOrWhiteSpace(enemyData.Index))
            {
                return string.Empty;
            }

            return enemyData.Index.Trim();
        }
    }

    public void Initialize(UnitData data)
    {
        enemyData = data;
        currentStats = data != null ? data.baseStats : default;

        // 디버그 확인용, 이후 제거 — 적 스폰 시 Index/UnitID 주입 여부 확인
        string id = UnitID;
        string nm = data != null ? data.Name : "null";
        Debug.Log($"[EnemyScript] Initialize: name={nm}, id='{id}'");
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
