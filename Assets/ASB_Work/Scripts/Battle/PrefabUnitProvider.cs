using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class PrefabUnitProvider : MonoBehaviour, IBattleUnitProvider
{
    [SerializeField] private List<UnitPresenter> unitPresenters = new List<UnitPresenter>();
    [SerializeField] private bool includeInactivePresenters = true;

    public List<UnitSpawnData> GetUnits()
    {
        if (unitPresenters == null || unitPresenters.Count == 0)
        {
            var inactiveMode = includeInactivePresenters ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
            unitPresenters = FindObjectsByType<UnitPresenter>(inactiveMode, FindObjectsSortMode.None).ToList();
        }

        return unitPresenters
            .Where(p => p != null && p.UnitData != null)
            .Select(p => new UnitSpawnData
            {
                Data = p.UnitData,
                Team = p.TeamType,
                ExistingRoot = p.gameObject,
                GridNumber = p.GridNumber
            })
            .ToList();
    }
}
