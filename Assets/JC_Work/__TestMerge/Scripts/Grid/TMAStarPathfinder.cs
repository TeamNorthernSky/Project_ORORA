using System.Collections.Generic;
using UnityEngine;

namespace Orora.TestMerge
{
    /// <summary>
    /// DH AStarPathfinder를 TM용으로 복제. 장애물 판단은 TMGridManager.CanEnterCell에 위임.
    /// </summary>
    public class TMAStarPathfinder : MonoBehaviour
    {
        [SerializeField] private TMGridManager gridManager;

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, int ignoreOccupantId = -1)
        {
            if (start == goal)
                return new List<Vector2Int> { start };

            if (gridManager != null && !gridManager.CanEnterCell(goal, goal, ignoreOccupantId))
                return null;

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

                Vector2Int[] orderedDirections = GetOrderedDirections(current, goal);
                for (int i = 0; i < orderedDirections.Length; i++)
                {
                    Vector2Int dir = orderedDirections[i];
                    Vector2Int neighbor = current + dir;

                    if (closedSet.Contains(neighbor))
                        continue;

                    if (gridManager != null && !gridManager.CanEnterCell(neighbor, goal, ignoreOccupantId))
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

        private static Vector2Int[] GetOrderedDirections(Vector2Int current, Vector2Int goal)
        {
            Vector2Int[] ordered = (Vector2Int[])TMGridManager.Directions8.Clone();
            System.Array.Sort(ordered, (a, b) =>
            {
                Vector2Int nextA = current + a;
                Vector2Int nextB = current + b;

                int distanceCompare = TMGridManager.GridDistance(nextA, goal).CompareTo(TMGridManager.GridDistance(nextB, goal));
                if (distanceCompare != 0) return distanceCompare;

                bool aIsDiagonal = a.x != 0 && a.y != 0;
                bool bIsDiagonal = b.x != 0 && b.y != 0;
                if (aIsDiagonal != bIsDiagonal)
                    return aIsDiagonal ? 1 : -1;

                int manhattanCompare =
                    (Mathf.Abs(goal.x - nextA.x) + Mathf.Abs(goal.y - nextA.y))
                    .CompareTo(Mathf.Abs(goal.x - nextB.x) + Mathf.Abs(goal.y - nextB.y));
                return manhattanCompare;
            });

            return ordered;
        }

        private static float Heuristic(Vector2Int a, Vector2Int b)
        {
            return TMGridManager.GridDistance(a, b);
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
}
