using System;
using System.Collections;
using UnityEngine;

namespace Orora.TestMerge
{
    /// <summary>
    /// 셀 진입 이벤트 수신 후 인접 아이템·광산에 대한 지연 상호작용 수행.
    /// DH PartyInteractionController의 TM 버전.
    /// </summary>
    public class TMPartyInteractionController
    {
        private readonly TMGridManager gridManager;
        private readonly float itemPickupDelay;
        private readonly MonoBehaviour coroutineOwner;
        private readonly Func<Vector2Int> currentGridProvider;

        private Coroutine pendingInteractionCoroutine;

        public bool IsInputLocked { get; private set; }

        public event Action<Vector2Int> AdjacentItemCellEntered;

        public TMPartyInteractionController(
            TMGridManager gridManager,
            float itemPickupDelay,
            MonoBehaviour coroutineOwner,
            Func<Vector2Int> currentGridProvider)
        {
            this.gridManager = gridManager;
            this.itemPickupDelay = itemPickupDelay;
            this.coroutineOwner = coroutineOwner;
            this.currentGridProvider = currentGridProvider;
        }

        public void HandleGridEntered(Vector2Int enteredGrid)
        {
            if (gridManager == null) return;

            if (gridManager.TryGetAdjacentCellWith<TMItemObject>(enteredGrid, out var itemGrid, out _))
            {
                CancelPendingInteraction();
                IsInputLocked = true;
                pendingInteractionCoroutine = coroutineOwner.StartCoroutine(InvokeDelayedItemPickup(itemGrid));
                AdjacentItemCellEntered?.Invoke(itemGrid);
                return;
            }

            if (gridManager.TryGetAdjacentCellWith<TMMine>(enteredGrid, out var mineGrid, out var mine))
            {
                if (mine != null && mine.State == TMMineState.Unclaimed)
                {
                    CancelPendingInteraction();
                    IsInputLocked = true;
                    pendingInteractionCoroutine = coroutineOwner.StartCoroutine(InvokeDelayedMineClaim(mineGrid));
                }
            }
        }

        public void Dispose()
        {
            CancelPendingInteraction();
            IsInputLocked = false;
        }

        private IEnumerator InvokeDelayedItemPickup(Vector2Int itemGrid)
        {
            yield return new WaitForSeconds(itemPickupDelay);
            pendingInteractionCoroutine = null;

            if (gridManager == null) { IsInputLocked = false; yield break; }

            var currentGrid = currentGridProvider != null ? currentGridProvider() : itemGrid;
            if (!IsAdjacentOrSame(currentGrid, itemGrid)) { IsInputLocked = false; yield break; }

            if (!gridManager.TryGetCellObject<TMItemObject>(itemGrid, out var item)) { IsInputLocked = false; yield break; }

            gridManager.UnregisterCellObject(itemGrid);
            gridManager.SetEvent(itemGrid, -1);
            item.GetItem();
            IsInputLocked = false;
        }

        private IEnumerator InvokeDelayedMineClaim(Vector2Int mineGrid)
        {
            yield return new WaitForSeconds(itemPickupDelay);
            pendingInteractionCoroutine = null;

            if (gridManager == null) { IsInputLocked = false; yield break; }

            var currentGrid = currentGridProvider != null ? currentGridProvider() : mineGrid;
            if (!IsAdjacentOrSame(currentGrid, mineGrid)) { IsInputLocked = false; yield break; }

            if (!gridManager.TryGetCellObject<TMMine>(mineGrid, out var mine)) { IsInputLocked = false; yield break; }

            if (mine.State == TMMineState.Unclaimed)
                mine.MineClaim();

            IsInputLocked = false;
        }

        private void CancelPendingInteraction()
        {
            if (pendingInteractionCoroutine == null) return;
            coroutineOwner.StopCoroutine(pendingInteractionCoroutine);
            pendingInteractionCoroutine = null;
        }

        private static bool IsAdjacentOrSame(Vector2Int a, Vector2Int b)
        {
            int dx = Mathf.Abs(a.x - b.x);
            int dy = Mathf.Abs(a.y - b.y);
            return dx <= 1 && dy <= 1;
        }
    }
}
