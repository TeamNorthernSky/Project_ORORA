using UnityEngine;
#if !UNITY_EDITOR
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

public static class AutoBootstrap
{
    private const string PrefabPath = "Assets/JC_Work/Prefab_jc/[GameManager].prefab";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Execute()
    {
        if (GameManager.Instance != null) return;

        GameObject prefab = LoadPrefab();
        if (prefab != null)
        {
            var go = Object.Instantiate(prefab);
            go.name = "[GameManager]";
            Object.DontDestroyOnLoad(go);
        }
        else
        {
            Debug.LogError("[AutoBootstrap] GameManager 프리팹 로드 실패");
        }
    }

    private static GameObject LoadPrefab()
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
#else
        Addressables.InitializeAsync().WaitForCompletion();
        var handle = Addressables.LoadAssetAsync<GameObject>("GameManager");
        handle.WaitForCompletion();
        return handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null;
#endif
    }
}
