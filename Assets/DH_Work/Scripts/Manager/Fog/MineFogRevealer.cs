using System.Collections.Generic;
using UnityEngine;

public class MineFogRevealer : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private FogGridManager fogGridManager;
    [SerializeField] private MineRegistry mineRegistry;
    [SerializeField, Min(0)] private int revealRadius = 1;
    [SerializeField] private bool revealClaimedMinesOnEnable = true;

    private void OnEnable()
    {
        Mine.MineClaimed += HandleMineClaimed;

        if (revealClaimedMinesOnEnable)
            RevealAllClaimedMines();
    }

    private void OnDisable()
    {
        Mine.MineClaimed -= HandleMineClaimed;
    }

    [ContextMenu("Reveal Claimed Mines")]
    public void RevealAllClaimedMines()
    {
        if (gridManager == null || fogGridManager == null || mineRegistry == null)
            return;

        IReadOnlyList<Mine> mines = mineRegistry.Mines;
        for (int i = 0; i < mines.Count; i++)
        {
            Mine mine = mines[i];
            if (mine == null || mine.mineState != MineState.Claimed)
                continue;

            RevealMineArea(mine);
        }
    }

    private void HandleMineClaimed(Mine mine)
    {
        if (mine == null)
            return;

        RevealMineArea(mine);
    }

    private void RevealMineArea(Mine mine)
    {
        if (gridManager == null || fogGridManager == null)
            return;

        Vector2Int mineGrid = gridManager.WorldToGrid(mine.transform.position);
        fogGridManager.RevealArea(mineGrid, revealRadius);
    }

    private void OnValidate()
    {
        if (mineRegistry == null)
            mineRegistry = FindFirstObjectByType<MineRegistry>();
    }
}
