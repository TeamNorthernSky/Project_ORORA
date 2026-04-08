using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGridMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;

    [Header("Move Settings")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float arriveThreshold = 0.01f;

    private readonly Queue<Vector2Int> pathQueue = new Queue<Vector2Int>();
    private bool isMoving;
    private Vector2Int currentGrid;
    private float fixedY;

    public event Action<List<Vector2Int>> PathUpdated;
    public event Action<Vector2Int> AdjacentItemCellEntered;
    public event Action<Vector2Int> AdjacentFactoryCellEntered;
    public event Action MoveCompleted;

    private void Awake()
    {
        fixedY = transform.position.y;
        currentGrid = gridManager != null ? gridManager.WorldToGrid(transform.position) : Vector2Int.zero;
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
            bool reachedPathEnd = pathQueue.Count == 0;

            HandleAdjacentInteractableProximity(currentGrid);
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

    private void HandleAdjacentInteractableProximity(Vector2Int enteredGrid)
    {
        if (gridManager == null)
            return;

        HandleAdjacentItemProximity(enteredGrid);
        HandleAdjacentFactoryProximity(enteredGrid);
    }

    private void HandleAdjacentItemProximity(Vector2Int enteredGrid)
    {
        if (!gridManager.TryGetAdjacentItemGrid(enteredGrid, out Vector2Int itemGrid))
            return;

        OnAdjacentItemCellEntered(itemGrid);
    }

    private void HandleAdjacentFactoryProximity(Vector2Int enteredGrid)
    {
        if (!gridManager.TryGetAdjacentFactoryGrid(enteredGrid, out Vector2Int factoryGrid))
            return;

        OnAdjacentFactoryCellEntered(factoryGrid);
    }

    private void OnAdjacentItemCellEntered(Vector2Int itemGrid)
    {
        // TODO: Enter your item-adjacent command here when needed.
        AdjacentItemCellEntered?.Invoke(itemGrid);
    }

    private void OnAdjacentFactoryCellEntered(Vector2Int factoryGrid)
    {
        // TODO: Enter your factory-adjacent command here when needed.
        AdjacentFactoryCellEntered?.Invoke(factoryGrid);
    }
}

