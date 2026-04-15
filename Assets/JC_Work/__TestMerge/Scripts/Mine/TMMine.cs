using System;
using UnityEngine;

namespace Orora.TestMerge
{
    /// <summary>광산. Unclaimed 상태에서 파티가 인접 시 Claim. Claimed면 매 턴 자원 생산.</summary>
    public class TMMine : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string mineId = "mine_001";

        [Header("Production")]
        [SerializeField] private TMResourceType producedResource = TMResourceType.Ore;
        [SerializeField, Min(0)] private int amountPerTurn = 10;

        [Header("State")]
        [SerializeField] private TMMineState state = TMMineState.Unclaimed;

        public string MineId => mineId;
        public TMMineState State => state;
        public TMResourceType ProducedResource => producedResource;
        public int AmountPerTurn => amountPerTurn;

        public static event Action<TMMine> MineClaimed;

        public void ApplyInitialData(TMResourceType resource, int amountPerTurn, TMMineState state)
        {
            this.producedResource = resource;
            this.amountPerTurn = Mathf.Max(0, amountPerTurn);
            this.state = state;
        }

        public void ApplyInitialData(int amountPerTurn, TMMineState state)
        {
            this.amountPerTurn = Mathf.Max(0, amountPerTurn);
            this.state = state;
        }

        public void MineClaim()
        {
            if (state == TMMineState.Claimed) return;
            state = TMMineState.Claimed;
            MineClaimed?.Invoke(this);
        }

        public void ProduceForTurn()
        {
            if (state != TMMineState.Claimed) return;
            TMResourceBridge.Add(producedResource, amountPerTurn);
        }
    }
}
