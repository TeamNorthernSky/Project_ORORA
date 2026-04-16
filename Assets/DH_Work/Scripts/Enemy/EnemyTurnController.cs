using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTurnController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyRegistry enemyRegistry;
    [SerializeField] private PartyRegistry partyRegistry;
    [SerializeField] private CastleRegistry castleRegistry;
    [SerializeField] private AStarPathfinder pathfinder;
    [SerializeField] private CombatEncounterManager combatEncounterManager;
    [SerializeField] private GridManager gridManager;

    private readonly List<TargetCandidate> resourceCandidates = new List<TargetCandidate>();
    private readonly List<TargetCandidate> strategicCandidates = new List<TargetCandidate>();

    private readonly struct TargetCandidate
    {
        public TargetCandidate(EnemyTargetType targetType, Component target, Vector2Int grid)
        {
            TargetType = targetType;
            Target = target;
            Grid = grid;
        }

        public EnemyTargetType TargetType { get; }
        public Component Target { get; }
        public Vector2Int Grid { get; }
    }

    public IEnumerator ExecuteEnemyTurn()
    {
        ResolveReferences();

        if (enemyRegistry == null || partyRegistry == null || pathfinder == null)
            yield break;

        IReadOnlyList<EnemyUnit> enemies = enemyRegistry.Enemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyUnit enemy = enemies[i];
            if (enemy == null)
                continue;

            if (HandleAdjacentMineInteraction(enemy))
                continue;

            if (HandleAdjacentItemInteraction(enemy))
                continue;

            ValidateCurrentTarget(enemy);

            if (!enemy.HasTarget())
                AcquireTarget(enemy);

            if (!enemy.HasTarget())
                continue;

            PartyGridMover adjacentParty = FindAdjacentParty(enemy.GetCurrentGrid());
            if (adjacentParty != null && TryBeginCombat(adjacentParty, enemy))
                yield break;

            Vector2Int targetGrid = GetTargetGrid(enemy.CurrentTargetType, enemy.CurrentTarget);
            List<Vector2Int> fullPath = FindApproachPath(
                enemy,
                enemy.CurrentTargetType,
                enemy.CurrentTarget,
                targetGrid);
            List<Vector2Int> movePath = TrimPathToMovePoints(fullPath, enemy.MovePointsPerTurn);

            if (movePath != null && movePath.Count > 1)
                yield return enemy.MoveAlongPath(movePath);

            if (HandleAdjacentMineInteraction(enemy))
                continue;

            if (HandleAdjacentItemInteraction(enemy))
                continue;

            adjacentParty = FindAdjacentParty(enemy.GetCurrentGrid());
            if (adjacentParty != null && TryBeginCombat(adjacentParty, enemy))
                yield break;
        }
    }

    private void ValidateCurrentTarget(EnemyUnit enemy)
    {
        if (enemy == null || !enemy.HasTarget())
            return;

        if (enemy.CurrentTarget == null)
        {
            enemy.ClearTarget();
            return;
        }

        Vector2Int enemyGrid = enemy.GetCurrentGrid();
        Vector2Int targetGrid = GetTargetGrid(enemy.CurrentTargetType, enemy.CurrentTarget);

        if (IsTargetReached(enemyGrid, enemy.CurrentTargetType, enemy.CurrentTarget, targetGrid))
        {
            enemy.ClearTarget();
            return;
        }

        if (GridManager.GridDistance(enemyGrid, targetGrid) > enemy.DetectionRange)
            enemy.ClearTarget();
    }

    private void AcquireTarget(EnemyUnit enemy)
    {
        resourceCandidates.Clear();
        strategicCandidates.Clear();

        Vector2Int enemyGrid = enemy.GetCurrentGrid();
        CollectResourceCandidates(enemyGrid, enemy.DetectionRange);
        CollectStrategicCandidates(enemyGrid, enemy.DetectionRange);

        TargetCandidate? selectedCandidate = SelectCandidate(enemy, enemyGrid);
        if (selectedCandidate.HasValue)
        {
            enemy.SetTarget(selectedCandidate.Value.TargetType, selectedCandidate.Value.Target);
            return;
        }

        CastleUnit fallbackCastle = castleRegistry != null ? castleRegistry.GetClosestCastle(enemyGrid) : null;
        if (fallbackCastle != null)
            enemy.SetTarget(EnemyTargetType.Castle, fallbackCastle);
        else
            enemy.ClearTarget();
    }

    private TargetCandidate? SelectCandidate(EnemyUnit enemy, Vector2Int enemyGrid)
    {
        bool hasResourceCandidates = resourceCandidates.Count > 0;
        bool hasStrategicCandidates = strategicCandidates.Count > 0;

        if (!hasResourceCandidates && !hasStrategicCandidates)
            return null;

        if (hasResourceCandidates && !hasStrategicCandidates)
            return GetHighestPriorityCandidate(resourceCandidates, enemyGrid);

        if (!hasResourceCandidates && hasStrategicCandidates)
            return GetHighestPriorityCandidate(strategicCandidates, enemyGrid);

        int totalWeight = enemy.ResourceGroupWeight + enemy.StrategicGroupWeight;
        if (totalWeight <= 0)
            return GetHighestPriorityCandidate(strategicCandidates, enemyGrid);

        int roll = Random.Range(0, totalWeight);
        bool chooseResourceGroup = roll < enemy.ResourceGroupWeight;

        return chooseResourceGroup
            ? GetHighestPriorityCandidate(resourceCandidates, enemyGrid)
            : GetHighestPriorityCandidate(strategicCandidates, enemyGrid);
    }

    private TargetCandidate? GetHighestPriorityCandidate(List<TargetCandidate> candidates, Vector2Int enemyGrid)
    {
        if (candidates == null || candidates.Count == 0)
            return null;

        TargetCandidate? prioritizedCandidate = null;
        for (int i = 0; i < candidates.Count; i++)
        {
            TargetCandidate candidate = candidates[i];
            if (!prioritizedCandidate.HasValue)
            {
                prioritizedCandidate = candidate;
                continue;
            }

            if (GetPriority(candidate.TargetType) < GetPriority(prioritizedCandidate.Value.TargetType))
            {
                prioritizedCandidate = candidate;
                continue;
            }

            if (GetPriority(candidate.TargetType) == GetPriority(prioritizedCandidate.Value.TargetType)
                && GridManager.GridDistance(enemyGrid, candidate.Grid)
                    < GridManager.GridDistance(enemyGrid, prioritizedCandidate.Value.Grid))
            {
                prioritizedCandidate = candidate;
            }
        }

        return prioritizedCandidate;
    }

    private void CollectResourceCandidates(Vector2Int enemyGrid, int detectionRange)
    {
        Mine[] mines = FindObjectsByType<Mine>(FindObjectsSortMode.None);
        for (int i = 0; i < mines.Length; i++)
        {
            Mine mine = mines[i];
            if (mine == null || gridManager == null)
                continue;

            Vector2Int targetGrid = gridManager.WorldToGrid(mine.transform.position);
            if (!IsWithinDetectionRange(enemyGrid, targetGrid, detectionRange) || IsAdjacent(enemyGrid, targetGrid))
                continue;

            resourceCandidates.Add(new TargetCandidate(EnemyTargetType.Mine, mine, targetGrid));
        }

        ItemObject[] items = FindObjectsByType<ItemObject>(FindObjectsSortMode.None);
        for (int i = 0; i < items.Length; i++)
        {
            ItemObject item = items[i];
            if (item == null)
                continue;

            Vector2Int targetGrid = GetTargetGrid(EnemyTargetType.Item, item);
            if (!IsWithinDetectionRange(enemyGrid, targetGrid, detectionRange) || IsAdjacent(enemyGrid, targetGrid))
                continue;

            resourceCandidates.Add(new TargetCandidate(EnemyTargetType.Item, item, targetGrid));
        }
    }

    private void CollectStrategicCandidates(Vector2Int enemyGrid, int detectionRange)
    {
        if (castleRegistry != null)
        {
            IReadOnlyList<CastleUnit> castles = castleRegistry.Castles;
            for (int i = 0; i < castles.Count; i++)
            {
                CastleUnit castle = castles[i];
                if (castle == null)
                    continue;

                Vector2Int targetGrid = castle.GetCurrentGrid();
                if (!IsWithinDetectionRange(enemyGrid, targetGrid, detectionRange)
                    || (gridManager != null && gridManager.IsAdjacentToCastle(enemyGrid, castle)))
                    continue;

                strategicCandidates.Add(new TargetCandidate(EnemyTargetType.Castle, castle, targetGrid));
            }
        }

        PartyGridMover[] parties = partyRegistry.PartyMovers;
        for (int i = 0; i < parties.Length; i++)
        {
            PartyGridMover party = parties[i];
            if (party == null)
                continue;

            Vector2Int targetGrid = party.GetCurrentGrid();
            if (!IsWithinDetectionRange(enemyGrid, targetGrid, detectionRange) || IsAdjacent(enemyGrid, targetGrid))
                continue;

            strategicCandidates.Add(new TargetCandidate(EnemyTargetType.Party, party, targetGrid));
        }
    }

    private PartyGridMover FindAdjacentParty(Vector2Int enemyGrid)
    {
        PartyGridMover[] parties = partyRegistry.PartyMovers;
        for (int i = 0; i < parties.Length; i++)
        {
            PartyGridMover party = parties[i];
            if (party == null)
                continue;

            if (IsAdjacent(enemyGrid, party.GetCurrentGrid()))
                return party;
        }

        return null;
    }

    private bool TryBeginCombat(PartyGridMover party, EnemyUnit enemy)
    {
        if (combatEncounterManager == null)
            return false;

        if (!IsAdjacent(party.GetCurrentGrid(), enemy.GetCurrentGrid()))
            return false;

        return combatEncounterManager.BeginCombat(party, enemy);
    }

    private List<Vector2Int> FindApproachPath(
        EnemyUnit enemy,
        EnemyTargetType targetType,
        Component target,
        Vector2Int targetGrid)
    {
        Vector2Int enemyGrid = enemy.GetCurrentGrid();
        List<Vector2Int> bestPath = null;
        Vector2Int bestApproachGrid = enemyGrid;

        List<Vector2Int> approachCandidates = GetApproachCandidates(targetType, target, targetGrid);
        for (int i = 0; i < approachCandidates.Count; i++)
        {
            Vector2Int candidateApproachGrid = approachCandidates[i];
            List<Vector2Int> candidatePath = pathfinder.FindPath(
                enemyGrid,
                candidateApproachGrid,
                enemy.transform,
                true);
            if (candidatePath == null || candidatePath.Count <= 1)
                continue;

            if (bestPath == null || candidatePath.Count < bestPath.Count)
            {
                bestPath = candidatePath;
                bestApproachGrid = candidateApproachGrid;
                continue;
            }

            if (bestPath != null
                && candidatePath.Count == bestPath.Count
                && IsBetterAlignedApproach(enemyGrid, targetGrid, candidateApproachGrid, bestApproachGrid))
            {
                bestPath = candidatePath;
                bestApproachGrid = candidateApproachGrid;
            }
        }

        return bestPath;
    }

    private List<Vector2Int> GetApproachCandidates(EnemyTargetType targetType, Component target, Vector2Int targetGrid)
    {
        if (targetType == EnemyTargetType.Castle && target is CastleUnit castle && gridManager != null)
        {
            MultiGridOccupant occupant = castle.GetComponent<MultiGridOccupant>();
            if (occupant != null)
                return new List<Vector2Int>(occupant.GetAdjacentOuterCells());
        }

        List<Vector2Int> approachCandidates = new List<Vector2Int>(GridManager.Directions8.Length);
        for (int i = 0; i < GridManager.Directions8.Length; i++)
            approachCandidates.Add(targetGrid + GridManager.Directions8[i]);

        return approachCandidates;
    }

    private Vector2Int GetTargetGrid(EnemyTargetType targetType, Component target)
    {
        switch (targetType)
        {
            case EnemyTargetType.Mine:
                if (target is PartyGridMover party)
                    return party.GetCurrentGrid();

                if (target is CastleUnit castle)
                    return castle.GetCurrentGrid();
                break;
            case EnemyTargetType.Item:
            case EnemyTargetType.Party:
            case EnemyTargetType.Castle:
                if (target is PartyGridMover typedParty)
                    return typedParty.GetCurrentGrid();

                if (target is CastleUnit typedCastle)
                    return typedCastle.GetCurrentGrid();
                break;
        }

        return target != null && gridManager != null
            ? gridManager.WorldToGrid(target.transform.position)
            : Vector2Int.zero;
    }

    private static bool IsWithinDetectionRange(Vector2Int enemyGrid, Vector2Int targetGrid, int detectionRange)
    {
        return GridManager.GridDistance(enemyGrid, targetGrid) <= detectionRange;
    }

    private static bool IsAdjacent(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return dx <= 1 && dy <= 1;
    }

    private bool IsTargetReached(
        Vector2Int enemyGrid,
        EnemyTargetType targetType,
        Component target,
        Vector2Int targetGrid)
    {
        if (targetType == EnemyTargetType.Castle && target is CastleUnit castle && gridManager != null)
            return gridManager.IsAdjacentToCastle(enemyGrid, castle);

        return IsAdjacent(enemyGrid, targetGrid);
    }

    private static int GetPriority(EnemyTargetType targetType)
    {
        switch (targetType)
        {
            case EnemyTargetType.Mine:
            case EnemyTargetType.Castle:
                return 0;
            case EnemyTargetType.Item:
            case EnemyTargetType.Party:
                return 1;
            default:
                return int.MaxValue;
        }
    }

    private static List<Vector2Int> TrimPathToMovePoints(List<Vector2Int> path, int movePoints)
    {
        if (path == null || path.Count == 0)
            return path;

        int clampedSteps = Mathf.Clamp(movePoints, 0, Mathf.Max(0, path.Count - 1));
        int allowedNodeCount = clampedSteps + 1;
        if (allowedNodeCount >= path.Count)
            return path;

        return path.GetRange(0, allowedNodeCount);
    }

    private bool HandleAdjacentItemInteraction(EnemyUnit enemy)
    {
        if (enemy == null || gridManager == null)
            return false;

        if (!gridManager.TryGetAdjacentItemGrid(enemy.GetCurrentGrid(), out Vector2Int itemGrid))
            return false;

        if (!gridManager.TryGetItemObjectAtGrid(itemGrid, out ItemObject item))
            return false;

        if (enemy.CurrentTarget == item)
            enemy.ClearTarget();

        item.RemoveWithoutReward();
        return true;
    }

    private bool HandleAdjacentMineInteraction(EnemyUnit enemy)
    {
        if (enemy == null || gridManager == null)
            return false;

        if (!gridManager.TryGetAdjacentMineGrid(enemy.GetCurrentGrid(), out Vector2Int mineGrid))
            return false;

        if (!gridManager.TryGetMineObjectAtGrid(mineGrid, out Mine mine))
            return false;

        if (mine.IsEnemyClaimed)
            return false;

        if (enemy.CurrentTarget == mine)
            enemy.ClearTarget();

        mine.EnemyClaim();
        return true;
    }

    private static bool IsBetterAlignedApproach(
        Vector2Int enemyGrid,
        Vector2Int targetGrid,
        Vector2Int candidateApproachGrid,
        Vector2Int currentBestApproachGrid)
    {
        int candidateAlignment = GetApproachAlignmentScore(enemyGrid, targetGrid, candidateApproachGrid);
        int currentAlignment = GetApproachAlignmentScore(enemyGrid, targetGrid, currentBestApproachGrid);
        if (candidateAlignment != currentAlignment)
            return candidateAlignment > currentAlignment;

        int candidateDistance = GridManager.GridDistance(enemyGrid, candidateApproachGrid);
        int currentDistance = GridManager.GridDistance(enemyGrid, currentBestApproachGrid);
        if (candidateDistance != currentDistance)
            return candidateDistance < currentDistance;

        int candidateManhattan =
            Mathf.Abs(targetGrid.x - candidateApproachGrid.x) + Mathf.Abs(targetGrid.y - candidateApproachGrid.y);
        int currentManhattan =
            Mathf.Abs(targetGrid.x - currentBestApproachGrid.x) + Mathf.Abs(targetGrid.y - currentBestApproachGrid.y);
        return candidateManhattan < currentManhattan;
    }

    private static int GetApproachAlignmentScore(Vector2Int enemyGrid, Vector2Int targetGrid, Vector2Int approachGrid)
    {
        Vector2Int toTarget = targetGrid - enemyGrid;
        Vector2Int toApproach = approachGrid - enemyGrid;

        int dot = toTarget.x * toApproach.x + toTarget.y * toApproach.y;
        int axisMatch = 0;

        if (toTarget.x != 0 && toApproach.x != 0 && Mathf.Sign(toTarget.x) == Mathf.Sign(toApproach.x))
            axisMatch++;

        if (toTarget.y != 0 && toApproach.y != 0 && Mathf.Sign(toTarget.y) == Mathf.Sign(toApproach.y))
            axisMatch++;

        return dot * 10 + axisMatch;
    }

    private void ResolveReferences()
    {
        if (enemyRegistry == null)
            enemyRegistry = FindFirstObjectByType<EnemyRegistry>();

        if (partyRegistry == null)
            partyRegistry = FindFirstObjectByType<PartyRegistry>();

        if (castleRegistry == null)
            castleRegistry = FindFirstObjectByType<CastleRegistry>();

        if (pathfinder == null)
            pathfinder = FindFirstObjectByType<AStarPathfinder>();

        if (combatEncounterManager == null)
            combatEncounterManager = FindFirstObjectByType<CombatEncounterManager>();

        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();
    }
}
