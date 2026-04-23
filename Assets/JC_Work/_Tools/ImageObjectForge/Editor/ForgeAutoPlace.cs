using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Orora.ImageObjectForge
{
    internal static class ForgeAutoPlace
    {
        public class Report
        {
            public int placed;
            public int skippedNoSidecar;
            public int skippedMissingComponents;
            public int skippedInvalidSidecar;
            public List<string> warnings = new List<string>();
            public List<string> errors = new List<string>();
        }

        // 폴더 안 모든 프리팹을 배치 (기존 진입점, 폴더 스캔 후 RunForPrefabs로 위임).
        public static Report Run(Canvas targetCanvas, string scanFolderAssetPath)
        {
            var r = new Report();
            if (string.IsNullOrEmpty(scanFolderAssetPath)) { r.errors.Add("Scan Folder가 지정되지 않음"); return r; }
            if (!AssetDatabase.IsValidFolder(scanFolderAssetPath)) { r.errors.Add($"유효한 폴더가 아님: {scanFolderAssetPath}"); return r; }

            var prefabGUIDs = AssetDatabase.FindAssets("t:Prefab", new[] { scanFolderAssetPath });
            var prefabs = new List<GameObject>(prefabGUIDs.Length);
            foreach (var guid in prefabGUIDs)
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (p != null) prefabs.Add(p);
            }
            return RunForPrefabs(targetCanvas, prefabs);
        }

        // 사이드카(canvasSize + cropBounds) 기반 좌상단 표준 배치.
        // 캔버스 RawImage 등 배경 의존 없음. 캔버스 RefResolution이 사이드카 canvasSize와
        // 다르면 경고 후 환산 없이 사이드카 픽셀을 그대로 적용.
        public static Report RunForPrefabs(Canvas targetCanvas, IList<GameObject> prefabs)
        {
            var r = new Report();
            if (targetCanvas == null) { r.errors.Add("Target Canvas가 지정되지 않음"); return r; }
            if (prefabs == null || prefabs.Count == 0) { r.errors.Add("배치할 프리팹이 없음"); return r; }

            // canvasSize 별 RefResolution 경고 중복 방지
            var warnedSizes = new HashSet<string>();

            foreach (var prefab in prefabs)
            {
                if (prefab == null) continue;

                var img = prefab.GetComponentInChildren<Image>(true);
                if (img == null || img.sprite == null) { r.skippedMissingComponents++; continue; }

                var spritePath = AssetDatabase.GetAssetPath(img.sprite);
                if (string.IsNullOrEmpty(spritePath)) { r.skippedMissingComponents++; continue; }

                var sidecarPath = ForgeIO.GetSidecarPathFor(spritePath);
                var meta = ForgeIO.LoadCropSidecar(sidecarPath);
                if (meta == null) { r.skippedNoSidecar++; continue; }

                int canvasW = meta.canvasSize.width;
                int canvasH = meta.canvasSize.height;
                if (canvasW <= 0 || canvasH <= 0
                    || meta.cropBounds.width <= 0 || meta.cropBounds.height <= 0)
                {
                    r.skippedInvalidSidecar++;
                    r.warnings.Add($"{prefab.name}: canvasSize 또는 cropBounds 무효");
                    continue;
                }

                // RefResolution 비교 — 다르면 경고만, 환산 없이 사이드카 픽셀 그대로 사용.
                var scaler = targetCanvas.GetComponent<CanvasScaler>();
                if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    int refW = Mathf.RoundToInt(scaler.referenceResolution.x);
                    int refH = Mathf.RoundToInt(scaler.referenceResolution.y);
                    if (refW != canvasW || refH != canvasH)
                    {
                        var key = $"{canvasW}x{canvasH}->{refW}x{refH}";
                        if (warnedSizes.Add(key))
                        {
                            r.warnings.Add($"CanvasScaler RefResolution ({refW}x{refH}) != 사이드카 canvasSize ({canvasW}x{canvasH}). 환산 없이 사이드카 픽셀 그대로 적용 (위치 어긋날 수 있음).");
                        }
                    }
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, targetCanvas.transform);
                if (instance == null) { r.warnings.Add($"{prefab.name}: Instantiate 실패"); continue; }
                Undo.RegisterCreatedObjectUndo(instance, "Auto-Place Prefab");

                var rt = instance.GetComponent<RectTransform>();
                if (rt == null)
                {
                    r.warnings.Add($"{prefab.name}: RectTransform 없음, 위치 설정 스킵");
                    r.placed++;
                    continue;
                }

                if (rt.localScale != Vector3.one)
                {
                    r.warnings.Add($"{prefab.name}: localScale = ({rt.localScale.x:0.###}, {rt.localScale.y:0.###}, {rt.localScale.z:0.###}) (1,1,1 아님)");
                }

                // 좌상단 표준 강제: anchor·pivot 모두 (0, 1).
                ForgePrefabFactory.ApplyTopLeftAnchor(rt);

                // 사이드카 좌상단(이미지 y-up → UI y-down 변환):
                //   topLeftX = cropBounds.x
                //   topLeftYFromImageTop = canvasH - (cropBounds.y + cropBounds.height)
                // anchor·pivot이 (0,1)인 자식의 anchoredPosition은
                //   X = 부모 좌상단에서 오른쪽 거리, Y = 부모 좌상단에서 아래 방향(음수).
                float topLeftX = meta.cropBounds.x;
                float topLeftYFromTop = canvasH - (meta.cropBounds.y + meta.cropBounds.height);
                rt.anchoredPosition = new Vector2(topLeftX, -topLeftYFromTop);

                // 크기는 사이드카가 정의 (sprite 크기와 무관하게 강제 적용).
                rt.sizeDelta = new Vector2(meta.cropBounds.width, meta.cropBounds.height);

                r.placed++;
            }

            EditorUtility.SetDirty(targetCanvas);
            return r;
        }
    }
}
