using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;

    private static readonly Vector2Int[] Directions8 =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1),
        new Vector2Int(-1, -1)
    };

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        if(start == goal)
            return new List<Vector2Int> { start };

        if(gridManager != null)
        {
            if (!gridManager.IsWalkable(start) || !gridManager.IsWalkable(goal))
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

            for (int i = 0; i < Directions8.Length; i++)
            {
                Vector2Int dir = Directions8[i];
                Vector2Int neighbor = current + dir;
                if (closedSet.Contains(neighbor))
                    continue;

                if (gridManager != null && !gridManager.IsWalkable(neighbor))
                    continue;

                // 대각선 코너 끼임 방지: (x+dx, y) 와 (x, y+dy) 둘 다 walkable일 때만 허용
                if (gridManager != null && dir.x != 0 && dir.y != 0)
                {
                    Vector2Int sideA = new Vector2Int(current.x + dir.x, current.y);
                    Vector2Int sideB = new Vector2Int(current.x, current.y + dir.y);
                    if (!gridManager.IsWalkable(sideA) || !gridManager.IsWalkable(sideB))
                        continue;
                }

                float stepCost = (Directions8[i].x == 0 || Directions8[i].y == 0) ? 1f : 1.4142135f;
                float tentativeG = GetOrInfinity(gScore, current) + stepCost;

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
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        int min = Mathf.Min(dx, dy);
        int max = Mathf.Max(dx, dy);
        return (max - min) + (1.4142135f * min);
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
