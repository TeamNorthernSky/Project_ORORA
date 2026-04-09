using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [SerializeField] private int day = 1;
    [SerializeField] private PartyRegistry partyRegistry;

    public void EndPlayerTurn()
    {
        StartEnemyTurn();
    }

    private void StartEnemyTurn()
    {
        // AI action
        EndEnemyTurn();
    }

    private void EndEnemyTurn()
    {
        AdvanceDay();
        StartPlayerTurn();
    }

    private void StartPlayerTurn()
    {
        if (partyRegistry == null)
            return;

        PartyGridMover[] partyMovers = partyRegistry.PartyMovers;
        for (int i = 0; i < partyMovers.Length; i++)
        {
            PartyGridMover partyMover = partyMovers[i];
            if (partyMover == null)
                continue;

            partyMover.ResetMovePointsToMax();
        }
    }

    private void AdvanceDay()
    {
        day++;
    }
}
