using System.Collections.Generic;
using UnityEngine;

public class ClickSelectionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private AStarPathfinder pathfinder;
    [SerializeField] private PlayerGridMover playerMover;
    [SerializeField] private Transform marker;

    [Header("Raycast")]
    [SerializeField] private LayerMask landLayerMask = ~0;
    [SerializeField] private float rayDistance = 500f;

    [Header("Camera (Quarter Follow on Move)")]
    [SerializeField] private QuarterViewCameraFollower cameraFollower;

    [Header("Path Preview")]
    [SerializeField] private PathPreviewRenderer pathPreviewRenderer;

    private Vector2Int markerGrid;
    private bool hasMarkerGrid;
    private bool hasPendingRepath;
    private List<Vector2Int> previewPathOverride;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (cameraFollower == null && mainCamera != null)
            cameraFollower = mainCamera.GetComponent<QuarterViewCameraFollower>();

        if (marker != null)
            marker.gameObject.SetActive(false);

        if (pathPreviewRenderer != null)
            pathPreviewRenderer.Hide();

        if (playerMover != null)
        {
            playerMover.PathUpdated += HandlePathUpdated;
            playerMover.StepReached += HandleStepReached;
            playerMover.MoveCompleted += HandleMoveCompleted;
        }
    }

    private void OnDestroy()
    {
        if (playerMover != null)
        {
            playerMover.PathUpdated -= HandlePathUpdated;
            playerMover.StepReached -= HandleStepReached;
            playerMover.MoveCompleted -= HandleMoveCompleted;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TryHandleClick();

        if (playerMover != null && playerMover.IsMoving)
            UpdateRealtimePathPreview();
    }

    private void TryHandleClick()
    {
        if (mainCamera == null || gridManager == null || playerMover == null || pathfinder == null || marker == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!TryGetClosestLandHit(ray, out RaycastHit landHit))
            return;

        Vector2Int clickedGrid = gridManager.WorldToGrid(landHit.point);
        Vector3 markerWorld = gridManager.GridToWorldCenter(clickedGrid);
        markerWorld.y = gridManager.GetLandSurfaceY();

        PlaceMarker(clickedGrid, markerWorld);

        if (playerMover.IsMoving)
        {
            hasPendingRepath = true;
            UpdatePendingPathPreview();
            return;
        }

        StartMoveToMarker();
    }

    private bool TryGetClosestLandHit(Ray ray, out RaycastHit landHit)
    {
        landHit = default;
        if (gridManager.LandTransform == null)
            return false;

        float bestDist = float.PositiveInfinity;
        bool found = false;

        var hits = Physics.RaycastAll(ray, rayDistance, landLayerMask);
        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            var t = h.transform;
            if (marker != null && (t == marker || t.IsChildOf(marker)))
                continue;

            if (t == gridManager.LandTransform || (t != null && t.IsChildOf(gridManager.LandTransform)))
            {
                if (h.distance < bestDist)
                {
                    bestDist = h.distance;
                    landHit = h;
                    found = true;
                }
            }
        }

        return found;
    }

    private void PlaceMarker(Vector2Int grid, Vector3 worldPosition)
    {
        markerGrid = grid;
        hasMarkerGrid = true;

        marker.position = worldPosition;
        if (!marker.gameObject.activeSelf)
            marker.gameObject.SetActive(true);
    }

    private void StartMoveToMarker()
    {
        if (!hasMarkerGrid || playerMover == null || pathfinder == null)
            return;

        hasPendingRepath = false;
        previewPathOverride = null;

        Vector2Int playerGrid = playerMover.GetCurrentGrid();
        List<Vector2Int> path = pathfinder.FindPath(playerGrid, markerGrid);

        Debug.Log($"[ClickToMove] playerGrid={playerGrid} goalGrid={markerGrid} pathLen={(path == null ? 0 : path.Count)}");

        playerMover.MoveByGridPath(path);

        if (pathPreviewRenderer != null)
            pathPreviewRenderer.RenderPath(path, gridManager);

        if (cameraFollower != null)
        {
            cameraFollower.SetFollowTarget(playerMover.transform);
            cameraFollower.SetFollowEnabled(true);
        }
    }

    private void HandlePathUpdated(List<Vector2Int> remainingPath)
    {
        if (pathPreviewRenderer != null)
            pathPreviewRenderer.RenderPath(remainingPath, gridManager);
    }

    private void HandleStepReached(Vector2Int reachedGrid)
    {
        if (!hasPendingRepath || !hasMarkerGrid || playerMover == null || pathfinder == null)
            return;

        StartMoveToMarker();
    }

    private void HandleMoveCompleted()
    {
        hasPendingRepath = false;
        previewPathOverride = null;
        hasMarkerGrid = false;

        if (marker != null)
            marker.gameObject.SetActive(false);

        if (pathPreviewRenderer != null)
            pathPreviewRenderer.Hide();
    }

    private void UpdateRealtimePathPreview()
    {
        if (pathPreviewRenderer == null || playerMover == null || gridManager == null)
            return;

        List<Vector2Int> pathToRender = hasPendingRepath && previewPathOverride != null
            ? previewPathOverride
            : playerMover.GetRemainingPath();

        pathPreviewRenderer.RenderPathFromWorld(playerMover.transform.position, pathToRender, gridManager);
    }

    private void UpdatePendingPathPreview()
    {
        previewPathOverride = null;

        if (!hasPendingRepath || !hasMarkerGrid || playerMover == null || pathfinder == null || pathPreviewRenderer == null || gridManager == null)
            return;

        if (!playerMover.TryGetNextGrid(out Vector2Int nextGrid))
            return;

        List<Vector2Int> pendingPath = pathfinder.FindPath(nextGrid, markerGrid);
        if (pendingPath == null || pendingPath.Count == 0)
            return;

        previewPathOverride = pendingPath;
        pathPreviewRenderer.RenderPathFromWorld(playerMover.transform.position, previewPathOverride, gridManager);
    }
}
