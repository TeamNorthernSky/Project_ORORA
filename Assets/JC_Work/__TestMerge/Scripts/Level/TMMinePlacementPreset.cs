using UnityEngine;

namespace Orora.TestMerge
{
    [CreateAssetMenu(fileName = "TMMinePlacementPreset", menuName = "Orora/TestMerge/Mine Placement Preset")]
    public class TMMinePlacementPreset : ScriptableObject
    {
        [SerializeField] private TMResourceType resourceType = TMResourceType.Ore;
        [SerializeField, Min(0)] private int resourcePerTurn = 5;
        [SerializeField] private TMMineState initialState = TMMineState.Unclaimed;

        public TMResourceType ResourceType => resourceType;
        public int ResourcePerTurn => resourcePerTurn;
        public TMMineState InitialState => initialState;
    }
}
