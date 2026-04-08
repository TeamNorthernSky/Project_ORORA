using System.Collections.Generic;
using UnityEngine;

public class ClickSelectionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private AStarPathfinder pathfinder;
    [SerializeField] private PlayerGridMover[] playerMovers;
    [SerializeField] private Transform marker;

    [Header("Raycast")]
    [SerializeField] private LayerMask landLayerMask = ~0;
    [SerializeField] private float rayDistance = 500f;

    [Header("Camera (Quarter Follow on Move)")]
    [SerializeField] private QuarterViewCameraFollower cameraFollower;

    [Header("Path Preview")]
    [SerializeField] private PathPreviewRenderer pathPreviewRenderer;

    private PlayerGridMover activeMover;
    private Vector2Int markerGrid;
    private bool hasMarkerGrid;
    private List<Vector2Int> previewPath;
    private bool hasPreviewPath;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        activeMover = playerMovers[0];

        if (cameraFollower == null && mainCamera != null)
            cameraFollower = mainCamera.GetComponent<QuarterViewCameraFollower>();

        if (marker != null)
            marker.gameObject.SetActive(false);

        if (pathPreviewRenderer != null)
            pathPreviewRenderer.Hide();

        if (activeMover != null)
        {
            activeMover.PathUpdated += HandlePathUpdated;
            activeMover.MoveCompleted += HandleMoveCompleted;
        }
    }

    private void OnDestroy()
    {
        if (activeMover != null)
        {
            activeMover.PathUpdated -= HandlePathUpdated;
            activeMover.MoveCompleted -= HandleMoveCompleted;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TryHandleClick();

        if (activeMover != null && activeMover.IsMoving)
            UpdateRealtimePathPreview();
    }

    private void TryHandleClick()
    {
        if (mainCamera == null || gridManager == null || activeMover == null || pathfinder == null || marker == null)
            return;

        if (activeMover.IsMoving)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (TryHandleMoverSelection(ray))
            return;

        if (TryHandleMarkerClick(ray))
            return;

        if (!TryGetClosestLandHit(ray, out RaycastHit landHit))
            return;

        Vector2Int clickedGrid = gridManager.WorldToGrid(landHit.point);
        if (gridManager.HasObstacle(clickedGrid))
            return;

        Vector3 markerWorld = gridManager.GridToWorldCenter(clickedGrid);
        markerWorld.y = gridManager.GetLandSurfaceY();

        PlaceMarker(clickedGrid, markerWorld);
        PreviewMoveToMarker();
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

    private bool TryHandleMoverSelection(Ray ray)
    {
        var hits = Physics.RaycastAll(ray, rayDistance);
        float bestDist = float.PositiveInfinity;
        PlayerGridMover clickedMover = null;

        for (int i = 0; i < hits.Length; i++)
        {
            var t = hits[i].transform;
            if (t == null)
                continue;

            PlayerGridMover mover = t.GetComponentInParent<PlayerGridMover>();
            if (mover == null)
                continue;

            if (!IsRegisteredMover(mover))
                continue;

            if (hits[i].distance < bestDist)
            {
                bestDist = hits[i].distance;
                clickedMover = mover;
            }
        }
        if (clickedMover == null)
            return false;

        SetActiveMover(clickedMover);
        return true;
    }

    private bool IsRegisteredMover(PlayerGridMover mover)
    {
        foreach (var m in playerMovers)
        {
            if (m == mover)
                return true;
        }
        return false;
    }

    private void SetActiveMover(PlayerGridMover mover)
    {
        if (mover == null || mover == activeMover)
            return;

        if (activeMover != null)
        {
            activeMover.PathUpdated -= HandlePathUpdated;
            activeMover.MoveCompleted -= HandleMoveCompleted;
        }

        activeMover = mover;
        activeMover.PathUpdated += HandlePathUpdated;
        activeMover.MoveCompleted += HandleMoveCompleted;

        previewPath = null;
        hasPreviewPath = false;
        hasMarkerGrid = false;

        if (marker != null)
            marker.gameObject.SetActive(false);

        if (pathPreviewRenderer != null)
            pathPreviewRenderer.Hide();

        if (cameraFollower != null)
            cameraFollower.SetFollowTarget(activeMover.transform);
    }

    private bool TryHandleMarkerClick(Ray ray)
    {
        if (marker == null || !marker.gameObject.activeInHierarchy || !hasMarkerGrid || !hasPreviewPath)
            return false;

        var hits = Physics.RaycastAll(ray, rayDistance);
        float bestDist = float.PositiveInfinity;
        bool hitMarker = false;

        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            var t = hit.transform;
            if (t == null)
                continue;

            if (t == marker || t.IsChildOf(marker))
            {
                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    hitMarker = true;
                }
            }
        }

        if (!hitMarker)
            return false;

        ConfirmMoveToMarker();
        return true;
    }

    private void PlaceMarker(Vector2Int grid, Vector3 worldPosition)
    {
        markerGrid = grid;
        hasMarkerGrid = true;

        marker.position = worldPosition;
        if (!marker.gameObject.activeSelf)
            marker.gameObject.SetActive(true);
    }

    private void PreviewMoveToMarker()
    {
        if (!hasMarkerGrid || activeMover == null || pathfinder == null)
            return;

        Vector2Int playerGrid = activeMover.GetCurrentGrid();
        List<Vector2Int> path = pathfinder.FindPath(playerGrid, markerGrid);
        path = AdjustPathForSpecialDestination(path);
        previewPath = path;
        hasPreviewPath = path != null && path.Count > 1;

        Debug.Log($"[ClickToMove] playerGrid={playerGrid} goalGrid={markerGrid} pathLen={(path == null ? 0 : path.Count)}");

        if (pathPreviewRenderer != null)
            pathPreviewRenderer.RenderPath(path, gridManager);

        if (cameraFollower != null)
        {
            cameraFollower.SetFollowTarget(activeMover.transform);
        }
    }

    private void ConfirmMoveToMarker()
    {
        if (!hasMarkerGrid || !hasPreviewPath || activeMover == null)
            return;

        activeMover.MoveByGridPath(previewPath);

        if (cameraFollower != null)
        {
            cameraFollower.SetFollowTarget(activeMover.transform);
            cameraFollower.SetFollowEnabled(true);
        }
    }

    private void HandlePathUpdated(List<Vector2Int> remainingPath)
    {
        if (pathPreviewRenderer != null)
            pathPreviewRenderer.RenderPath(remainingPath, gridManager);
    }

    private void HandleMoveCompleted()
    {
        previewPath = null;
        hasPreviewPath = false;
        hasMarkerGrid = false;

        if (marker != null)
            marker.gameObject.SetActive(false);

        if (pathPreviewRenderer != null)
            pathPreviewRenderer.Hide();
    }

    private void UpdateRealtimePathPreview()
    {
        if (pathPreviewRenderer == null || activeMover == null || gridManager == null)
            return;

        pathPreviewRenderer.RenderPathFromWorld(activeMover.transform.position, activeMover.GetRemainingPath(), gridManager);
    }

    private List<Vector2Int> AdjustPathForSpecialDestination(List<Vector2Int> path)
    {
        if (path == null || path.Count == 0 || gridManager == null || !hasMarkerGrid)
            return path;

        if (!gridManager.HasItemOrFactory(markerGrid))
            return path;

        if (path[path.Count - 1] != markerGrid)
            return path;

        if (path.Count <= 1)
            return path;

        path.RemoveAt(path.Count - 1);
        return path;
    }
}
