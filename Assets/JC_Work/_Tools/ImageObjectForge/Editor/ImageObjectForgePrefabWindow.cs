using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;

namespace Orora.ImageObjectForge
{
    public class ImageObjectForgePrefabWindow : EditorWindow
    {
        [MenuItem("Tools/JC/Image Object Forge/Create Prefab…")]
        public static void Open()
        {
            var w = GetWindow<ImageObjectForgePrefabWindow>("Prefab Factory");
            w.minSize = new Vector2(420, 540);
        }

        [MenuItem("Tools/JC/Image Object Forge/Batch Convert Output PNGs to Sprite")]
        public static void BatchConvertMenu()
        {
            int mode = EditorUtility.DisplayDialogComplex(
                "Batch Convert Output PNGs",
                "Output 폴더의 PNG들을 Sprite 임포트 설정으로 변환합니다.\n\n기본: 이미 Sprite인 파일은 스킵\n강제 재적용: 모든 PNG를 재설정",
                "기본 실행",
                "취소",
                "강제 재적용");
            if (mode == 1) return;
            bool force = (mode == 2);
            var (processed, skipped) = ForgeIO.BatchApplyOutputSprites(force);
            EditorUtility.DisplayDialog("Batch Convert 완료",
                $"처리됨: {processed}건\n스킵됨: {skipped}건", "OK");
        }

        // 선택 상태
        List<Sprite> _selected = new List<Sprite>();
        ForgePrefabFactory.PrefabKind _kind = ForgePrefabFactory.PrefabKind.Button;

        // Button 전용
        TMP_FontAsset _font;
        string _buttonText = "Button";
        bool _useFilenameAsText = true;
        float _fontSize = 14f;
        Color _textColor = Color.black;

        // 크기 오버라이드
        bool _useSizeOverride;
        Vector2 _sizeOverride = new Vector2(100, 40);

        // Advanced
        bool _showAdvanced;
        float _pixelsPerUnit = 100f;
        SpriteAlignment _pivot = SpriteAlignment.Center;
        FilterMode _filterMode = FilterMode.Bilinear;
        TextureImporterCompression _compression = TextureImporterCompression.Uncompressed;
        bool _generateMipMaps = false;
        int _maxSize = 2048;
        bool _batchForceReapply = false;

        // Status
        string _statusMsg;
        double _statusExpireAt;
        Vector2 _scroll;

        void OnEnable()
        {
            if (_font == null) _font = ForgePrefabFactory.LoadDefaultFont();
        }

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Prefab Factory", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("출력: " + ForgePrefabFactory.PrefabsDir, EditorStyles.miniLabel);
            EditorGUILayout.Space(6);

            // -------- Batch Convert 섹션 --------
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Output Folder Utilities", EditorStyles.boldLabel);
                _batchForceReapply = EditorGUILayout.Toggle("강제 재적용", _batchForceReapply);
                if (GUILayout.Button("Batch Convert Output PNGs → Sprite", GUILayout.Height(26)))
                {
                    EditorApplication.delayCall += () => RunBatchConvert(_batchForceReapply);
                }
            }

            EditorGUILayout.Space(8);

            // -------- 타입 --------
            _kind = (ForgePrefabFactory.PrefabKind)EditorGUILayout.EnumPopup("Type", _kind);

            EditorGUILayout.Space(6);

