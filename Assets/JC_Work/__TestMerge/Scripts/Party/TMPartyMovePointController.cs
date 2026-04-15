using UnityEngine;

namespace Orora.TestMerge
{
    public class TMPartyMovePointController
    {
        private readonly int maxMovePoints;
        private int remainingMovePoints;

        public int RemainingMovePoints => remainingMovePoints;
        public int MaxMovePoints => maxMovePoints;

        public TMPartyMovePointController(int maxMovePoints)
        {
            this.maxMovePoints = Mathf.Max(0, maxMovePoints);
            remainingMovePoints = this.maxMovePoints;
        }

        public bool CanSpend(int amount) => amount >= 0 && amount <= remainingMovePoints;

        public void SpendStep()
        {
            remainingMovePoints = Mathf.Max(0, remainingMovePoints - 1);
        }

        public void ResetToMax()
        {
            remainingMovePoints = maxMovePoints;
        }
    }
}
