using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PathPreviewRenderer : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float width = 0.08f;
    [SerializeField] private Color reachableColor = Color.green;
    [SerializeField] private Color unreachableColor = Color.red;
    [SerializeField] private bool drawWhenPathValid = true;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;

        if (lineRenderer.material == null)
        {
            // URP에서도 보이도록 Unlit 기본 셰이더 사용
            var shader = Shader.Find("Unlit/Color");
            if (shader != null)
                lineRenderer.material = new Material(shader);
        }
        ApplySegmentColors(0, 0);
        Hide();
    }

    public void RenderPath(List<Vector2Int> path, GridManager gridManager, int reachableSegments)
    {
        if (!drawWhenPathValid)
        {
            Hide();
            return;
        }

        if (path == null || path.Count < 2 || gridManager == null)
        {
            Hide();
            return;
        }

        lineRenderer.positionCount = path.Count;
        float y = gridManager.GetLandSurfaceY();

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 p = gridManager.GridToWorldCenter(path[i]);
            p.y = y + 0.02f; // 바닥에 살짝 띄워서 Z-fighting 방지
            lineRenderer.SetPosition(i, p);
        }

        ApplySegmentColors(path.Count - 1, reachableSegments);
        lineRenderer.enabled = true;
    }

    public void RenderPathFromWorld(Vector3 startWorldPosition, List<Vector2Int> remainingPath, GridManager gridManager, int reachableSegments)
    {
        if (!drawWhenPathValid)
        {
            Hide();
            return;
        }

        if (remainingPath == null || remainingPath.Count == 0 || gridManager == null)
        {
            Hide();
            return;
        }

        float y = gridManager.GetLandSurfaceY() + 0.02f;
        lineRenderer.positionCount = remainingPath.Count + 1;

        startWorldPosition.y = y;
        lineRenderer.SetPosition(0, startWorldPosition);

        for (int i = 0; i < remainingPath.Count; i++)
        {
            Vector3 p = gridManager.GridToWorldCenter(remainingPath[i]);
            p.y = y;
            lineRenderer.SetPosition(i + 1, p);
        }

        ApplySegmentColors(remainingPath.Count, reachableSegments);
        lineRenderer.enabled = true;
    }

    public void Hide()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    private void ApplySegmentColors(int totalSegments, int reachableSegments)
    {
        if (lineRenderer == null)
            return;

        if (totalSegments <= 0)
        {
            lineRenderer.colorGradient = BuildSolidGradient(reachableColor);
            return;
        }

        int clampedReachableSegments = Mathf.Clamp(reachableSegments, 0, totalSegments);
        float split = (float)clampedReachableSegments / totalSegments;

        if (clampedReachableSegments == 0)
        {
            lineRenderer.colorGradient = BuildSolidGradient(unreachableColor);
            return;
        }

        if (clampedReachableSegments == totalSegments)
        {
            lineRenderer.colorGradient = BuildSolidGradient(reachableColor);
            return;
        }

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(reachableColor, 0f),
                new GradientColorKey(reachableColor, split),
                new GradientColorKey(unreachableColor, split),
                new GradientColorKey(unreachableColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(reachableColor.a, 0f),
                new GradientAlphaKey(reachableColor.a, split),
                new GradientAlphaKey(unreachableColor.a, split),
                new GradientAlphaKey(unreachableColor.a, 1f)
            });
        lineRenderer.colorGradient = gradient;
    }

    private Gradient BuildSolidGradient(Color color)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new[]
            {
                new GradientAlphaKey(color.a, 0f),
                new GradientAlphaKey(color.a, 1f)
            });
        return gradient;
    }
}

