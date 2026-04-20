using UnityEngine;
using System.Collections;
using TMPro;

public class TurnManager : MonoBehaviour
{
    public event System.Action<int> DayAdvanced;

    [SerializeField] private int day = 1;
    [SerializeField] private PartyRegistry partyRegistry;
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private EnemyTurnController enemyTurnController;
    [SerializeField] private TMP_Text turnStateText;

    private bool enemyTurnRunning;

    private void Awake()
    {
        if (enemyTurnController == null)
            enemyTurnController = FindFirstObjectByType<EnemyTurnController>();

        UpdateTurnStateText("Player Turn");
    }

    public void EndPlayerTurn()
    {
        StartEnemyTurn();
    }

    private void StartEnemyTurn()
    {
        if (enemyTurnRunning)
            return;

        UpdateTurnStateText("Enemy Turn");

        if (enemyTurnController == null)
        {
            EndEnemyTurn();
            return;
        }

        StartCoroutine(RunEnemyTurn());
    }

    private void EndEnemyTurn()
    {
        AdvanceDay();
        ProduceClaimedMines();
        StartPlayerTurn();
    }

    private void StartPlayerTurn()
    {
        UpdateTurnStateText("Player Turn");

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
        DayAdvanced?.Invoke(day);
    }

    private void ProduceClaimedMines()
    {
        Mine[] mines = FindObjectsByType<Mine>(FindObjectsSortMode.None);
        for (int i = 0; i < mines.Length; i++)
        {
            Mine mine = mines[i];
            if (mine == null)
                continue;

            mine.ProduceForTurn(resourceManager);
        }
    }

    public int GetDay()
    {
        return day;
    }

    private IEnumerator RunEnemyTurn()
    {
        enemyTurnRunning = true;
        yield return enemyTurnController.ExecuteEnemyTurn();
        enemyTurnRunning = false;
        EndEnemyTurn();
    }

    private void UpdateTurnStateText(string nextText)
    {
        if (turnStateText == null)
            return;

        turnStateText.text = nextText;
    }
}
