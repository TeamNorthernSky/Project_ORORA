using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRegistry : MonoBehaviour
{
    private readonly List<EnemyUnit> enemies = new List<EnemyUnit>();

    public IReadOnlyList<EnemyUnit> Enemies => enemies;

    private void Awake()
    {
        RefreshSceneEnemies();
    }

    public void Register(EnemyUnit enemy)
    {
        if (enemy == null || enemies.Contains(enemy))
            return;

        enemies.Add(enemy);
    }

    public void Unregister(EnemyUnit enemy)
    {
        if (enemy == null)
            return;

        enemies.Remove(enemy);
    }

    [ContextMenu("Refresh Scene Enemies")]
    public void RefreshSceneEnemies()
    {
        enemies.Clear();

        EnemyUnit[] sceneEnemies = FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);
        for (int i = 0; i < sceneEnemies.Length; i++)
        {
            EnemyUnit enemy = sceneEnemies[i];
            if (enemy == null)
                continue;

            Register(enemy);
        }
    }

    public EnemyUnit GetClosestEnemy(Vector2Int grid)
    {
        EnemyUnit closest = null;
        int closestDistance = int.MaxValue;

        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];
            if (enemy == null)
                continue;

            int distance = GridManager.GridDistance(grid, enemy.GetCurrentGrid());
            if (distance >= closestDistance)
                continue;

            closest = enemy;
            closestDistance = distance;
        }

        return closest;
    }
}
