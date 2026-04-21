using UnityEngine;

namespace KJ_Work.Integration
{
    /// <summary>
    /// JC_Work의 AutoBootstrap에 의해 생성되는 레거시 [GameManager]를 억제합니다.
    /// KJ_Work 시스템이 활성화된 환경에서 중복 매니저로 인한 혼선을 방지하는 목적으로 사용합니다.
    /// </summary>
    public static class KJ_SuppressLegacyBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            // KJ_GameManager가 씬에 있는 경우에만 레거시 억제 작동
            if (Object.FindObjectOfType<KJ_GameManager>() != null)
            {
                SuppressLegacyManager();
            }
        }

        public static void SuppressLegacyManager()
        {
            GameObject legacyGM = GameObject.Find("[GameManager]");
            if (legacyGM != null)
            {
                // 이미 생성된 경우 삭제
                Debug.LogWarning("[KJ_SuppressLegacyBootstrap] 레거시 [GameManager] 감지됨. 중복 방지를 위해 삭제합니다.");
                Object.DestroyImmediate(legacyGM);
            }
            
            // 만약 GameManager.Instance에 무언가 할당되어 있고 그것이 레거시라면 해제
            // (타입 체크가 필요할 수 있으나 여기서는 이름으로 우선 판단)
        }
    }
}
