using UnityEngine;

namespace Orora.TestMerge
{
    /// <summary>아이템. 파티 인접 시 자원 가산 후 파괴.</summary>
    public class TMItemObject : MonoBehaviour
    {
        [SerializeField] private TMResourceType resource = TMResourceType.Gold;
        [SerializeField, Min(0)] private int amount = 100;

        public TMResourceType Resource => resource;
        public int Amount => amount;

        public void ApplyInitialData(TMResourceType resource, int amount)
        {
            this.resource = resource;
            this.amount = Mathf.Max(0, amount);
        }

        public void ApplyInitialAmount(int amount)
        {
            this.amount = Mathf.Max(0, amount);
        }

        public void GetItem()
        {
            TMResourceBridge.Add(resource, amount);
            Destroy(gameObject);
        }
    }
}
