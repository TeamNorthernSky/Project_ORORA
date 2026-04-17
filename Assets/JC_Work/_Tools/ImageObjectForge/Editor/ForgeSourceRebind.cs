using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Orora.ImageObjectForge
{
    internal static class ForgeSourceRebind
    {
        public class ValidationResult
        {
            public int total;
            public int compatible;
            public int incompatible;
            public List<string> issues = new List<string>();
            public List<SidecarEntry> entries = new List<SidecarEntry>();
        }

        public class SidecarEntry
        {
            public string sidecarPath;
            public ForgeCropMeta oldMeta;
            public bool compatible;
            public string issue;
        }

        public static List<string> FindSidecarsInFolder(string folderAssetPath)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(folderAssetPath)) return result;
            if (!AssetDatabase.IsValidFolder(folderAssetPath)) return result;

            var guids = AssetDatabase.FindAssets("t:TextAsset", new[] { folderAssetPath });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".crop.json", System.StringComparison.OrdinalIgnoreCase))
                    result.Add(path);
            }
            return result;
        }

        public static ValidationResult Validate(string folderAssetPath, Texture2D newSourceTexture)
        {
            var r = new ValidationResult();
            if (newSourceTexture == null) { r.issues.Add("New Source Image이 지정되지 않음"); return r; }
            string newPath = AssetDatabase.GetAssetPath(newSourceTexture);
            if (string.IsNullOrEmpty(newPath)) { r.issues.Add("New Source가 유효한 에셋이 아님"); return r; }
            if (string.IsNullOrEmpty(folderAssetPath)) { r.issues.Add("Scan Folder 미지정"); return r; }
            if (!AssetDatabase.IsValidFolder(folderAssetPath)) { r.issues.Add($"유효한 폴더 아님: {folderAssetPath}"); return r; }

            var newInfo = ForgePngInfo.Read(ForgeIO.AbsPath(newPath));

            int newW = 0, newH = 0;
            var importer = AssetImporter.GetAtPath(newPath) as TextureImporter;
            if (importer != null)
            {
                importer.GetSourceTextureWidthAndHeight(out newW, out newH);
            }
            if (newW == 0 || newH == 0)
            {
                newW = newSourceTexture.width;
                newH = newSourceTexture.height;
            }

            var sidecars = FindSidecarsInFolder(folderAssetPath);
            r.total = sidecars.Count;

            foreach (var sidecarPath in sidecars)
            {
                var entry = new SidecarEntry { sidecarPath = sidecarPath };
                var meta = ForgeIO.LoadCropSidecar(sidecarPath);
                if (meta == null)
                {
                    entry.issue = "사이드카 파싱 실패";
                    r.incompatible++;
                    r.entries.Add(entry);
                    continue;
                }
                entry.oldMeta = meta;

                if (meta.sourceSize.width != newW || meta.sourceSize.height != newH)
                {
                    entry.issue = $"크기 불일치 (기존 {meta.sourceSize.width}x{meta.sourceSize.height} → 새 {newW}x{newH})";
                    r.incompatible++;
                    r.entries.Add(entry);
                    continue;
                }

                if (meta.sourceBitDepth > 0 && newInfo.isPng)
                {
                    if (meta.sourceBitDepth != newInfo.bitDepth)
                    {
                        entry.issue = $"비트 깊이 불일치 (기존 {meta.sourceBitDepth} → 새 {newInfo.bitDepth})";
                        r.incompatible++;
                        r.entries.Add(entry);
                        continue;
                    }
                    if (meta.sourceColorType >= 0 && meta.sourceColorType != newInfo.colorType)
                    {
                        entry.issue = $"컬러 타입 불일치 (기존 {meta.sourceColorType} → 새 {newInfo.colorType})";
                        r.incompatible++;
                        r.entries.Add(entry);
                        continue;
                    }
                }

                if (meta.sourceDpiX > 0 && newInfo.hasDpi)
                {
                    if (meta.sourceDpiX != newInfo.dpiX || meta.sourceDpiY != newInfo.dpiY)
                    {
                        entry.issue = $"DPI 불일치 (기존 {meta.sourceDpiX}x{meta.sourceDpiY} → 새 {newInfo.dpiX}x{newInfo.dpiY})";
                        r.incompatible++;
                        r.entries.Add(entry);
                        continue;
                    }
                }

                entry.compatible = true;
                r.compatible++;
                r.entries.Add(entry);
            }

            return r;
        }

        public static int Rebind(ValidationResult validation, Texture2D newSourceTexture)
        {
            if (validation == null || validation.incompatible > 0 || newSourceTexture == null) return 0;

            string newPath = AssetDatabase.GetAssetPath(newSourceTexture);
            if (string.IsNullOrEmpty(newPath)) return 0;
            string newGUID = AssetDatabase.AssetPathToGUID(newPath);
            var newInfo = ForgePngInfo.Read(ForgeIO.AbsPath(newPath));

            int updated = 0;
            foreach (var entry in validation.entries)
            {
                if (!entry.compatible || entry.oldMeta == null) continue;
                entry.oldMeta.version = 2;
                entry.oldMeta.sourceAssetPath = newPath;
                entry.oldMeta.sourceGUID = newGUID;
                if (newInfo.isPng)
                {
                    entry.oldMeta.sourceBitDepth = newInfo.bitDepth;
                    entry.oldMeta.sourceColorType = newInfo.colorType;
                    entry.oldMeta.sourceDpiX = newInfo.dpiX;
                    entry.oldMeta.sourceDpiY = newInfo.dpiY;
                }
                string json = JsonUtility.ToJson(entry.oldMeta, true);
                try
                {
                    File.WriteAllText(ForgeIO.AbsPath(entry.sidecarPath), json);
                    AssetDatabase.ImportAsset(entry.sidecarPath);
                    updated++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[ImageObjectForge] Rebind 쓰기 실패 {entry.sidecarPath}: {ex.Message}");
                }
            }
            return updated;
        }
    }
}
