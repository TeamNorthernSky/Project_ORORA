using System.Collections.Generic;
using UnityEngine;

public class MineRegistry : MonoBehaviour
{
    private readonly List<Mine> mines = new List<Mine>();

    public IReadOnlyList<Mine> Mines => mines;

    private void Awake()
    {
        RegisterExistingMines();
    }

    public void Register(Mine mine)
    {
        if (mine == null || mines.Contains(mine))
            return;

        mines.Add(mine);
    }

    public void Unregister(Mine mine)
    {
        if (mine == null)
            return;

        mines.Remove(mine);
    }

    [ContextMenu("Rebuild Registry")]
    public void RegisterExistingMines()
    {
        mines.Clear();

        Mine[] sceneMines = FindObjectsByType<Mine>(FindObjectsSortMode.None);
        for (int i = 0; i < sceneMines.Length; i++)
        {
            Mine mine = sceneMines[i];
            if (mine == null)
                continue;

            Register(mine);
        }
    }
}
