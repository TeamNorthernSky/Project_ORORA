using System;
using UnityEngine;

namespace Orora.TestMerge
{
    /// <summary>
    /// 파티 한 기에 부착. TMPartyGridMover.GridEntered 구독 → 자신의 위치 변경을 레지스트리에 통지.
    /// OnEnable 시 registry에 자동 등록.
    /// </summary>
    [RequireComponent(typeof(TMPartyGridMover))]
    public class TMPartyFogRevealer : MonoBehaviour, IFogRevealer
    {
        [SerializeField] private TMFogRevealerRegistry registry;
        [SerializeField, Min(0)] private int revealRadius = 3;

        private TMPartyGridMover mover;
        private Vector2Int cachedGrid;

        public Vector2Int GridPosition => mover != null ? mover.GetCurrentGrid() : cachedGrid;
        public int Radius => revealRadius;
        public bool IsActive => isActiveAndEnabled && mover != null;

        public event Action<IFogRevealer> RevealerChanged;

        private void Awake()
        {
            mover = GetComponent<TMPartyGridMover>();
        }

        private void OnEnable()
        {
            if (mover != null)
                mover.GridEntered += HandleGridEntered;
            if (registry != null)
                registry.Register(this);
        }

        private void OnDisable()
        {
            if (mover != null)
                mover.GridEntered -= HandleGridEntered;
            if (registry != null)
                registry.Unregister(this);
        }

        private void HandleGridEntered(Vector2Int grid)
        {
            cachedGrid = grid;
            RevealerChanged?.Invoke(this);
        }
    }
}
