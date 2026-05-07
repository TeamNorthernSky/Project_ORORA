using System;
using System.Collections.Generic;

[Serializable]
public class CombatEnemyPersistentData
{
    public int EnemyId => enemyId;
    public IReadOnlyList<int> UnitIndices => unitIndices;

    [UnityEngine.SerializeField] private int enemyId;
    [UnityEngine.SerializeField] private List<int> unitIndices = new List<int>();

    public CombatEnemyPersistentData(int enemyId, IReadOnlyList<int> unitIndices)
    {
        this.enemyId = Math.Max(1, enemyId);
        SetUnitIndices(unitIndices);
    }

    public void SetEnemyId(int nextEnemyId)
    {
        enemyId = Math.Max(1, nextEnemyId);
    }

    public void SetUnitIndices(IReadOnlyList<int> source)
    {
        unitIndices.Clear();

        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] > 0)
                unitIndices.Add(source[i]);
        }
    }
}
