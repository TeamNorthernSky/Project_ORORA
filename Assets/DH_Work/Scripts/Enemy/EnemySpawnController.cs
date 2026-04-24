using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Transform enemyRoot;

    [Header("Spawn Schedule")]
    [SerializeField] private List<EnemySpawnEntry> spawnEntries = new List<EnemySpawnEntry>();

    private void OnEnable()
    {
        ResolveReferences();

        if (turnManager != null)
            turnManager.DayAdvanced += HandleDayAdvanced;

        TrySpawnDueEntries();
    }

    private void OnDisable()
    {
        if (turnManager != null)
            turnManager.DayAdvanced -= HandleDayAdvanced;
    }

    [ContextMenu("Try Spawn Due Entries")]
    public void TrySpawnDueEntries()
    {
        int currentDay = turnManager != null ? turnManager.GetDay() : 1;
        TrySpawnDueEntries(currentDay);
    }

    private void HandleDayAdvanced(int currentDay)
    {
        TrySpawnDueEntries(currentDay);
    }

    private void TrySpawnDueEntries(int currentDay)
    {
        if (gridManager == null)
            return;

        for (int i = 0; i < spawnEntries.Count; i++)
        {
            EnemySpawnEntry entry = spawnEntries[i];
            if (entry == null || entry.Spawned || entry.EnemyPrefab == null)
                continue;

            if (currentDay < entry.SpawnDay)
                continue;

            if (!gridManager.CanOccupyCell(entry.SpawnGrid, null, true))
            {
                Debug.LogWarning(
                    $"EnemySpawnController could not spawn '{entry.EnemyPrefab.name}' at {entry.SpawnGrid} on day {currentDay} because the cell is blocked.",
                    this);
                continue;
            }

            EnemyUnit spawnedEnemy = Instantiate(entry.EnemyPrefab, Vector3.zero, Quaternion.identity, enemyRoot);
            spawnedEnemy.SnapToGridPosition(entry.SpawnGrid);
            entry.MarkSpawned();
        }
    }

    private void ResolveReferences()
    {
        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>();

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();
    }

    [Serializable]
    private sealed class EnemySpawnEntry
    {
        [SerializeField] private EnemyUnit enemyPrefab;
        [SerializeField, Min(1)] private int spawnDay = 1;
        [SerializeField] private Vector2Int spawnGrid = Vector2Int.zero;
        [SerializeField] private bool spawned;

        public EnemyUnit EnemyPrefab => enemyPrefab;
        public int SpawnDay => Mathf.Max(1, spawnDay);
        public Vector2Int SpawnGrid => spawnGrid;
        public bool Spawned => spawned;

        public void MarkSpawned()
        {
            spawned = true;
        }
    }
}
