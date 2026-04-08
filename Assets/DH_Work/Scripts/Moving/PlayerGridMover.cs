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
    [SerializeField] private float itemPickupDelay = 0.5f;

    private readonly Queue<Vector2Int> pathQueue = new Queue<Vector2Int>();
    private bool isMoving;
    private bool isInputLocked;
    private Vector2Int currentGrid;
    private float fixedY;
    private Coroutine pendingItemPickupCoroutine;

    public event Action<List<Vector2Int>> PathUpdated;
    public event Action<Vector2Int> AdjacentItemCellEntered;
    public event Action<Vector2Int> AdjacentMineCellEntered;
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
    public bool IsInputLocked => isInputLocked;

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
        HandleAdjacentMineProximity(enteredGrid);
    }

    private void HandleAdjacentItemProximity(Vector2Int enteredGrid)
    {
        if (!gridManager.TryGetAdjacentItemGrid(enteredGrid, out Vector2Int itemGrid))
            return;

        OnAdjacentItemCellEntered(itemGrid);
    }

    private void HandleAdjacentMineProximity(Vector2Int enteredGrid)
    {
        if (!gridManager.TryGetAdjacentMineGrid(enteredGrid, out Vector2Int mineGrid))
            return;

        OnAdjacentMineCellEntered(mineGrid);
    }

    private void OnAdjacentItemCellEntered(Vector2Int itemGrid)
    {
        if (pendingItemPickupCoroutine != null)
            StopCoroutine(pendingItemPickupCoroutine);

        isInputLocked = true;
        pendingItemPickupCoroutine = StartCoroutine(InvokeDelayedItemPickup(itemGrid));
        AdjacentItemCellEntered?.Invoke(itemGrid);
    }

    private void OnAdjacentMineCellEntered(Vector2Int mineGrid)
    {
        // TODO: Enter your mine-adjacent command here when needed.
        AdjacentMineCellEntered?.Invoke(mineGrid);
    }

    private System.Collections.IEnumerator InvokeDelayedItemPickup(Vector2Int itemGrid)
    {
        yield return new WaitForSeconds(itemPickupDelay);

        pendingItemPickupCoroutine = null;

        if (gridManager == null)
        {
            isInputLocked = false;
            yield break;
        }

        if (!IsAdjacentOrSame(currentGrid, itemGrid))
        {
            isInputLocked = false;
            yield break;
        }

        if (!gridManager.TryGetItemObjectAtGrid(itemGrid, out ItemObject itemObject))
        {
            isInputLocked = false;
            yield break;
        }

        itemObject.GetItem();
        isInputLocked = false;
    }

    private static bool IsAdjacentOrSame(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return dx <= 1 && dy <= 1;
    }
}

