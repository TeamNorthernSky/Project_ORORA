using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class TurnManager : MonoBehaviour
{
    public event System.Action<int> DayAdvanced;
    public event System.Action<bool> EnemyTurnStateChanged;

    [SerializeField] private int day = 1;
    [SerializeField] private PartyRegistry partyRegistry;
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private EnemyTurnController enemyTurnController;
    [SerializeField] private TMP_Text turnStateText;
    [SerializeField] private MineRegistry mineRegistry;

    private bool enemyTurnRunning;

    public bool IsEnemyTurnRunning => enemyTurnRunning;
    public bool IsPlayerTurn => !enemyTurnRunning;

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

        enemyTurnRunning = true;
        EnemyTurnStateChanged?.Invoke(true);
        UpdateTurnStateText("Enemy Turn");

        if (enemyTurnController == null)
        {
            enemyTurnRunning = false;
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
        if (mineRegistry == null)
            return;

        IReadOnlyList<Mine> mines = mineRegistry.Mines;

        for (int i = 0; i < mines.Count; i++)
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
        yield return enemyTurnController.ExecuteEnemyTurn();
        enemyTurnRunning = false;
        EnemyTurnStateChanged?.Invoke(false);
        EndEnemyTurn();
    }

    private void UpdateTurnStateText(string nextText)
    {
        if (turnStateText == null)
            return;

        turnStateText.text = nextText;
    }

    private void OnValidate()
    {
        if (mineRegistry == null)
            mineRegistry = FindFirstObjectByType<MineRegistry>();
    }
}
