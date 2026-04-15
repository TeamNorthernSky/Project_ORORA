using System.IO;
using UnityEngine;
using UnityEditor;

namespace Orora.ImageObjectForge
{
    internal static class ForgeIO
    {
        public const string ToolRoot = "Assets/JC_Work/_Tools/ImageObjectForge";
        public const string SourcesDir = ToolRoot + "/Sources";
        public const string OutputDir = ToolRoot + "/Output";

        public static bool LoadImageFromDialog(out Texture2D tex, out string assetPath, out string errorMsg)
        {
            tex = null; assetPath = null; errorMsg = null;
            string path = EditorUtility.OpenFilePanel("Load Image", "", "png,jpg,jpeg");
            if (string.IsNullOrEmpty(path)) { errorMsg = "취소됨"; return false; }
            return LoadImage(path, out tex, out assetPath, out errorMsg);
        }

        public static bool LoadImage(string srcAbsPath, out Texture2D tex, out string assetPath, out string errorMsg)
        {
            tex = null; assetPath = null; errorMsg = null;
            if (!File.Exists(srcAbsPath))
            {
                errorMsg = "파일이 존재하지 않음: " + srcAbsPath;
                return false;
            }

            EnsureDir(SourcesDir);

            string stem = Path.GetFileNameWithoutExtension(srcAbsPath);
            string ext = Path.GetExtension(srcAbsPath).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext)) ext = ".png";

            string destName = stem + ext;
            string absSourcesDir = AbsPath(SourcesDir);
            string destAbs = Path.Combine(absSourcesDir, destName);

            // 중복 시 _2, _3 ...
            int i = 2;
            while (File.Exists(destAbs))
            {
                destName = stem + "_" + i + ext;
                destAbs = Path.Combine(absSourcesDir, destName);
                i++;
            }

            try { File.Copy(srcAbsPath, destAbs); }
            catch (System.Exception ex) { errorMsg = "복사 실패: " + ex.Message; return false; }

            assetPath = SourcesDir + "/" + destName;
            AssetDatabase.ImportAsset(assetPath);

            byte[] data;
            try { data = File.ReadAllBytes(destAbs); }
            catch (System.Exception ex) { errorMsg = "읽기 실패: " + ex.Message; return false; }

            tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            if (!tex.LoadImage(data, false))
            {
                Object.DestroyImmediate(tex);
                tex = null;
                errorMsg = "디코딩 실패 (지원되는 png/jpg인지 확인)";
                return false;
            }
            return true;
        }

        public static RectInt ComputeMaskBounds(byte[] mask, int imgW, int imgH)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            for (int y = 0; y < imgH; y++)
            {
                int row = y * imgW;
                for (int x = 0; x < imgW; x++)
                {
                    if (mask[row + x] != 0)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }
            if (minX > maxX) return new RectInt(0, 0, 0, 0);
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        // 마스크=0 픽셀은 투명. 마스크≠0 픽셀은 원본 RGB + A=255.
        // bounds는 이미지 좌표(y-up). 결과 텍스처도 y-up(텍스처 표준) 그대로.
        public static Texture2D BuildCropped(Texture2D source, byte[] mask, RectInt bounds)
        {
            int W = source.width;
            var srcPix = source.GetPixels32();
            var dst = new Color32[bounds.width * bounds.height];
            var transparent = new Color32(0, 0, 0, 0);

            for (int dy = 0; dy < bounds.height; dy++)
            {
                int srcRow = (bounds.y + dy) * W + bounds.x;
                int dstRow = dy * bounds.width;
                for (int dx = 0; dx < bounds.width; dx++)
                {
                    if (mask[srcRow + dx] != 0)
                    {
                        var c = srcPix[srcRow + dx];
                        c.a = 255;
                        dst[dstRow + dx] = c;
                    }
                    else
                    {
                        dst[dstRow + dx] = transparent;
                    }
                }
            }
            var t = new Texture2D(bounds.width, bounds.height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            t.SetPixels32(dst);
            t.Apply(false, false);
            return t;
        }

        public static string SavePng(Texture2D tex, string filename, out string errorMsg)
        {
            errorMsg = null;
            EnsureDir(OutputDir);
            string assetPath = OutputDir + "/" + filename;
            string abs = AbsPath(assetPath);
            try
            {
                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(abs, bytes);
                AssetDatabase.ImportAsset(assetPath);
            }
            catch (System.Exception ex) { errorMsg = ex.Message; return null; }
            return assetPath;
        }

        public static string NextAvailableOutputName(string stem)
        {
            EnsureDir(OutputDir);
            string absDir = AbsPath(OutputDir);
            string baseName = string.IsNullOrEmpty(stem) ? "cut" : stem + "_cut";
            string candidate = baseName + ".png";
            int i = 2;
            while (File.Exists(Path.Combine(absDir, candidate)))
            {
                candidate = baseName + "_" + i + ".png";
                i++;
            }
            return candidate;
        }

        public static void EnsureDir(string assetDir)
        {
            string abs = AbsPath(assetDir);
            if (!Directory.Exists(abs)) Directory.CreateDirectory(abs);
        }

        public static string AbsPath(string assetPath)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), assetPath);
        }
    }
}
