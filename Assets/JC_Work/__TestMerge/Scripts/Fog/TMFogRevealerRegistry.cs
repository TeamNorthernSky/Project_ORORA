using System;
using System.Collections.Generic;
using UnityEngine;

namespace Orora.TestMerge
{
    /// <summary>
    /// IFogRevealer 목록 관리. 등록·해제·순회를 제공.
    /// TMFogRenderManager와 TMTurnFogController가 구독.
    /// </summary>
    public class TMFogRevealerRegistry : MonoBehaviour
    {
        private readonly List<IFogRevealer> revealers = new List<IFogRevealer>();

        public event Action<IFogRevealer> RevealerRegistered;
        public event Action<IFogRevealer> RevealerUnregistered;
        public event Action AnyRevealerChanged;

        public IReadOnlyList<IFogRevealer> Revealers => revealers;

        public void Register(IFogRevealer revealer)
        {
            if (revealer == null || revealers.Contains(revealer)) return;
            revealers.Add(revealer);
            revealer.RevealerChanged += HandleRevealerChanged;
            RevealerRegistered?.Invoke(revealer);
            AnyRevealerChanged?.Invoke();
        }

        public void Unregister(IFogRevealer revealer)
        {
            if (revealer == null || !revealers.Remove(revealer)) return;
            revealer.RevealerChanged -= HandleRevealerChanged;
            RevealerUnregistered?.Invoke(revealer);
            AnyRevealerChanged?.Invoke();
        }

        private void HandleRevealerChanged(IFogRevealer revealer)
        {
            AnyRevealerChanged?.Invoke();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < revealers.Count; i++)
            {
                if (revealers[i] != null)
                    revealers[i].RevealerChanged -= HandleRevealerChanged;
            }
            revealers.Clear();
        }
    }
}
