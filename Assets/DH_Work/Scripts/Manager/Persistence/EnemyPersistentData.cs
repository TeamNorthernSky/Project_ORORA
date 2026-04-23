using System;
using System.Collections.Generic;

[Serializable]
public class EnemyPersistentData
{
    public string EnemyId => enemyId;
    public IReadOnlyList<int> CombatUnitIndices => combatUnitIndices;

    [UnityEngine.SerializeField] private string enemyId;
    [UnityEngine.SerializeField] private List<int> combatUnitIndices = new List<int>();

    public EnemyPersistentData(string enemyId, IReadOnlyList<int> combatUnitIndices)
    {
        this.enemyId = enemyId ?? string.Empty;
        SetCombatUnitIndices(combatUnitIndices);
    }

    public void SetCombatUnitIndices(IReadOnlyList<int> source)
    {
        combatUnitIndices.Clear();

        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
            combatUnitIndices.Add(source[i]);
    }
}
