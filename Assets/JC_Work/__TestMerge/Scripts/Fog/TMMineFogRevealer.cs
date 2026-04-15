using System;
using System.Collections.Generic;
using UnityEngine;

namespace Orora.TestMerge
{
    /// <summary>
    /// 광산들을 감시하여 Claimed 상태인 광산의 위치 주변 시야를 공개.
    /// 여러 광산을 한 컴포넌트가 관리(개별 광산마다 Revealer를 두지 않음).
    /// </summary>
    public class TMMineFogRevealer : MonoBehaviour
    {
        [SerializeField] private TMFogRevealerRegistry registry;
        [SerializeField] private TMGridManager gridManager;
        [SerializeField, Min(0)] private int revealRadius = 2;

        private readonly List<MineRevealerEntry> entries = new List<MineRevealerEntry>();

        private void OnEnable()
        {
            TMMine.MineClaimed += HandleMineClaimed;
        }

        private void OnDisable()
        {
            TMMine.MineClaimed -= HandleMineClaimed;
            foreach (var e in entries)
                if (registry != null) registry.Unregister(e);
            entries.Clear();
        }

        private void HandleMineClaimed(TMMine mine)
        {
            if (mine == null || gridManager == null || registry == null) return;

            Vector2Int grid = gridManager.WorldToGrid(mine.transform.position);
            var entry = new MineRevealerEntry(grid, revealRadius);
            entries.Add(entry);
            registry.Register(entry);
        }

        private class MineRevealerEntry : IFogRevealer
        {
            private readonly Vector2Int grid;
            private readonly int radius;

            public Vector2Int GridPosition => grid;
            public int Radius => radius;
            public bool IsActive => true;
            public event Action<IFogRevealer> RevealerChanged;

            public MineRevealerEntry(Vector2Int grid, int radius)
            {
                this.grid = grid;
                this.radius = radius;
                _ = RevealerChanged; // suppress unused warning
            }
        }
    }
}
