using System;
using System.Collections.Generic;
using UnityEngine;

namespace Orora.TestMerge
{
    public class TMPartySelectionController
    {
        private readonly TMPartyRegistry partyRegistry;
        private readonly TMQuarterViewCameraFollower cameraFollower;
        private readonly float rayDistance;

        private TMPartyGridMover activeMover;

        public event Action<TMPartyGridMover> ActiveMoverChanged;
        public event Action<List<Vector2Int>> ActiveMoverPathUpdated;
        public event Action ActiveMoverMoveCompleted;

        public TMPartyGridMover ActiveMover => activeMover;

        public TMPartySelectionController(TMPartyRegistry partyRegistry, TMQuarterViewCameraFollower cameraFollower, float rayDistance)
        {
            this.partyRegistry = partyRegistry;
            this.cameraFollower = cameraFollower;
            this.rayDistance = rayDistance;
        }

        public void Initialize()
        {
            var movers = GetMovers();
            if (movers.Length == 0) return;
            SetActiveMoverInternal(movers[0], false);
        }

        public void Dispose()
        {
            UnsubscribeFromActiveMover();
        }

        public bool TryHandleSelectionClick(Ray ray)
        {
            var hits = Physics.RaycastAll(ray, rayDistance);
            float bestDist = float.PositiveInfinity;
            TMPartyGridMover clicked = null;

            for (int i = 0; i < hits.Length; i++)
            {
                var t = hits[i].transform;
                if (t == null) continue;

                var mover = t.GetComponentInParent<TMPartyGridMover>();
                if (mover == null || !IsRegistered(mover)) continue;

                if (hits[i].distance < bestDist)
                {
                    bestDist = hits[i].distance;
                    clicked = mover;
                }
            }

            if (clicked == null) return false;
            SetActiveMoverInternal(clicked, true);
            return true;
        }

        public void FocusActiveMover()
        {
            if (cameraFollower == null || activeMover == null) return;
            cameraFollower.SetFollowTarget(activeMover.transform);
            cameraFollower.RecenterOnFollowTarget();
            cameraFollower.SetFollowEnabled(true);
        }

        private bool IsRegistered(TMPartyGridMover mover)
        {
            var movers = GetMovers();
            for (int i = 0; i < movers.Length; i++)
                if (movers[i] == mover) return true;
            return false;
        }

        private TMPartyGridMover[] GetMovers()
        {
            return partyRegistry != null ? partyRegistry.PartyMovers : Array.Empty<TMPartyGridMover>();
        }

        private void SetActiveMoverInternal(TMPartyGridMover mover, bool notifyChange)
        {
            if (mover == null || mover == activeMover) return;
            UnsubscribeFromActiveMover();

            activeMover = mover;
            activeMover.PathUpdated += HandlePathUpdated;
            activeMover.MoveCompleted += HandleMoveCompleted;

            if (cameraFollower != null)
            {
                cameraFollower.SetFollowTarget(activeMover.transform);
                cameraFollower.RecenterOnFollowTarget();
            }

            if (notifyChange) ActiveMoverChanged?.Invoke(activeMover);
        }

        private void UnsubscribeFromActiveMover()
        {
            if (activeMover == null) return;
            activeMover.PathUpdated -= HandlePathUpdated;
            activeMover.MoveCompleted -= HandleMoveCompleted;
        }

        private void HandlePathUpdated(List<Vector2Int> remaining) => ActiveMoverPathUpdated?.Invoke(remaining);
        private void HandleMoveCompleted() => ActiveMoverMoveCompleted?.Invoke();
    }
}