            // -------- 파일 선택 --------
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Selected PNGs ({_selected.Count}개)", EditorStyles.boldLabel);
                    if (GUILayout.Button("Select PNGs…", GUILayout.Width(110)))
                    {
                        EditorApplication.delayCall += () =>
                            ForgeSpritePickerWindow.Show(_selected, list =>
                            {
                                _selected = list ?? new List<Sprite>();
                                Repaint();
                            });
                    }
                }
                if (_selected.Count == 0)
                {
                    EditorGUILayout.LabelField("선택된 파일 없음", EditorStyles.miniLabel);
                }
                else
                {
                    for (int i = 0; i < _selected.Count; i++)
                    {
                        var sp = _selected[i];
                        if (sp == null) continue;
                        var r = sp.rect;
                        EditorGUILayout.LabelField($"• {sp.name}  ({(int)r.width}×{(int)r.height})", EditorStyles.miniLabel);
                    }
                }
            }

            EditorGUILayout.Space(6);

            // -------- 크기 --------
            _useSizeOverride = EditorGUILayout.Toggle("크기 수동 지정 (공통)", _useSizeOverride);
            if (_useSizeOverride)
            {
                _sizeOverride = EditorGUILayout.Vector2Field("  Size", _sizeOverride);
            }
            else
            {
                EditorGUILayout.LabelField("  각 스프라이트의 픽셀 크기 사용", EditorStyles.miniLabel);
            }

            // -------- Button 전용 --------
            if (_kind == ForgePrefabFactory.PrefabKind.Button)
            {
                EditorGUILayout.Space(6);
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    EditorGUILayout.LabelField("Button Text", EditorStyles.boldLabel);
                    _font = (TMP_FontAsset)EditorGUILayout.ObjectField("Font", _font, typeof(TMP_FontAsset), false);
                    _useFilenameAsText = EditorGUILayout.Toggle("파일명을 Text로 사용", _useFilenameAsText);
                    using (new EditorGUI.DisabledScope(_useFilenameAsText))
                    {
                        _buttonText = EditorGUILayout.TextField("Text (공통)", _buttonText);
                    }
                    _fontSize = EditorGUILayout.FloatField("Font Size", _fontSize);
                    _textColor = EditorGUILayout.ColorField("Text Color", _textColor);
                }
            }

            EditorGUILayout.Space(6);

            // -------- Advanced --------
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "Advanced (Sprite Import 옵션 — 배치 변환 시 사용)", true);
            if (_showAdvanced)
            {
                EditorGUI.indentLevel++;
                _pixelsPerUnit = EditorGUILayout.FloatField("Pixels Per Unit", _pixelsPerUnit);
                _pivot = (SpriteAlignment)EditorGUILayout.EnumPopup("Pivot", _pivot);
                _filterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode", _filterMode);
                _compression = (TextureImporterCompression)EditorGUILayout.EnumPopup("Compression", _compression);
                _generateMipMaps = EditorGUILayout.Toggle("Generate MipMaps", _generateMipMaps);
                _maxSize = EditorGUILayout.IntPopup("Max Size", _maxSize,
                    new[] { "256", "512", "1024", "2048", "4096", "8192" },
                    new[] { 256, 512, 1024, 2048, 4096, 8192 });
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(12);

            GUI.enabled = _selected.Count > 0;
            if (GUILayout.Button($"Create Prefabs ({_selected.Count})", GUILayout.Height(32)))
            {
                EditorApplication.delayCall += DoCreateBatch;
            }
            GUI.enabled = true;

            if (EditorApplication.timeSinceStartup < _statusExpireAt && !string.IsNullOrEmpty(_statusMsg))
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.HelpBox(_statusMsg, MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        void RunBatchConvert(bool force)
        {
            var (processed, skipped) = ForgeIO.BatchApplyOutputSprites(
                force,
                pixelsPerUnit: _pixelsPerUnit,
                alignment: _pivot,
                filterMode: _filterMode,
                compression: _compression,
                generateMipMaps: _generateMipMaps,
                maxSize: _maxSize);
            ShowStatus($"Batch Convert 완료 — 처리 {processed} / 스킵 {skipped}", false);
        }

        void DoCreateBatch()
        {
            if (_selected.Count == 0) return;

            var opts = new ForgePrefabFactory.Options
            {
                Kind = _kind,
                Font = _font,
                ButtonText = _buttonText,
                FontSize = _fontSize,
                TextColor = _textColor,
                SizeOverride = _useSizeOverride ? (Vector2?)_sizeOverride : null,
            };

            bool useFilenameText = (_kind == ForgePrefabFactory.PrefabKind.Button) && _useFilenameAsText;
            var result = ForgePrefabFactory.CreateBatch(_selected, opts, useFilenameText, out int failCount);

            if (result.Count > 0)
            {
                EditorGUIUtility.PingObject(result[0]);
            }
            ShowStatus($"생성 완료 — 성공 {result.Count} / 실패 {failCount}", false);
            Repaint();
        }

        void ShowStatus(string msg, bool isError)
        {
            _statusMsg = msg;
            _statusExpireAt = EditorApplication.timeSinceStartup + 5.0;
            if (isError) Debug.LogError("[ImageObjectForge] " + msg);
            Repaint();
        }
    }
}
