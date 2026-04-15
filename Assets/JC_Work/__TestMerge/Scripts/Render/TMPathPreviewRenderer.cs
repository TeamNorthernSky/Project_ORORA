using System.Collections.Generic;
using UnityEngine;

namespace Orora.TestMerge
{
    public class TMPathPreviewRenderer : MonoBehaviour
    {
        [Header("Line Renderers")]
        [SerializeField] private LineRenderer reachableLineRenderer;
        [SerializeField] private LineRenderer unreachableLineRenderer;

        [Header("Style")]
        [SerializeField] private float width = 0.08f;
        [SerializeField] private Color reachableColor = Color.green;
        [SerializeField] private Color unreachableColor = Color.red;
        [SerializeField] private bool drawWhenPathValid = true;

        private const string ReachableChildName = "ReachablePath";
        private const string UnreachableChildName = "UnreachablePath";

        private void Awake()
        {
            ResolveLineRenderers();
            SetupLineRenderer(reachableLineRenderer, reachableColor);
            SetupLineRenderer(unreachableLineRenderer, unreachableColor);
            Hide();
        }

        public void RenderPath(List<Vector2Int> path, TMGridManager gridManager, int reachableSegments)
        {
            if (!drawWhenPathValid) { Hide(); return; }
            if (path == null || path.Count < 2 || gridManager == null) { Hide(); return; }

            float y = gridManager.GetLandSurfaceY() + 0.02f;
            var points = new List<Vector3>(path.Count);
            for (int i = 0; i < path.Count; i++)
            {
                Vector3 p = gridManager.GridToWorldCenter(path[i]);
                p.y = y;
                points.Add(p);
            }
            RenderSplitPath(points, reachableSegments);
        }

        public void RenderPathFromWorld(Vector3 startWorldPosition, List<Vector2Int> remainingPath, TMGridManager gridManager, int reachableSegments)
        {
            if (!drawWhenPathValid) { Hide(); return; }
            if (remainingPath == null || remainingPath.Count == 0 || gridManager == null) { Hide(); return; }

            float y = gridManager.GetLandSurfaceY() + 0.02f;
            var points = new List<Vector3>(remainingPath.Count + 1);
            startWorldPosition.y = y;
            points.Add(startWorldPosition);
            for (int i = 0; i < remainingPath.Count; i++)
            {
                Vector3 p = gridManager.GridToWorldCenter(remainingPath[i]);
                p.y = y;
                points.Add(p);
            }
            RenderSplitPath(points, reachableSegments);
        }

        public void Hide()
        {
            if (reachableLineRenderer != null) reachableLineRenderer.enabled = false;
            if (unreachableLineRenderer != null) unreachableLineRenderer.enabled = false;
        }

        private void ResolveLineRenderers()
        {
            if (reachableLineRenderer == null) reachableLineRenderer = FindChildLineRenderer(ReachableChildName);
            if (unreachableLineRenderer == null) unreachableLineRenderer = FindChildLineRenderer(UnreachableChildName);
        }

        private LineRenderer FindChildLineRenderer(string childName)
        {
            var child = transform.Find(childName);
            return child != null ? child.GetComponent<LineRenderer>() : null;
        }

        private void RenderSplitPath(List<Vector3> points, int reachableSegments)
        {
            if (points == null || points.Count < 2) { Hide(); return; }

            int totalSegments = points.Count - 1;
            int clamped = Mathf.Clamp(reachableSegments, 0, totalSegments);
            int unreachable = totalSegments - clamped;

            if (clamped > 0)
                SetLinePoints(reachableLineRenderer, points.GetRange(0, clamped + 1));
            else if (reachableLineRenderer != null)
                reachableLineRenderer.enabled = false;

            if (unreachable > 0)
                SetLinePoints(unreachableLineRenderer, points.GetRange(clamped, unreachable + 1));
            else if (unreachableLineRenderer != null)
                unreachableLineRenderer.enabled = false;
        }

        private void SetLinePoints(LineRenderer lr, List<Vector3> points)
        {
            if (lr == null) return;
            lr.positionCount = points.Count;
            lr.SetPositions(points.ToArray());
            lr.enabled = true;
        }

        private void SetupLineRenderer(LineRenderer lr, Color color)
        {
            if (lr == null) return;
            lr.useWorldSpace = true;
            lr.loop = false;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.startColor = color;
            lr.endColor = color;

            if (lr.material == null)
            {
                Shader shader = Shader.Find("Unlit/Color");
                if (shader != null) lr.material = new Material(shader);
            }
            if (lr.material != null) lr.material.color = color;
        }
    }
}
