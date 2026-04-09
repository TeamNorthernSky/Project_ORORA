using System;
using System.Collections;
using UnityEngine;

public class PartyInteractionController
{
    private readonly GridManager gridManager;
    private readonly ResourceManager resourceManager;
    private readonly float itemPickupDelay;
    private readonly MonoBehaviour coroutineOwner;
    private readonly Func<Vector2Int> currentGridProvider;

    private Coroutine pendingItemPickupCoroutine;

    public bool IsInputLocked { get; private set; }

    public event Action<Vector2Int> AdjacentItemCellEntered;
    public event Action<Vector2Int> AdjacentMineCellEntered;

    public PartyInteractionController(
        GridManager gridManager,
        ResourceManager resourceManager,
        float itemPickupDelay,
        MonoBehaviour coroutineOwner,
        Func<Vector2Int> currentGridProvider)
    {
        this.gridManager = gridManager;
        this.resourceManager = resourceManager;
        this.itemPickupDelay = itemPickupDelay;
        this.coroutineOwner = coroutineOwner;
        this.currentGridProvider = currentGridProvider;
    }

    public void HandleGridEntered(Vector2Int enteredGrid)
    {
        if (gridManager == null)
            return;

        HandleAdjacentItemProximity(enteredGrid);
        HandleAdjacentMineProximity(enteredGrid);
    }

    public void Dispose()
    {
        CancelPendingItemPickup();
        IsInputLocked = false;
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

        AdjacentMineCellEntered?.Invoke(mineGrid);
    }

    private void OnAdjacentItemCellEntered(Vector2Int itemGrid)
    {
        CancelPendingItemPickup();

        IsInputLocked = true;
        pendingItemPickupCoroutine = coroutineOwner.StartCoroutine(InvokeDelayedItemPickup(itemGrid));
        AdjacentItemCellEntered?.Invoke(itemGrid);
    }

    private IEnumerator InvokeDelayedItemPickup(Vector2Int itemGrid)
    {
        yield return new WaitForSeconds(itemPickupDelay);

        pendingItemPickupCoroutine = null;

        if (gridManager == null)
        {
            IsInputLocked = false;
            yield break;
        }

        Vector2Int currentGrid = currentGridProvider != null ? currentGridProvider() : itemGrid;
        if (!IsAdjacentOrSame(currentGrid, itemGrid))
        {
            IsInputLocked = false;
            yield break;
        }

        if (!gridManager.TryGetItemObjectAtGrid(itemGrid, out ItemObject itemObject))
        {
            IsInputLocked = false;
            yield break;
        }

        itemObject.GetItem(resourceManager);
        IsInputLocked = false;
    }

    private void CancelPendingItemPickup()
    {
        if (pendingItemPickupCoroutine == null)
            return;

        coroutineOwner.StopCoroutine(pendingItemPickupCoroutine);
        pendingItemPickupCoroutine = null;
    }

    private static bool IsAdjacentOrSame(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return dx <= 1 && dy <= 1;
    }
}
