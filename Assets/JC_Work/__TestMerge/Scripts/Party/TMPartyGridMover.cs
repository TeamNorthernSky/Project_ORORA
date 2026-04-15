using System;
using System.Collections.Generic;
using UnityEngine;

namespace Orora.TestMerge
{
    /// <summary>
    /// 파티 이동자. 셀 단위 이산 이동 + 셀 경계 통과 이벤트.
    /// DH PartyGridMover의 TM 버전. 물리 쿼리 대신 TMGridManager(flat array)에 질의.
    /// </summary>
    public class TMPartyGridMover : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string partyId = "party_001";
        [SerializeField] private int occupantId = -1;

        [Header("References")]
        [SerializeField] private TMGridManager gridManager;

        [Header("Move Settings")]
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float arriveThreshold = 0.01f;
        [SerializeField] private float itemPickupDelay = 0.5f;
        [SerializeField] private int maxMovePoints = 10;

        private readonly Queue<Vector2Int> pathQueue = new Queue<Vector2Int>();
        private bool isMoving;
        private Vector2Int currentGrid;
        private float fixedY;
        private TMPartyMovePointController movePointController;
        private TMPartyInteractionController interactionController;

        public event Action<List<Vector2Int>> PathUpdated;
        public event Action<Vector2Int> AdjacentItemCellEntered;
        public event Action<Vector2Int> GridEntered;
        public event Action MoveCompleted;

        public string PartyId => partyId;
        public int OccupantId => occupantId;
        public bool IsMoving => isMoving;
        public bool IsInputLocked => interactionController != null && interactionController.IsInputLocked;
        public int RemainingMovePoints => movePointController != null ? movePointController.RemainingMovePoints : 0;
        public int MaxMovePoints => maxMovePoints;

        private void Awake()
        {
            fixedY = transform.position.y;
            currentGrid = gridManager != null ? gridManager.WorldToGrid(transform.position) : Vector2Int.zero;
            movePointController = new TMPartyMovePointController(maxMovePoints);
            interactionController = new TMPartyInteractionController(
                gridManager, itemPickupDelay, this, GetCurrentGrid);
            interactionController.AdjacentItemCellEntered += HandleAdjacentItemCellEntered;
        }

        private void OnDestroy()
        {
            if (interactionController != null)
            {
                interactionController.AdjacentItemCellEntered -= HandleAdjacentItemCellEntered;
                interactionController.Dispose();
            }
        }

        private void Update()
        {
            if (!isMoving || pathQueue.Count == 0 || gridManager == null) return;

            Vector2Int nextGrid = pathQueue.Peek();
            Vector3 target = gridManager.GridToWorldCenter(nextGrid);
            target.y = fixedY;

            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            if ((transform.position - target).sqrMagnitude <= arriveThreshold * arriveThreshold)
            {
                transform.position = target;
                pathQueue.Dequeue();

                // 점유 셀 갱신
                if (gridManager != null)
                {
                    gridManager.SetOccupant(currentGrid, -1);
                    gridManager.SetOccupant(nextGrid, occupantId);
                }

                currentGrid = nextGrid;
                movePointController?.SpendStep();
                bool reachedPathEnd = pathQueue.Count == 0;

                GridEntered?.Invoke(currentGrid);
                interactionController?.HandleGridEntered(currentGrid);
                NotifyPathUpdated();

                if (reachedPathEnd && pathQueue.Count == 0)
                {
                    isMoving = false;
                    MoveCompleted?.Invoke();
                }
            }
        }

        public Vector2Int GetCurrentGrid() => currentGrid;

        public bool CanSpendMovePoints(int amount)
            => movePointController != null && movePointController.CanSpend(amount);

        public void ResetMovePointsToMax() => movePointController?.ResetToMax();

        public void SnapToGridPosition(Vector2Int grid)
        {
            pathQueue.Clear();
            isMoving = false;

            if (gridManager != null)
                gridManager.SetOccupant(currentGrid, -1);

            currentGrid = grid;

            if (gridManager == null) return;

            gridManager.SetOccupant(currentGrid, occupantId);

            Vector3 worldPosition = gridManager.GridToWorldCenter(grid);
            worldPosition.y = fixedY;
            transform.position = worldPosition;
            GridEntered?.Invoke(currentGrid);
            NotifyPathUpdated();
        }

        public List<Vector2Int> GetRemainingPath()
        {
            var remainingPath = new List<Vector2Int>();
            remainingPath.AddRange(pathQueue);
            return remainingPath;
        }

        public void MoveByGridPath(List<Vector2Int> fullPath)
        {
            pathQueue.Clear();
            isMoving = false;

            if (fullPath != null && fullPath.Count > 0)
                currentGrid = fullPath[0];

            if (fullPath == null || fullPath.Count <= 1)
            {
                NotifyPathUpdated();
                MoveCompleted?.Invoke();
                return;
            }

            int moveCost = Mathf.Max(0, fullPath.Count - 1);
            if (!CanSpendMovePoints(moveCost))
            {
                NotifyPathUpdated();
                return;
            }

            for (int i = 1; i < fullPath.Count; i++)
                pathQueue.Enqueue(fullPath[i]);

            isMoving = pathQueue.Count > 0;
            NotifyPathUpdated();
        }

        private void NotifyPathUpdated()
        {
            var remainingPath = new List<Vector2Int> { GetCurrentGrid() };
            remainingPath.AddRange(pathQueue);
            PathUpdated?.Invoke(remainingPath);
        }

        private void HandleAdjacentItemCellEntered(Vector2Int itemGrid)
        {
            AdjacentItemCellEntered?.Invoke(itemGrid);
        }
    }
}
