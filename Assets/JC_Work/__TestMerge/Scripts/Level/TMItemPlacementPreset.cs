using UnityEngine;

namespace Orora.TestMerge
{
    [CreateAssetMenu(fileName = "TMItemPlacementPreset", menuName = "Orora/TestMerge/Item Placement Preset")]
    public class TMItemPlacementPreset : ScriptableObject
    {
        [SerializeField] private TMResourceType resourceType = TMResourceType.Gold;
        [SerializeField, Min(0)] private int amount = 10;

        public TMResourceType ResourceType => resourceType;
        public int Amount => amount;
    }
}
