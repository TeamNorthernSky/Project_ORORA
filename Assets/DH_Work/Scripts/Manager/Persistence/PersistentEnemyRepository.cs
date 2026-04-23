using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PersistentEnemyRepository : MonoBehaviour
{
    public static PersistentEnemyRepository Instance { get; private set; }

    [Header("Persistent Enemies")]
    [SerializeField] private List<EnemyPersistentData> enemies = new List<EnemyPersistentData>();

    private readonly Dictionary<string, EnemyPersistentData> enemyLookup = new Dictionary<string, EnemyPersistentData>();

    public IReadOnlyList<EnemyPersistentData> Enemies => enemies;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        RebuildLookup();
    }

    private void OnValidate()
    {
        RebuildLookup();
    }

    public void RegisterOrUpdateEnemy(string enemyId, IReadOnlyList<int> combatUnitIndices)
    {
        if (string.IsNullOrWhiteSpace(enemyId))
            return;

        if (enemyLookup.TryGetValue(enemyId, out EnemyPersistentData existingData))
        {
            existingData.SetCombatUnitIndices(combatUnitIndices);
            return;
        }

        var newData = new EnemyPersistentData(enemyId, combatUnitIndices);
        enemies.Add(newData);
        enemyLookup[enemyId] = newData;
    }

    public bool ContainsEnemy(string enemyId)
    {
        return !string.IsNullOrWhiteSpace(enemyId) && enemyLookup.ContainsKey(enemyId);
    }

    public bool TryGetEnemy(string enemyId, out EnemyPersistentData data)
    {
        if (string.IsNullOrWhiteSpace(enemyId))
        {
            data = null;
            return false;
        }

        return enemyLookup.TryGetValue(enemyId, out data);
    }

    public bool RemoveEnemy(string enemyId)
    {
        if (string.IsNullOrWhiteSpace(enemyId) || !enemyLookup.TryGetValue(enemyId, out EnemyPersistentData data))
            return false;

        enemyLookup.Remove(enemyId);
        enemies.Remove(data);
        return true;
    }

    public void ClearAllEnemies()
    {
        enemies.Clear();
        enemyLookup.Clear();
    }

    private void RebuildLookup()
    {
        enemyLookup.Clear();

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyPersistentData data = enemies[i];
            if (data == null || string.IsNullOrWhiteSpace(data.EnemyId))
                continue;

            if (enemyLookup.ContainsKey(data.EnemyId))
            {
                Debug.LogWarning($"PersistentEnemyRepository has duplicate enemyId '{data.EnemyId}'.", this);
                continue;
            }

            enemyLookup.Add(data.EnemyId, data);
        }
    }
}
