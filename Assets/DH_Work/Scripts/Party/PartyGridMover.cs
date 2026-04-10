using System;
using System.Collections.Generic;
using UnityEngine;

public class PartyGridMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private ResourceManager resourceManager;

    [Header("Move Settings")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float arriveThreshold = 0.01f;
    [SerializeField] private float itemPickupDelay = 0.5f;
    [SerializeField] private int maxMovePoints = 10;

    private readonly Queue<Vector2Int> pathQueue = new Queue<Vector2Int>();
    private bool isMoving;
    private Vector2Int currentGrid;
    private float fixedY;
    private PartyMovePointController movePointController;
    private PartyInteractionController interactionController;

    public event Action<List<Vector2Int>> PathUpdated;
    public event Action<Vector2Int> AdjacentItemCellEntered;
    public event Action MoveCompleted;

    private void Awake()
    {
        fixedY = transform.position.y;
        currentGrid = gridManager != null ? gridManager.WorldToGrid(transform.position) : Vector2Int.zero;
        movePointController = new PartyMovePointController(maxMovePoints);
        interactionController = new PartyInteractionController(
            gridManager,
            resourceManager,
            itemPickupDelay,
            this,
            GetCurrentGrid);

        interactionController.AdjacentItemCellEntered += HandleAdjacentItemCellEntered;
    }

    private void OnDestroy()
    {
        if (interactionController == null)
            return;

        interactionController.AdjacentItemCellEntered -= HandleAdjacentItemCellEntered;
        interactionController.Dispose();
    }

    private void Update()
    {
        if (!isMoving || pathQueue.Count == 0 || gridManager == null)
            return;

        Vector2Int nextGrid = pathQueue.Peek();
        Vector3 target = gridManager.GridToWorldCenter(nextGrid);
        target.y = fixedY;

        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        if ((transform.position - target).sqrMagnitude <= arriveThreshold * arriveThreshold)
        {
            transform.position = target;
            pathQueue.Dequeue();
            currentGrid = nextGrid;
            movePointController?.SpendStep();
            bool reachedPathEnd = pathQueue.Count == 0;

            interactionController?.HandleGridEntered(currentGrid);
            NotifyPathUpdated();

            if (reachedPathEnd && pathQueue.Count == 0)
            {
                isMoving = false;
                MoveCompleted?.Invoke();
            }
        }
    }

    public Vector2Int GetCurrentGrid()
    {
        return currentGrid;
    }

    public bool IsMoving => isMoving;
    public bool IsInputLocked => interactionController != null && interactionController.IsInputLocked;
    public int RemainingMovePoints => movePointController != null ? movePointController.RemainingMovePoints : 0;
    public int MaxMovePoints => maxMovePoints;

    public bool CanSpendMovePoints(int amount)
    {
        return movePointController != null && movePointController.CanSpend(amount);
    }

    public void ResetMovePointsToMax()
    {
        movePointController?.ResetToMax();
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

        // path[0]는 현재 위치이므로 제외하고 enqueue
        int moveCost = GetPathMoveCost(fullPath);
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

    private static int GetPathMoveCost(List<Vector2Int> path)
    {
        return path == null ? 0 : Mathf.Max(0, path.Count - 1);
    }
}

