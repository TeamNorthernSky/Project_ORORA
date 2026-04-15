using System.Collections;
using UnityEngine;

namespace Orora.TestMerge
{
    /// <summary>
    /// APlayScene 엔트리. 매니저들의 초기화 순서 오케스트레이션.
    /// 1) TMGridManager는 자체 Awake에서 기본 초기화 → TMLevelLoader.Start 가 재초기화
    /// 2) GameManager(JC)는 AutoBootstrap이 BeforeSceneLoad에 소환 → Currency 준비 완료
    /// 3) TMLevelLoader Start → 그리드 크기 확정 + 오브젝트 스폰 + 파티 Snap
    /// 4) TMFogRenderManager.Initialize (그리드 확정 후)
    /// 5) TMTurnFogController.InitialReveal (revealers 등록 후 1회 공개)
    /// </summary>
    public class TMAPlaySceneController : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private TMGridManager gridManager;
        [SerializeField] private TMLevelLoader levelLoader;
        [SerializeField] private TMFogRenderManager fogRenderManager;
        [SerializeField] private TMFogFadeAnimator fadeAnimator;
        [SerializeField] private TMFogRevealerRegistry revealerRegistry;
        [SerializeField] private TMTurnFogController turnFogController;
        [SerializeField] private TMTurnManager turnManager;

        [Header("Initial Currency")]
        [SerializeField] private int initialGold = 0;
        [SerializeField] private int initialWood = 0;
        [SerializeField] private int initialOre = 0;

        private void Start()
        {
            StartCoroutine(InitializeSequence());
        }

        private IEnumerator InitializeSequence()
        {
            // GameManager(JC)가 먼저 초기화되도록 1프레임 대기 (AutoBootstrap BeforeSceneLoad)
            yield return null;

            EnsureCurrencyInitialized();

            // levelLoader가 Start에서 gridManager.Initialize 호출. 그 이후에 fog init.
            // levelLoader.Start는 이 코루틴 진입 시점에 이미 실행되어 있음.
            // 한 프레임 더 대기하여 Start 순서 차이를 흡수.
            yield return null;

            if (fogRenderManager != null)
                fogRenderManager.Initialize();

            if (fadeAnimator != null && !fadeAnimator.IsInitialized && gridManager != null)
                fadeAnimator.Initialize(gridManager.Width, gridManager.Height);

            yield return null;

            if (turnFogController != null)
                turnFogController.InitialReveal();

            Debug.Log("[TMAPlaySceneController] 초기화 시퀀스 완료");
        }

        private void EnsureCurrencyInitialized()
        {
            if (GameManager.Instance == null || GameManager.Instance.Currency == null) return;
            var c = GameManager.Instance.Currency;
            if (c.Get(CurrencyType.Gold) == 0 && initialGold > 0) c.Set(CurrencyType.Gold, initialGold);
            if (c.Get(CurrencyType.Wood) == 0 && initialWood > 0) c.Set(CurrencyType.Wood, initialWood);
            if (c.Get(CurrencyType.Ore)  == 0 && initialOre  > 0) c.Set(CurrencyType.Ore,  initialOre);
        }
    }
}
