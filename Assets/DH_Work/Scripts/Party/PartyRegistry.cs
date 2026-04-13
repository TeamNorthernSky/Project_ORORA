using System;
using UnityEngine;

public class PartyRegistry : MonoBehaviour
{
    [SerializeField] private PartyGridMover[] partyMovers;

    public PartyGridMover[] PartyMovers => partyMovers ?? Array.Empty<PartyGridMover>();

    public bool TryGetPartyById(string partyId, out PartyGridMover partyMover)
    {
        partyMover = null;

        if (string.IsNullOrWhiteSpace(partyId))
            return false;

        PartyGridMover[] movers = PartyMovers;
        for (int i = 0; i < movers.Length; i++)
        {
            PartyGridMover candidate = movers[i];
            if (candidate == null)
                continue;

            if (!string.Equals(candidate.PartyId, partyId, StringComparison.Ordinal))
                continue;

            partyMover = candidate;
            return true;
        }

        return false;
    }
}
