using System;
using UnityEngine;

public class EnemyInteractionController
{
    private readonly GridManager gridManager;
    private readonly PartyRegistry partyRegistry;
    private readonly CombatEncounterManager combatEncounterManager;
    private readonly EnemyUnit ownerEnemy;
    private readonly Func<Vector2Int> currentGridProvider;

    public event Action<PartyGridMover, EnemyUnit> AdjacentPartyDetected;
    public event Action<Mine, EnemyUnit> AdjacentMineDetected;
    public event Action<ItemObject, EnemyUnit> AdjacentItemDetected;
    public event Action<CastleUnit, EnemyUnit> AdjacentCastleDetected;

    public EnemyInteractionController(
        GridManager gridManager,
        PartyRegistry partyRegistry,
        CombatEncounterManager combatEncounterManager,
        EnemyUnit ownerEnemy,
        Func<Vector2Int> currentGridProvider)
    {
        this.gridManager = gridManager;
        this.partyRegistry = partyRegistry;
        this.combatEncounterManager = combatEncounterManager;
        this.ownerEnemy = ownerEnemy;
        this.currentGridProvider = currentGridProvider;
    }

    public void HandleGridEntered(Vector2Int enteredGrid)
    {
        if (gridManager == null || ownerEnemy == null)
            return;

        HandleAdjacentPartyProximity(enteredGrid);
        HandleAdjacentMineProximity(enteredGrid);
        HandleAdjacentItemProximity(enteredGrid);
        HandleAdjacentCastleProximity(enteredGrid);
    }

    public void HandleCurrentGrid()
    {
        if (currentGridProvider == null)
            return;

        HandleGridEntered(currentGridProvider());
    }

    private void HandleAdjacentPartyProximity(Vector2Int enteredGrid)
    {
        if (partyRegistry == null)
            return;

        PartyGridMover[] parties = partyRegistry.PartyMovers;
        for (int i = 0; i < parties.Length; i++)
        {
            PartyGridMover party = parties[i];
            if (party == null || !IsAdjacentOrSame(enteredGrid, party.GetCurrentGrid()))
                continue;

            AdjacentPartyDetected?.Invoke(party, ownerEnemy);
            combatEncounterManager?.BeginCombat(party, ownerEnemy);
            return;
        }
    }

    private void HandleAdjacentMineProximity(Vector2Int enteredGrid)
    {
        if (!gridManager.TryGetAdjacentMineGrid(enteredGrid, out Vector2Int mineGrid))
            return;

        if (!gridManager.TryGetMineObjectAtGrid(mineGrid, out Mine mine))
            return;

        AdjacentMineDetected?.Invoke(mine, ownerEnemy);
    }

    private void HandleAdjacentItemProximity(Vector2Int enteredGrid)
    {
        if (!gridManager.TryGetAdjacentItemGrid(enteredGrid, out Vector2Int itemGrid))
            return;

        if (!gridManager.TryGetItemObjectAtGrid(itemGrid, out ItemObject item))
            return;

        AdjacentItemDetected?.Invoke(item, ownerEnemy);
    }

    private void HandleAdjacentCastleProximity(Vector2Int enteredGrid)
    {
        for (int i = 0; i < GridManager.Directions8.Length; i++)
        {
            Vector2Int candidate = enteredGrid + GridManager.Directions8[i];
            if (!gridManager.TryGetCastleObjectAtGrid(candidate, out CastleUnit castle))
                continue;

            AdjacentCastleDetected?.Invoke(castle, ownerEnemy);
            return;
        }
    }

    private static bool IsAdjacentOrSame(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return dx <= 1 && dy <= 1;
    }
}
