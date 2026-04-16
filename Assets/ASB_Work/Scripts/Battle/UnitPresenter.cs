using UnityEngine;

[DisallowMultipleComponent]
public class UnitPresenter : MonoBehaviour, IUnitIdentifier
{
    [SerializeField] private ScriptableObject unitDataAsset;
    [SerializeField] private TeamType teamType = TeamType.Player;
    [SerializeField] private int gridNumber = 1;

    public IUnitData RuntimeDataOverride { get; private set; }

    public IUnitData UnitData => RuntimeDataOverride ?? unitDataAsset as IUnitData;
    public TeamType TeamType => UnitData != null ? UnitData.teamType : teamType;
    public int GridNumber => gridNumber;

    public string UnitID => UnitData != null ? UnitData.unitId : string.Empty;

    public void SetRuntimeData(IUnitData data, TeamType team, int grid)
    {
        RuntimeDataOverride = data;
        teamType = team;
        gridNumber = grid;
    }
}
