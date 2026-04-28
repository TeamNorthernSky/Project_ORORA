using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HQModalSwapper
{
    private const string LobbyScenePath = "Assets/JC_Work/__ProtoType/Scenes/LobbyScene.unity";
    private const string OldModalName   = "Modal_HQ";

    [MenuItem("Tools/Lobby/Swap HQ Modal (use selected prefab)")]
    public static void Swap()
    {
        var prefab = Selection.activeObject as GameObject;
        if (prefab == null
            || !EditorUtility.IsPersistent(prefab)
            || PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.NotAPrefab
            || PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.MissingAsset)
        {
            EditorUtility.DisplayDialog(
                "HQ Modal Swap",
                "Project 창에서 **prefab asset 파일**을 선택한 뒤 다시 실행하세요.\n" +
                "(씬 안의 prefab 인스턴스나 .cs 파일 등은 안 됩니다)",
                "OK");
            return;
        }
        Debug.Log($"[HQModalSwapper] 선택된 prefab: {AssetDatabase.GetAssetPath(prefab)}");

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("[HQModalSwapper] 사용자 취소");
            return;
        }

        var scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);

        Transform canvas = null;
        foreach (var go in scene.GetRootGameObjects())
        {
            canvas = FindRecursive(go.transform, "CanvasLobby");
            if (canvas != null) break;
        }
        if (canvas == null)
        {
            Debug.LogError("[HQModalSwapper] CanvasLobby 못 찾음");
            return;
        }

        // 기존 Modal_HQ 위치 기록 후 삭제
        int siblingIndex = -1;
        var oldModal = FindRecursive(canvas, OldModalName);
        if (oldModal != null)
        {
            siblingIndex = oldModal.GetSiblingIndex();
            Object.DestroyImmediate(oldModal.gameObject);
            Debug.Log($"[HQModalSwapper] 기존 {OldModalName} 삭제 (siblingIndex={siblingIndex})");
        }
        else
        {
            Debug.LogWarning($"[HQModalSwapper] 기존 {OldModalName} 못 찾음 — 신규 인스턴스만 배치");
        }

        // 새 prefab 인스턴스 배치
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
        if (instance == null)
        {
            Debug.LogError($"[HQModalSwapper] InstantiatePrefab 실패 — prefab이 valid asset이 아닐 수 있습니다. 경로: {AssetDatabase.GetAssetPath(prefab)}");
            return;
        }
        instance.name = OldModalName;
        if (siblingIndex >= 0) instance.transform.SetSiblingIndex(siblingIndex);
        Debug.Log($"[HQModalSwapper] 신규 {prefab.name} → {OldModalName} 배치");

        // LobbyMenuController._modalHQ 재연결
        // BuildModalPrefab 출력 = Root(활성) → Modal(비활성). 토글 대상은 Modal 자식.
        var modalChild = instance.transform.Find("Modal");
        if (modalChild == null)
        {
            Debug.LogWarning("[HQModalSwapper] 인스턴스에 'Modal' 자식이 없음. _modalHQ 연결을 수동으로 해주세요");
        }
        else
        {
            LobbyMenuController controller = null;
            foreach (var go in scene.GetRootGameObjects())
            {
                controller = go.GetComponentInChildren<LobbyMenuController>(true);
                if (controller != null) break;
            }
            if (controller == null)
            {
                Debug.LogWarning("[HQModalSwapper] LobbyMenuController 못 찾음. _modalHQ 연결 수동 필요");
            }
            else
            {
                var so = new SerializedObject(controller);
                var prop = so.FindProperty("_modalHQ");
                if (prop == null)
                {
                    Debug.LogWarning("[HQModalSwapper] LobbyMenuController._modalHQ 필드 못 찾음");
                }
                else
                {
                    prop.objectReferenceValue = modalChild.gameObject;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("[HQModalSwapper] LobbyMenuController._modalHQ → 새 인스턴스의 Modal 자식");
                }
            }
        }

        // CloseButton 자동 닫기 매칭 진단
        // (LobbyMenuController.BindModalCloseButtons는 자식 중 'CloseButton' 이름의 Button을 찾음)
        Transform closeBtn = modalChild != null
            ? FindRecursive(modalChild, "CloseButton")
            : null;
        if (closeBtn == null)
        {
            Debug.LogWarning("[HQModalSwapper] 'CloseButton' 이름의 자식이 새 모달에 없음. " +
                             "X 닫기 버튼이 있다면 GameObject 이름을 'CloseButton'으로 변경하면 자동 닫기 매칭됨.");
        }
        else
        {
            Debug.Log("[HQModalSwapper] CloseButton 자동 닫기 매칭 가능");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[HQModalSwapper] 완료");
    }

    private static Transform FindRecursive(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindRecursive(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
