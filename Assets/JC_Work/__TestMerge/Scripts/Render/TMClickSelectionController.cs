using UnityEngine;
using UnityEngine.EventSystems;

namespace Orora.TestMerge
{
    public class TMClickSelectionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private TMGridManager gridManager;
        [SerializeField] private TMAStarPathfinder pathfinder;
        [SerializeField] private TMPartyRegistry partyRegistry;
        [SerializeField] private Transform marker;

        [Header("Raycast")]
        [SerializeField] private LayerMask landLayerMask = ~0;
        [SerializeField] private float rayDistance = 500f;

        [Header("Camera")]
        [SerializeField] private TMQuarterViewCameraFollower cameraFollower;

        [Header("Path Preview")]
        [SerializeField] private TMPathPreviewRenderer pathPreviewRenderer;

        [Header("UI Input")]
        [SerializeField] private TMUIInputBlocker uiInputBlocker;

        [Header("Marker Visual")]
        [SerializeField] private Color validMarkerColor = Color.green;
        [SerializeField] private Color invalidMarkerColor = Color.red;

        private TMPartySelectionController partySelectionController;
        private TMMoveCommandPreviewController moveCommandPreviewController;

        private void Start()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (cameraFollower == null && mainCamera != null)
                cameraFollower = mainCamera.GetComponent<TMQuarterViewCameraFollower>();

            partySelectionController = new TMPartySelectionController(partyRegistry, cameraFollower, rayDistance);
            moveCommandPreviewController = new TMMoveCommandPreviewController(
                gridManager, pathfinder, marker, pathPreviewRenderer,
                validMarkerColor, invalidMarkerColor, rayDistance);

            partySelectionController.ActiveMoverChanged += HandleActiveMoverChanged;
            partySelectionController.ActiveMoverPathUpdated += HandleActiveMoverPathUpdated;
            partySelectionController.ActiveMoverMoveCompleted += HandleActiveMoverMoveCompleted;

            partySelectionController.Initialize();
            moveCommandPreviewController.Initialize();
        }

        private void OnDestroy()
        {
            if (partySelectionController != null)
            {
                partySelectionController.ActiveMoverChanged -= HandleActiveMoverChanged;
                partySelectionController.ActiveMoverPathUpdated -= HandleActiveMoverPathUpdated;
                partySelectionController.ActiveMoverMoveCompleted -= HandleActiveMoverMoveCompleted;
                partySelectionController.Dispose();
            }
        }

        private void Update()
        {
            RefreshUILockState();

            if (Input.GetMouseButtonDown(0))
                TryHandleClick();

            var active = partySelectionController != null ? partySelectionController.ActiveMover : null;
            if (active != null && active.IsMoving)
                moveCommandPreviewController?.UpdateRealtimePathPreview(active);
        }

        private void TryHandleClick()
        {
            var active = partySelectionController != null ? partySelectionController.ActiveMover : null;
            if (mainCamera == null || gridManager == null || active == null || pathfinder == null || marker == null)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (active.IsMoving || active.IsInputLocked) return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (partySelectionController.TryHandleSelectionClick(ray))
                return;

            if (moveCommandPreviewController.TryConfirmMove(ray, active))
            {
                partySelectionController.FocusActiveMover();
                return;
            }

            if (!TryGetClosestLandHit(ray, out var landHit)) return;

            Vector2Int clickedGrid = gridManager.WorldToGrid(landHit.point);

            if (!gridManager.IsInBounds(clickedGrid)) return;
            if (gridManager.HasObstacle(clickedGrid)) return;
            if (gridManager.HasOccupant(clickedGrid, active.OccupantId)) return;
            if (!gridManager.IsVisibleCell(clickedGrid)) return;

            moveCommandPreviewController.PreviewMoveToGrid(active, clickedGrid);
        }

        private bool TryGetClosestLandHit(Ray ray, out RaycastHit landHit)
        {
            landHit = default;
            if (gridManager.LandTransform == null) return false;

            float bestDist = float.PositiveInfinity;
            bool found = false;
            var hits = Physics.RaycastAll(ray, rayDistance, landLayerMask);
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                var t = h.transform;
                if (marker != null && (t == marker || t.IsChildOf(marker))) continue;
                if (t == gridManager.LandTransform || (t != null && t.IsChildOf(gridManager.LandTransform)))
                {
                    if (h.distance < bestDist) { bestDist = h.distance; landHit = h; found = true; }
                }
            }
            return found;
        }

        private void HandleActiveMoverChanged(TMPartyGridMover _)
        {
            moveCommandPreviewController?.ClearPreview();
        }

        private void HandleActiveMoverPathUpdated(System.Collections.Generic.List<Vector2Int> remainingPath)
        {
            moveCommandPreviewController?.HandlePathUpdated(remainingPath);
        }

        private void HandleActiveMoverMoveCompleted()
        {
            moveCommandPreviewController?.HandleMoveCompleted();
        }

        private void RefreshUILockState()
        {
            if (uiInputBlocker == null) return;
            var active = partySelectionController != null ? partySelectionController.ActiveMover : null;
            bool shouldLock = active != null && (active.IsMoving || active.IsInputLocked);
            uiInputBlocker.SetLocked(shouldLock);
        }
    }
}
