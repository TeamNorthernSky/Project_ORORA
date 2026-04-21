using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KJ_Work.Integration
{
    public static class KJ_AutoBootstrap
    {
        private const string PrefabPath = "Assets/KJ_Work/Prefabs/[KJ_GameManager].prefab";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Execute()
        {
            // KJ_GameManager가 이미 존재하는지 확인 (싱글톤 체크)
            if (KJ_GameManager.Instance != null) return;

            GameObject prefab = LoadPrefab();
            if (prefab != null)
            {
                var go = Object.Instantiate(prefab);
                go.name = "[KJ_GameManager]";
                Object.DontDestroyOnLoad(go);
                Debug.Log("[KJ_AutoBootstrap] KJ_GameManager가 자동 생성되었습니다.");
            }
            else
            {
                // 프리팹이 아직 없는 경우 에디터 경고 (워크플로우 상 프리팹 생성이 필요함)
                if (Application.isEditor)
                {
                    Debug.LogWarning($"[KJ_AutoBootstrap] '{PrefabPath}' 프리팹을 찾을 수 없습니다. 프리팹 생성이 필요합니다.");
                }
            }
        }

        private static GameObject LoadPrefab()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
#else
            // 빌드 시에는 Resources나 Addressables 등 별도 경로 처리가 필요할 수 있으나
            // 현재 요구사항은 에디터 및 개발 환경에서의 JC Fog 구현이므로 AssetDatabase 우선 사용
            return null; 
#endif
        }
    }
}
