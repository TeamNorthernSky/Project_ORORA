using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class HQModalButtonsPopulator
{
    private const string LobbyScenePath = "Assets/JC_Work/__ProtoType/Scenes/LobbyScene.unity";
    private const string HQModalName    = "Modal_HQ";
    private const string PanelName      = "PanelHQModal";

    // 회색 오버레이 색상 — LobbyScene 13버튼과 동일 (#808080 α 0.2 ≈ 51/255)
    private static readonly Color HoverGrayColor = new Color32(0x80, 0x80, 0x80, 0x33);

    [MenuItem("Tools/Lobby/Setup HQ Modal Buttons")]
    public static void Setup()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.Log("[HQModalButtonsPopulator] 사용자 취소");
            return;
        }

        var scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);

        Transform hqModal = null;
        foreach (var go in scene.GetRootGameObjects())
        {
            hqModal = FindRecursive(go.transform, HQModalName);
            if (hqModal != null) break;
        }
        if (hqModal == null)
        {
            Debug.LogError($"[HQModalButtonsPopulator] LobbyScene에서 {HQModalName} 못 찾음");
            return;
        }

        var panel = FindRecursive(hqModal, PanelName);
        if (panel == null)
        {
            Debug.LogError($"[HQModalButtonsPopulator] {HQModalName} 안에서 {PanelName} 못 찾음");
            return;
        }

        var buttons = panel.GetComponentsInChildren<Button>(true);
        if (buttons.Length == 0)
        {
            Debug.LogWarning($"[HQModalButtonsPopulator] {PanelName} 자식에 Button이 없음");
            return;
        }

        int processed = 0, skipped = 0;
        foreach (var btn in buttons)
        {
            bool any = ApplyHoverGray(btn);
            if (any) processed++;
            else skipped++;
        }

        Debug.Log($"[HQModalButtonsPopulator] 부착 완료 — 신규 처리 {processed}건, 스킵(이미 부착) {skipped}건, 전체 {buttons.Length}건");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // 버튼에 HoverGray 자식 + AudioSource + ButtonEffectAudio + ButtonEffectActiveToggle 부착.
    // 이미 모두 부착돼 있으면 false 반환(=스킵).
    static bool ApplyHoverGray(Button btn)
    {
        bool changed = false;

        // 1. HoverGray 자식
        var hoverGrayTr = btn.transform.Find("HoverGray");
        GameObject hoverGray;
        if (hoverGrayTr == null)
        {
            hoverGray = new GameObject("HoverGray", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            hoverGray.transform.SetParent(btn.transform, false);
            FullStretch(hoverGray.GetComponent<RectTransform>());
            var img = hoverGray.GetComponent<Image>();
            img.color = HoverGrayColor;
            img.raycastTarget = false;
            hoverGray.SetActive(false);
            changed = true;
        }
        else
        {
            hoverGray = hoverGrayTr.gameObject;
        }

        // 2. AudioSource
        if (btn.GetComponent<AudioSource>() == null)
        {
            var src = btn.gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            changed = true;
        }

        // 3. ButtonEffectAudio
        if (btn.GetComponent<ButtonEffectAudio>() == null)
        {
            btn.gameObject.AddComponent<ButtonEffectAudio>();
            changed = true;
        }

        // 4. ButtonEffectActiveToggle + hoverObjects = [HoverGray]
        var toggle = btn.GetComponent<ButtonEffectActiveToggle>();
        if (toggle == null)
        {
            toggle = btn.gameObject.AddComponent<ButtonEffectActiveToggle>();
            changed = true;
        }
        var so = new SerializedObject(toggle);
        var arr = so.FindProperty("hoverObjects");
        if (arr != null)
        {
            // 이미 hoverGray가 등록돼 있는지 확인
            bool already = false;
            for (int i = 0; i < arr.arraySize; i++)
            {
                if (arr.GetArrayElementAtIndex(i).objectReferenceValue == hoverGray)
                {
                    already = true; break;
                }
            }
            if (!already)
            {
                arr.arraySize = 1;
                arr.GetArrayElementAtIndex(0).objectReferenceValue = hoverGray;
                so.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }
        }

        return changed;
    }

    static Transform FindRecursive(Transform root, string name)
    {
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindRecursive(root.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    static void FullStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
