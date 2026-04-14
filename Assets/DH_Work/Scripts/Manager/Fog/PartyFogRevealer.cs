using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartyFogRevealer : MonoBehaviour
{
    [SerializeField] private PartyRegistry partyRegistry;
    [SerializeField] private FogGridManager fogGridManager;
    [SerializeField, Min(0)] private int revealRadius = 3;
    [SerializeField] private bool revealCurrentPositionsOnEnable = true;

    private readonly HashSet<PartyGridMover> subscribedMovers = new HashSet<PartyGridMover>();
    private Coroutine initialRevealCoroutine;

    private void OnEnable()
    {
        SubscribeToRegisteredParties();

        if (revealCurrentPositionsOnEnable)
            QueueInitialReveal();
    }

    private void OnDisable()
    {
        if (initialRevealCoroutine != null)
        {
            StopCoroutine(initialRevealCoroutine);
            initialRevealCoroutine = null;
        }

        UnsubscribeFromRegisteredParties();
    }

    [ContextMenu("Reveal Current Party Positions")]
    public void RevealAllCurrentPartyPositions()
    {
        if (fogGridManager == null || partyRegistry == null)
            return;

        PartyGridMover[] movers = partyRegistry.PartyMovers;
        for (int i = 0; i < movers.Length; i++)
        {
            PartyGridMover mover = movers[i];
            if (mover == null)
                continue;

            fogGridManager.RevealArea(mover.GetCurrentGrid(), revealRadius);
        }
    }

    private void SubscribeToRegisteredParties()
    {
        if (partyRegistry == null)
            return;

        PartyGridMover[] movers = partyRegistry.PartyMovers;
        for (int i = 0; i < movers.Length; i++)
        {
            PartyGridMover mover = movers[i];
            if (mover == null || subscribedMovers.Contains(mover))
                continue;

            mover.GridEntered += HandlePartyGridEntered;
            subscribedMovers.Add(mover);
        }
    }

    private void UnsubscribeFromRegisteredParties()
    {
        foreach (PartyGridMover mover in subscribedMovers)
        {
            if (mover == null)
                continue;

            mover.GridEntered -= HandlePartyGridEntered;
        }

        subscribedMovers.Clear();
    }

    private void HandlePartyGridEntered(Vector2Int currentGrid)
    {
        if (fogGridManager == null)
            return;

        fogGridManager.RevealArea(currentGrid, revealRadius);
    }

    private void QueueInitialReveal()
    {
        if (!Application.isPlaying)
        {
            RevealAllCurrentPartyPositions();
            return;
        }

        if (initialRevealCoroutine != null)
            StopCoroutine(initialRevealCoroutine);

        initialRevealCoroutine = StartCoroutine(RevealCurrentPositionsNextFrame());
    }

    private IEnumerator RevealCurrentPositionsNextFrame()
    {
        yield return null;
        initialRevealCoroutine = null;
        RevealAllCurrentPartyPositions();
    }
}
