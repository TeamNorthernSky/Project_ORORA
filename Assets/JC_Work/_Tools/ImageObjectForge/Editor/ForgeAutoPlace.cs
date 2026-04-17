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
            public int skippedSourceMismatch;
            public int skippedMissingComponents;
            public List<string> warnings = new List<string>();
            public List<string> errors = new List<string>();
        }

        public static Report Run(Canvas targetCanvas, string scanFolderAssetPath)
        {
            var r = new Report();
            if (targetCanvas == null) { r.errors.Add("Target Canvas가 지정되지 않음"); return r; }
            if (string.IsNullOrEmpty(scanFolderAssetPath)) { r.errors.Add("Scan Folder가 지정되지 않음"); return r; }
            if (!AssetDatabase.IsValidFolder(scanFolderAssetPath)) { r.errors.Add($"유효한 폴더가 아님: {scanFolderAssetPath}"); return r; }

            RawImage bgRaw = null;
            foreach (Transform child in targetCanvas.transform)
            {
                var ri = child.GetComponent<RawImage>();
                if (ri != null && ri.texture != null) { bgRaw = ri; break; }
            }
            if (bgRaw == null)
            {
                r.errors.Add("Canvas 직계 자식 중 texture가 설정된 RawImage를 찾을 수 없음");
                return r;
            }

            var bgTex = bgRaw.texture;
            string bgPath = AssetDatabase.GetAssetPath(bgTex);
            string bgGUID = AssetDatabase.AssetPathToGUID(bgPath);
            int bgW = bgTex.width;
            int bgH = bgTex.height;

            var scaler = targetCanvas.GetComponent<CanvasScaler>();
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                int refW = Mathf.RoundToInt(scaler.referenceResolution.x);
                int refH = Mathf.RoundToInt(scaler.referenceResolution.y);
                if (refW != bgW || refH != bgH)
                {
                    r.warnings.Add($"CanvasScaler Reference Resolution ({refW}x{refH}) != 배경 크기 ({bgW}x{bgH}). 위치가 어긋날 수 있음.");
                }
            }

            var prefabGUIDs = AssetDatabase.FindAssets("t:Prefab", new[] { scanFolderAssetPath });
            foreach (var guid in prefabGUIDs)
            {
                var prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null) continue;

                var img = prefab.GetComponentInChildren<Image>(true);
                if (img == null || img.sprite == null) { r.skippedMissingComponents++; continue; }

                var spritePath = AssetDatabase.GetAssetPath(img.sprite);
                if (string.IsNullOrEmpty(spritePath)) { r.skippedMissingComponents++; continue; }

                var sidecarPath = ForgeIO.GetSidecarPathFor(spritePath);
                var meta = ForgeIO.LoadCropSidecar(sidecarPath);
                if (meta == null) { r.skippedNoSidecar++; continue; }

                bool matchByGUID = !string.IsNullOrEmpty(meta.sourceGUID) && meta.sourceGUID == bgGUID;
                bool matchByPath = !string.IsNullOrEmpty(meta.sourceAssetPath) && meta.sourceAssetPath == bgPath;
                if (!matchByGUID && !matchByPath) { r.skippedSourceMismatch++; continue; }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, targetCanvas.transform);
                if (instance == null)
                {
                    r.warnings.Add($"{prefab.name}: Instantiate 실패");
                    continue;
                }
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
                    r.warnings.Add($"{prefab.name}: localScale = {rt.localScale.x:0.###}, {rt.localScale.y:0.###}, {rt.localScale.z:0.###} (1,1,1 아님)");
                }

                var center = new Vector2(0.5f, 0.5f);
                if (rt.anchorMin != center || rt.anchorMax != center)
                {
                    r.warnings.Add($"{prefab.name}: 앵커가 (0.5, 0.5)가 아님, 위치가 어긋날 수 있음");
                }

                float cx = meta.cropBounds.x + meta.cropBounds.width / 2f;
                float cy = meta.cropBounds.y + meta.cropBounds.height / 2f;
                rt.anchoredPosition = new Vector2(cx - bgW / 2f, cy - bgH / 2f);

                r.placed++;
            }

            EditorUtility.SetDirty(targetCanvas);
            return r;
        }
    }
}
