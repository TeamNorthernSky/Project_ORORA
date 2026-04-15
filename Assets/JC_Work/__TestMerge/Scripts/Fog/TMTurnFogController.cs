using UnityEngine;

namespace Orora.TestMerge
{
    /// <summary>
    /// 턴 진행 시 안개 갱신 오케스트레이터.
    /// 1) 모든 Revealer 위치 재공개 (lastRevealedDay = newDay)
    /// 2) 리포그 후보 셀 수집 (lastRevealedDay 가 delay 경과)
    /// 3) 후보 셀 visibility = Fogged + 1.5s 페이드 시작
    /// </summary>
    public class TMTurnFogController : MonoBehaviour
    {
        [SerializeField] private TMTurnManager turnManager;
        [SerializeField] private TMGridManager gridManager;
        [SerializeField] private TMFogRenderManager fogRenderManager;
        [SerializeField] private TMFogFadeAnimator fadeAnimator;

        [SerializeField, Min(1)] private int refogDelayDays = 3;

        public int RefogDelayDays => refogDelayDays;

        private void OnEnable()
        {
            if (turnManager != null)
                turnManager.DayAdvanced += HandleDayAdvanced;
        }

        private void OnDisable()
        {
            if (turnManager != null)
                turnManager.DayAdvanced -= HandleDayAdvanced;
        }

        private void HandleDayAdvanced(int newDay)
        {
            if (gridManager == null) return;

            // 1) Revealer 위치 재반영 (newDay로 lastRevealedDay 갱신)
            fogRenderManager?.RevealAll();

            // 2) 리포그 후보 수집
            var candidates = gridManager.CollectCellsToRefog(newDay, refogDelayDays);

            // 3) 각 후보: 상태 Fogged 로 변경 + 시각 페이드
            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                gridManager.SetVisibility(c, TMFogVisibilityState.Fogged);
                fadeAnimator?.StartRefogFade(c.x, c.y);
            }
        }

        /// <summary>씬 초기화 시 1회 reveal. TMAPlaySceneController가 호출.</summary>
        public void InitialReveal()
        {
            fogRenderManager?.RevealAll();
        }
    }
}
