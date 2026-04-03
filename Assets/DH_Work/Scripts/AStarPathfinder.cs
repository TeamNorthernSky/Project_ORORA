using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        if (start == goal)
            return new List<Vector2Int> { start };

        if (gridManager != null)
        {
            if (!gridManager.CanEnterCell(goal, goal))
                return null;
        }

        var openSet = new List<Vector2Int> { start };
        var closedSet = new HashSet<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, float> { [start] = 0f };

        while (openSet.Count > 0)
        {
            Vector2Int current = GetLowestF(openSet, gScore, goal);
            if (current == goal)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);
            closedSet.Add(current);

            for (int i = 0; i < GridManager.AxialDirections6.Length; i++)
            {
                Vector2Int dir = GridManager.AxialDirections6[i];
                Vector2Int neighbor = current + dir;

                if (closedSet.Contains(neighbor))
                    continue;

                if (gridManager != null && !gridManager.CanEnterCell(neighbor, goal))
                    continue;

                float tentativeG = GetOrInfinity(gScore, current) + 1f;

                if (!openSet.Contains(neighbor))
                    openSet.Add(neighbor);
                else if (tentativeG >= GetOrInfinity(gScore, neighbor))
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;
            }
        }

        return null;
    }
    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        return GridManager.HexDistance(a, b);
    }

    private static Vector2Int GetLowestF(List<Vector2Int> openSet, Dictionary<Vector2Int, float> gScore, Vector2Int goal)
    {
        Vector2Int best = openSet[0];
        float bestF = GetOrInfinity(gScore, best) + Heuristic(best, goal);

        for (int i = 1; i < openSet.Count; i++)
        {
            Vector2Int node = openSet[i];
            float f = GetOrInfinity(gScore, node) + Heuristic(node, goal);
            if (f < bestF)
            {
                bestF = f;
                best = node;
            }
        }

        return best;
    }

    private static float GetOrInfinity(Dictionary<Vector2Int, float> gScore, Vector2Int node)
    {
        return gScore.TryGetValue(node, out float score) ? score : float.PositiveInfinity;
    }

    private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        var path = new List<Vector2Int> { current };
        while (cameFrom.TryGetValue(current, out Vector2Int prev))
        {
            current = prev;
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
