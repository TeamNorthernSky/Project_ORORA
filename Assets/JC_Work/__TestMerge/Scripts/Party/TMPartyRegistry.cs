using System;
using UnityEngine;

namespace Orora.TestMerge
{
    public class TMPartyRegistry : MonoBehaviour
    {
        [SerializeField] private TMPartyGridMover[] partyMovers;

        public TMPartyGridMover[] PartyMovers => partyMovers ?? Array.Empty<TMPartyGridMover>();

        public bool TryGetPartyById(string partyId, out TMPartyGridMover partyMover)
        {
            partyMover = null;
            if (string.IsNullOrWhiteSpace(partyId)) return false;

            var movers = PartyMovers;
            for (int i = 0; i < movers.Length; i++)
            {
                if (movers[i] == null) continue;
                if (!string.Equals(movers[i].PartyId, partyId, StringComparison.Ordinal)) continue;
                partyMover = movers[i];
                return true;
            }
            return false;
        }
    }
}
