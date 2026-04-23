using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Orora.UI.Extensions;

namespace Orora.ImageObjectForge
{
    internal static class ForgePrefabFactory
    {
        public const string PrefabsDir = ForgeIO.ToolRoot + "/Prefabs";
        public const string DefaultFontAssetPath = "Assets/JC_Work/Assets_jc/Maplestory Light SDF.asset";

        public enum PrefabKind { Button, Label, SpriteRenderer }

        public class Options
        {
            public PrefabKind Kind = PrefabKind.Button;
            public Sprite SourceSprite;           // 필수
            public string NameOverride;           // null이면 sprite texture 이름 사용
            // Button 전용
            public TMP_FontAsset Font;
            public string ButtonText = "Button";
            public float FontSize = 14f;
            public Color TextColor = Color.black;
            // 공통
            public Vector2? SizeOverride;         // null이면 sprite 픽셀 크기
        }

        // 여러 Sprite를 일괄 생성. opts의 SourceSprite는 무시됨.
        // useSpriteNameAsButtonText=true 면 각 버튼의 Text를 해당 스프라이트 이름으로 자동 설정
        public static List<GameObject> CreateBatch(IList<Sprite> sprites, Options opts, bool useSpriteNameAsButtonText, out int failCount)
        {
            var result = new List<GameObject>();
            failCount = 0;
            if (sprites == null || sprites.Count == 0) return result;

            foreach (var sp in sprites)
            {
                if (sp == null) { failCount++; continue; }
                var o = new Options
                {
                    Kind = opts.Kind,
                    SourceSprite = sp,
                    NameOverride = null,
                    Font = opts.Font,
                    ButtonText = useSpriteNameAsButtonText ? sp.texture.name : opts.ButtonText,
                    FontSize = opts.FontSize,
                    TextColor = opts.TextColor,
                    SizeOverride = opts.SizeOverride,
                };
                var go = Create(o, out _);
                if (go != null) result.Add(go);
                else failCount++;
            }
            return result;
        }

        public static GameObject Create(Options opts, out string errorMsg)
        {
            errorMsg = null;
            if (opts == null || opts.SourceSprite == null) { errorMsg = "Sprite가 지정되지 않았습니다."; return null; }

            EnsureDir(PrefabsDir);

            string stem = string.IsNullOrEmpty(opts.NameOverride)
                ? opts.SourceSprite.texture.name
                : opts.NameOverride;
            string assetPath = NextAvailablePrefabPath(stem);

            GameObject root;
            switch (opts.Kind)
            {
                case PrefabKind.Button:         root = BuildButton(opts); break;
                case PrefabKind.Label:          root = BuildLabel(opts); break;
                case PrefabKind.SpriteRenderer: root = BuildSpriteRenderer(opts); break;
                default:
                    errorMsg = "지원하지 않는 타입";
                    return null;
            }

            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            Object.DestroyImmediate(root);
            if (savedPrefab == null) { errorMsg = "프리팹 저장 실패"; return null; }

            AssetDatabase.ImportAsset(assetPath);
            return savedPrefab;
        }

        // -------- Button --------
        static GameObject BuildButton(Options opts)
        {
            var size = ResolveSize(opts);
            var go = new GameObject(opts.SourceSprite.texture.name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            ApplyTopLeftAnchor(rt);
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.sprite = opts.SourceSprite;
            img.raycastTarget = true;

            // 스프라이트 텍스처 Readable 보장 (런타임 alpha hit test에 필요).
            if (opts.SourceSprite != null)
            {
                var spritePath = AssetDatabase.GetAssetPath(opts.SourceSprite);
                if (!string.IsNullOrEmpty(spritePath))
                {
                    var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
                    if (importer != null && !importer.isReadable)
                    {
                        importer.isReadable = true;
                        importer.SaveAndReimport();
                        var reloaded = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                        if (reloaded != null)
                        {
                            opts.SourceSprite = reloaded;
                            img.sprite = reloaded;
                        }
                    }
                }
            }

            // alphaHitTestMinimumThreshold는 직렬화 안됨.
            // 런타임에 값을 재설정할 전용 컴포넌트를 부착.
            var aht = go.AddComponent<AlphaHitThreshold>();
            aht.threshold = 0.5f;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            // 컬러 틴트 기본 유지 (Unity Button 디폴트)

            // 자식 Text (TMP)
            var textGO = new GameObject(go.name + "Text", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = opts.ButtonText ?? "Button";
            tmp.fontSize = opts.FontSize;
            tmp.color = opts.TextColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            var font = opts.Font != null ? opts.Font : LoadDefaultFont();
            if (font != null) tmp.font = font;

            return go;
        }

        // -------- Label (Image 단독) --------
        static GameObject BuildLabel(Options opts)
        {
            var size = ResolveSize(opts);
            var go = new GameObject(opts.SourceSprite.texture.name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            ApplyTopLeftAnchor(rt);
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.sprite = opts.SourceSprite;
            img.raycastTarget = false;

            return go;
        }

        // -------- SpriteRenderer (월드) --------
        static GameObject BuildSpriteRenderer(Options opts)
        {
            var go = new GameObject(opts.SourceSprite.texture.name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = opts.SourceSprite;
            return go;
        }

        // -------- Helpers --------
        // Auto-Place 좌상단 표준과 일관: anchor·pivot 모두 (0, 1).
        public static void ApplyTopLeftAnchor(RectTransform rt)
        {
            var topLeft = new Vector2(0f, 1f);
            rt.anchorMin = topLeft;
            rt.anchorMax = topLeft;
            rt.pivot = topLeft;
        }

        static Vector2 ResolveSize(Options opts)
        {
            if (opts.SizeOverride.HasValue) return opts.SizeOverride.Value;
            var r = opts.SourceSprite.rect;
            return new Vector2(r.width, r.height);
        }

        public static TMP_FontAsset LoadDefaultFont()
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontAssetPath);
        }

        static void EnsureDir(string assetDir)
        {
            string abs = Path.Combine(Directory.GetCurrentDirectory(), assetDir);
            if (!Directory.Exists(abs)) Directory.CreateDirectory(abs);
        }

        static string NextAvailablePrefabPath(string stem)
        {
            string absDir = Path.Combine(Directory.GetCurrentDirectory(), PrefabsDir);
            string candidate = stem + ".prefab";
            int i = 2;
            while (File.Exists(Path.Combine(absDir, candidate)))
            {
                candidate = stem + "_" + i + ".prefab";
                i++;
            }
            return PrefabsDir + "/" + candidate;
        }
    }
}
