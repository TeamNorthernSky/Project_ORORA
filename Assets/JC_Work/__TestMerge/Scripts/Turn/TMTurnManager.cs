using System;
using UnityEngine;

namespace Orora.TestMerge
{
    /// <summary>
    /// DH TurnManager를 TM용으로 복제. DayAdvanced 이벤트로 일 진행 알림.
    /// 파티 이동 포인트 리셋 및 광산 생산은 별도 리스너 혹은 TurnManager 내부에서 수행.
    /// </summary>
    public class TMTurnManager : MonoBehaviour
    {
        public event Action<int> DayAdvanced;
        public event Action TurnStarted;

        [SerializeField] private int day = 1;
        [SerializeField] private TMPartyRegistry partyRegistry;

        public int CurrentDay => day;

        public void EndPlayerTurn()
        {
            StartEnemyTurn();
        }

        private void StartEnemyTurn()
        {
            // 적 AI 자리 (DH와 동일하게 현재는 skip)
            EndEnemyTurn();
        }

        private void EndEnemyTurn()
        {
            AdvanceDay();
            ProduceClaimedMines();
            StartPlayerTurn();
        }

        private void StartPlayerTurn()
        {
            if (partyRegistry == null) return;

            var movers = partyRegistry.PartyMovers;
            for (int i = 0; i < movers.Length; i++)
            {
                if (movers[i] == null) continue;
                movers[i].ResetMovePointsToMax();
            }

            TurnStarted?.Invoke();
        }

        private void AdvanceDay()
        {
            day++;
            DayAdvanced?.Invoke(day);
        }

        private void ProduceClaimedMines()
        {
            var mines = FindObjectsByType<TMMine>(FindObjectsSortMode.None);
            for (int i = 0; i < mines.Length; i++)
            {
                if (mines[i] == null) continue;
                mines[i].ProduceForTurn();
            }
        }

        public int GetDay() => day;
    }
}
