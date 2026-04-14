using UnityEngine;

[CreateAssetMenu(
    fileName = "MinePlacementPreset",
    menuName = "DH Work/Level Editor/Mine Placement Preset")]
public class MinePlacementPreset : ScriptableObject
{
    [SerializeField] private ResourceType resourceType = ResourceType.Ore;
    [SerializeField] private int resourcePerTurn = 5;
    [SerializeField] private MineState initialState = MineState.Unclaimed;

    public ResourceType ResourceType => resourceType;
    public int ResourcePerTurn => resourcePerTurn;
    public MineState InitialState => initialState;
}
