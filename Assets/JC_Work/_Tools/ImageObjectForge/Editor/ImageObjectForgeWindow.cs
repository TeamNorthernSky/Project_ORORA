using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Orora.ImageObjectForge
{
    public class ImageObjectForgeWindow : EditorWindow
    {
        public enum ToolMode { Brush, Pen }
        public enum BrushMode { Paint, Erase }

        const float SidebarWidth = 220f;
        const float ToolbarHeight = 28f;
        const float StatusHeight = 22f;

        [MenuItem("Tools/JC/Image Object Forge")]
        public static void Open()
        {
            var w = GetWindow<ImageObjectForgeWindow>("Image Object Forge");
            w.minSize = new Vector2(960, 640);
        }

        ForgeDocument _doc;
        ForgeViewport _vp;
        ForgeUndoStack _undo;

        ToolMode _tool = ToolMode.Brush;
        BrushMode _brushMode = BrushMode.Paint;
        float _brushRadius = 12f;

        // Pen 상태
        readonly List<Vector2> _penPts = new List<Vector2>();
        const float PenCloseDistScreen = 10f;

        // 브러시 스트로크
        bool _strokeActive;
        Vector2 _strokeLastImg;
        byte[] _preOpMask;

        // 뷰 내비게이션
        bool _spaceDown;
        bool _panDragging;
        Vector2 _panLastScreen;

        // 마우스 위치(이미지 좌표, y-up). 현재 커서가 캔버스 위에 있을 때만 유효.
        Vector2 _cursorImg;
        bool _cursorInside;

        string _statusMsg;
        double _statusExpireAt;

        void OnEnable()
        {
            _doc = new ForgeDocument();
            _vp = new ForgeViewport();
            _undo = new ForgeUndoStack();
            wantsMouseMove = true;
            wantsMouseEnterLeaveWindow = true;
        }

        void OnDisable()
        {
            _doc?.DisposeInternal();
        }

        void OnGUI()
        {
            var e = Event.current;

            // Space 키 트래킹
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Space)
            { _spaceDown = true; Repaint(); }
            else if (e.type == EventType.KeyUp && e.keyCode == KeyCode.Space)
            { _spaceDown = false; Repaint(); }

            DrawToolbar();

            var body = new Rect(0, ToolbarHeight, position.width, position.height - ToolbarHeight - StatusHeight);
            var sidebar = new Rect(body.x, body.y, SidebarWidth, body.height);
            var canvas = new Rect(body.x + SidebarWidth, body.y, body.width - SidebarWidth, body.height);

            DrawSidebar(sidebar);
            DrawCanvas(canvas);
            DrawStatusBar(new Rect(0, position.height - StatusHeight, position.width, StatusHeight));

            HandleGlobalShortcuts(e);

            if (e.type == EventType.MouseMove) Repaint();
        }

        // -------- Toolbar --------
        void DrawToolbar()
        {
            var rect = new Rect(0, 0, position.width, ToolbarHeight);
            GUI.Box(rect, GUIContent.none, EditorStyles.toolbar);
            GUILayout.BeginArea(rect);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Load…", EditorStyles.toolbarButton, GUILayout.Width(70))) DoLoad();
                GUI.enabled = _doc.HasImage;
                if (GUILayout.Button("Save…", EditorStyles.toolbarButton, GUILayout.Width(70))) DoSave();
                GUI.enabled = true;
                GUILayout.Space(12);
                GUI.enabled = _undo.CanUndo;
                if (GUILayout.Button("Undo", EditorStyles.toolbarButton, GUILayout.Width(60))) DoUndo();
                GUI.enabled = _undo.CanRedo;
                if (GUILayout.Button("Redo", EditorStyles.toolbarButton, GUILayout.Width(60))) DoRedo();
                GUI.enabled = true;
                GUILayout.Space(12);
                GUI.enabled = _doc.HasImage;
                if (GUILayout.Button("Fit", EditorStyles.toolbarButton, GUILayout.Width(50))) _vp.Fit(CanvasRectCached(), _doc.Width, _doc.Height);
                if (GUILayout.Button("Clear Mask", EditorStyles.toolbarButton, GUILayout.Width(90))) DoClearMask();
                GUI.enabled = true;
                GUILayout.FlexibleSpace();
                GUILayout.Label(_doc.HasImage ? $"{_doc.Width} × {_doc.Height}" : "(no image)", EditorStyles.miniLabel);
            }
            GUILayout.EndArea();
        }

        Rect _cachedCanvasRect;
        Rect CanvasRectCached() => _cachedCanvasRect;

        // -------- Sidebar --------
        void DrawSidebar(Rect r)
        {
            GUI.Box(r, GUIContent.none);
            GUILayout.BeginArea(new Rect(r.x + 8, r.y + 8, r.width - 16, r.height - 16));

            EditorGUILayout.LabelField("Tool", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (ToggleButton(_tool == ToolMode.Brush, "Brush (B)")) _tool = ToolMode.Brush;
                if (ToggleButton(_tool == ToolMode.Pen, "Pen (P)")) _tool = ToolMode.Pen;
            }

            EditorGUILayout.Space(8);

            if (_tool == ToolMode.Brush)
            {
                EditorGUILayout.LabelField("Brush", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (ToggleButton(_brushMode == BrushMode.Paint, "Paint")) _brushMode = BrushMode.Paint;
                    if (ToggleButton(_brushMode == BrushMode.Erase, "Erase (E)")) _brushMode = BrushMode.Erase;
                }
                EditorGUILayout.LabelField("Size", Mathf.RoundToInt(_brushRadius * 2).ToString() + " px");
                _brushRadius = EditorGUILayout.Slider(_brushRadius, 1f, 256f);
                EditorGUILayout.LabelField("[ ]  키: 크기 조절", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("Pen", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Vertices: {_penPts.Count}");
                GUI.enabled = _penPts.Count >= 3;
                if (GUILayout.Button("Add (Enter)", GUILayout.Height(24))) CommitPen(true);
                if (GUILayout.Button("Subtract (Shift+Enter)", GUILayout.Height(24))) CommitPen(false);
                GUI.enabled = _penPts.Count > 0;
                if (GUILayout.Button("Cancel (Esc)", GUILayout.Height(20))) _penPts.Clear();
                GUI.enabled = true;
                EditorGUILayout.LabelField("좌클릭: 버텍스 추가", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("시작점 근처 클릭 = 닫고 Add", EditorStyles.miniLabel);
                EditorGUILayout.LabelField("Backspace: 직전 버텍스 제거", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("View", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Zoom", (_vp.Zoom * 100f).ToString("0.#") + " %");
            EditorGUILayout.LabelField("휠: 줌, 중클릭/Space+드래그: 팬", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("F: 뷰 피팅", EditorStyles.miniLabel);

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Shortcuts", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Ctrl+Z / Ctrl+Y : Undo/Redo", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Ctrl+O / Ctrl+S : Load/Save", EditorStyles.miniLabel);

            GUILayout.EndArea();
        }

        static bool ToggleButton(bool active, string label)
        {
            var style = new GUIStyle(GUI.skin.button);
            if (active) style.normal.background = style.active.background;
            return GUILayout.Button(label, active ? EditorStyles.miniButtonMid : EditorStyles.miniButton, GUILayout.Height(22));
        }

        // -------- Canvas --------
        void DrawCanvas(Rect canvasRect)
        {
            _cachedCanvasRect = canvasRect;
            EditorGUI.DrawRect(canvasRect, new Color(0.18f, 0.18f, 0.18f));

            if (!_doc.HasImage)
            {
                var center = new Rect(canvasRect.x, canvasRect.y + canvasRect.height * 0.5f - 10, canvasRect.width, 20);
                var style = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
                GUI.Label(center, "Load 버튼으로 이미지 로드", style);
                return;
            }

            var imgRect = _vp.GetImageDisplayRect(canvasRect, _doc.Width, _doc.Height);

            // 클리핑을 위해 BeginGroup 사용
            GUI.BeginClip(canvasRect);
            var localImgRect = new Rect(imgRect.x - canvasRect.x, imgRect.y - canvasRect.y, imgRect.width, imgRect.height);

            // 체커보드 배경
            DrawCheckerboard(localImgRect);

            GUI.DrawTexture(localImgRect, _doc.Source, ScaleMode.StretchToFill, true);
            GUI.DrawTexture(localImgRect, _doc.MaskOverlay, ScaleMode.StretchToFill, true);

            // 이미지 테두리
            ForgeGfx.DrawRectOutline(localImgRect, new Color(1f, 1f, 1f, 0.25f), 1f);

            // Pen 미리보기 (local 좌표 기준)
            if (_tool == ToolMode.Pen && _penPts.Count > 0)
            {
                DrawPenPreviewClipped(canvasRect);
            }

            // 브러시 커서
            if (_tool == ToolMode.Brush && _cursorInside && !_spaceDown && !_panDragging)
            {
                var sp = _vp.ImageToScreen(canvasRect, _cursorImg, _doc.Height);
                var local = new Vector2(sp.x - canvasRect.x, sp.y - canvasRect.y);
                Color c = _brushMode == BrushMode.Paint
                    ? new Color(1f, 0.6f, 0.3f, 0.9f)
                    : new Color(0.4f, 0.7f, 1f, 0.9f);
                ForgeGfx.DrawCircle(local, _brushRadius * _vp.Zoom, c, 1.5f, 36);
            }

            GUI.EndClip();

            HandleCanvasEvents(canvasRect);
        }

        void DrawPenPreviewClipped(Rect canvasRect)
        {
            var col = new Color(1f, 0.7f, 0.2f, 1f);
            var colDim = new Color(1f, 0.7f, 0.2f, 0.5f);

            for (int i = 0; i < _penPts.Count - 1; i++)
            {
                var a = _vp.ImageToScreen(canvasRect, _penPts[i], _doc.Height);
                var b = _vp.ImageToScreen(canvasRect, _penPts[i + 1], _doc.Height);
                ForgeGfx.DrawLine(a - canvasRect.position, b - canvasRect.position, col, 2f);
            }

            if (_cursorInside && _penPts.Count >= 1)
            {
                var last = _vp.ImageToScreen(canvasRect, _penPts[_penPts.Count - 1], _doc.Height);
                var cur = _vp.ImageToScreen(canvasRect, _cursorImg, _doc.Height);
                ForgeGfx.DrawLine(last - canvasRect.position, cur - canvasRect.position, colDim, 1.5f);

                // 닫기 가능 상태면 시작점까지 점선 대신 dim 라인
                if (_penPts.Count >= 3)
                {
                    var first = _vp.ImageToScreen(canvasRect, _penPts[0], _doc.Height);
                    bool canClose = Vector2.Distance(first, cur) <= PenCloseDistScreen;
                    var closeCol = canClose ? new Color(0.4f, 1f, 0.4f, 1f) : colDim;
                    ForgeGfx.DrawLine(cur - canvasRect.position, first - canvasRect.position, closeCol, canClose ? 2f : 1f);
                }
            }

            for (int i = 0; i < _penPts.Count; i++)
            {
                var sp = _vp.ImageToScreen(canvasRect, _penPts[i], _doc.Height);
                var local = sp - canvasRect.position;
                bool isFirst = (i == 0);
                var dotCol = isFirst ? new Color(0.4f, 1f, 0.4f, 1f) : col;
                ForgeGfx.FilledRect(new Rect(local.x - 3, local.y - 3, 6, 6), dotCol);
            }
        }

        void HandleCanvasEvents(Rect canvasRect)
        {
            var e = Event.current;
            bool over = canvasRect.Contains(e.mousePosition);

            // 마우스 위치 갱신
            if (over && _doc.HasImage)
            {
                _cursorInside = true;
                _cursorImg = _vp.ScreenToImage(canvasRect, e.mousePosition, _doc.Height);
            }
            else
            {
                _cursorInside = false;
            }

            if (!_doc.HasImage) return;

            // Scroll Wheel: Zoom
            if (e.type == EventType.ScrollWheel && over)
            {
                _vp.ZoomAt(canvasRect, e.mousePosition, e.delta.y, _doc.Height);
                e.Use();
                Repaint();
                return;
            }

            // 팬 시작/진행/종료
            bool panInit = over && e.type == EventType.MouseDown && (e.button == 2 || (e.button == 0 && _spaceDown));
            if (panInit)
            {
                _panDragging = true;
                _panLastScreen = e.mousePosition;
                e.Use();
                return;
            }
            if (_panDragging)
            {
                if (e.type == EventType.MouseDrag)
                {
                    _vp.PanBy(e.mousePosition - _panLastScreen);
                    _panLastScreen = e.mousePosition;
                    e.Use();
                    Repaint();
                    return;
                }
                if (e.type == EventType.MouseUp)
                {
                    _panDragging = false;
                    e.Use();
                    Repaint();
                    return;
                }
            }

            // 브러시
            if (_tool == ToolMode.Brush && over && !_spaceDown)
            {
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    StartBrushStroke();
                    var imgPt = _vp.ScreenToImage(canvasRect, e.mousePosition, _doc.Height);
                    _strokeLastImg = imgPt;
                    bool paint = _brushMode == BrushMode.Paint;
                    if (ForgeBrush.StampDisk(_doc.Mask, _doc.Width, _doc.Height, imgPt, _brushRadius, paint, out var d))
                        _doc.RebuildOverlayRect(d);
                    else
                        _doc.RebuildOverlayRect(d);
                    e.Use();
                    Repaint();
                    return;
                }
                if (_strokeActive && e.type == EventType.MouseDrag && e.button == 0)
                {
                    var imgPt = _vp.ScreenToImage(canvasRect, e.mousePosition, _doc.Height);
                    bool paint = _brushMode == BrushMode.Paint;
                    if (ForgeBrush.StrokeLine(_doc.Mask, _doc.Width, _doc.Height, _strokeLastImg, imgPt, _brushRadius, paint, out var d))
                        _doc.RebuildOverlayRect(d);
                    else
                        _doc.RebuildOverlayRect(d);
                    _strokeLastImg = imgPt;
                    e.Use();
                    Repaint();
                    return;
                }
                if (_strokeActive && e.type == EventType.MouseUp && e.button == 0)
                {
                    EndBrushStroke();
                    e.Use();
                    Repaint();
                    return;
                }
            }

            // 펜툴
            if (_tool == ToolMode.Pen && over && !_spaceDown)
            {
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    var imgPt = _vp.ScreenToImage(canvasRect, e.mousePosition, _doc.Height);

                    // 시작점 근처 클릭 → 닫고 Add
                    if (_penPts.Count >= 3)
                    {
                        var first = _vp.ImageToScreen(canvasRect, _penPts[0], _doc.Height);
                        if (Vector2.Distance(first, e.mousePosition) <= PenCloseDistScreen)
                        {
                            CommitPen(true);
                            e.Use();
                            Repaint();
                            return;
                        }
                    }

                    _penPts.Add(imgPt);
                    e.Use();
                    Repaint();
                    return;
                }
                if (e.type == EventType.MouseMove)
                {
                    Repaint();
                }
            }
        }

        void StartBrushStroke()
        {
            _strokeActive = true;
            _preOpMask = (byte[])_doc.Mask.Clone();
        }

        void EndBrushStroke()
        {
            if (!_strokeActive) return;
            _strokeActive = false;
            if (_preOpMask != null)
            {
                _undo.Push(_preOpMask, _doc.Mask, _doc.Width, _doc.Height);
                _preOpMask = null;
            }
        }

        void CommitPen(bool add)
        {
            if (_penPts.Count < 3) return;
            var pre = (byte[])_doc.Mask.Clone();
            var dirty = ForgePolygon.FillToMask(_doc.Mask, _doc.Width, _doc.Height, _penPts, add);
            if (dirty.width > 0 && dirty.height > 0)
            {
                _undo.PushHinted(pre, _doc.Mask, _doc.Width, dirty);
                _doc.RebuildOverlayRect(dirty);
            }
            _penPts.Clear();
            SetStatus(add ? "폴리곤 Add 완료" : "폴리곤 Subtract 완료");
            Repaint();
        }

        // -------- StatusBar --------
        void DrawStatusBar(Rect r)
        {
            GUI.Box(r, GUIContent.none, EditorStyles.toolbar);
            GUILayout.BeginArea(r);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (_doc.HasImage)
                {
                    string cur = _cursorInside
                        ? $"({Mathf.FloorToInt(_cursorImg.x)}, {Mathf.FloorToInt(_cursorImg.y)})"
                        : "-";
                    GUILayout.Label($"cursor: {cur}   zoom: {_vp.Zoom * 100f:0}%   tool: {_tool}"
                                    + (_tool == ToolMode.Brush ? $"/{_brushMode}" : "")
                                    + $"   undo: {_undo.UndoCount}/{ForgeUndoStack.MaxEntries}", EditorStyles.miniLabel);
                }
                GUILayout.FlexibleSpace();
                if (EditorApplication.timeSinceStartup < _statusExpireAt && !string.IsNullOrEmpty(_statusMsg))
                    GUILayout.Label(_statusMsg, EditorStyles.miniLabel);
            }
            GUILayout.EndArea();
        }

        // -------- Shortcuts --------
        void HandleGlobalShortcuts(Event e)
        {
            if (e.type != EventType.KeyDown) return;

            // Ctrl 조합
            if (e.control || e.command)
            {
                if (e.keyCode == KeyCode.Z) { DoUndo(); e.Use(); Repaint(); return; }
                if (e.keyCode == KeyCode.Y) { DoRedo(); e.Use(); Repaint(); return; }
                if (e.keyCode == KeyCode.O) { DoLoad(); e.Use(); return; }
                if (e.keyCode == KeyCode.S) { DoSave(); e.Use(); return; }
            }

            if (e.keyCode == KeyCode.B) { _tool = ToolMode.Brush; e.Use(); Repaint(); return; }
            if (e.keyCode == KeyCode.P) { _tool = ToolMode.Pen; e.Use(); Repaint(); return; }
            if (e.keyCode == KeyCode.F && _doc.HasImage)
            {
                _vp.Fit(_cachedCanvasRect, _doc.Width, _doc.Height); e.Use(); Repaint(); return;
            }
            if (e.keyCode == KeyCode.E && _tool == ToolMode.Brush)
            {
                _brushMode = _brushMode == BrushMode.Paint ? BrushMode.Erase : BrushMode.Paint;
                e.Use(); Repaint(); return;
            }
            if (e.keyCode == KeyCode.LeftBracket) { _brushRadius = Mathf.Max(1f, _brushRadius - 2f); e.Use(); Repaint(); return; }
            if (e.keyCode == KeyCode.RightBracket) { _brushRadius = Mathf.Min(256f, _brushRadius + 2f); e.Use(); Repaint(); return; }

            if (_tool == ToolMode.Pen)
            {
                if (e.keyCode == KeyCode.Escape) { _penPts.Clear(); e.Use(); Repaint(); return; }
                if (e.keyCode == KeyCode.Backspace && _penPts.Count > 0)
                { _penPts.RemoveAt(_penPts.Count - 1); e.Use(); Repaint(); return; }
                if ((e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) && _penPts.Count >= 3)
                {
                    CommitPen(!e.shift);
                    e.Use(); Repaint(); return;
                }
            }
        }

        // -------- Actions --------
        void DoLoad()
        {
            if (ForgeIO.LoadImageFromDialog(out var tex, out var assetPath, out var err))
            {
                _doc.SetSource(tex, assetPath);
                _undo.Clear();
                _penPts.Clear();
                _vp.Fit(_cachedCanvasRect.width > 0 ? _cachedCanvasRect : new Rect(0, 0, position.width - SidebarWidth, position.height - ToolbarHeight - StatusHeight),
                        _doc.Width, _doc.Height);
                SetStatus($"로드 완료: {assetPath}");
                Repaint();
            }
            else if (!string.IsNullOrEmpty(err) && err != "취소됨")
            {
                EditorUtility.DisplayDialog("Load 실패", err, "OK");
            }
        }

        void DoSave()
        {
            if (!_doc.HasImage) return;
            var bounds = ForgeIO.ComputeMaskBounds(_doc.Mask, _doc.Width, _doc.Height);
            if (bounds.width <= 0 || bounds.height <= 0)
            {
                EditorUtility.DisplayDialog("Save 불가", "선택된 영역이 없습니다.", "OK");
                return;
            }
            var preview = ForgeIO.BuildCropped(_doc.Source, _doc.Mask, bounds);
            string defaultName = ForgeIO.NextAvailableOutputName(_doc.SourceStem);
            ForgeSavePreviewWindow.Show(preview, bounds, defaultName, filename =>
            {
                // preview 텍스처 재생성 후 저장(preview는 SavePreviewWindow가 Dispose할 것이므로 별도 텍스처 생성)
                var outTex = ForgeIO.BuildCropped(_doc.Source, _doc.Mask, bounds);
                string path = ForgeIO.SavePng(outTex, filename, out var err);
                Object.DestroyImmediate(outTex);
                if (path != null)
                {
                    SetStatus($"저장: {path}");
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                    if (obj != null) EditorGUIUtility.PingObject(obj);
                }
                else
                {
                    EditorUtility.DisplayDialog("Save 실패", err ?? "unknown", "OK");
                }
                Repaint();
            });
        }

        void DoUndo()
        {
            var r = _undo.Undo(_doc.Mask, _doc.Width);
            if (r.width > 0) _doc.RebuildOverlayRect(r);
        }

        void DoRedo()
        {
            var r = _undo.Redo(_doc.Mask, _doc.Width);
            if (r.width > 0) _doc.RebuildOverlayRect(r);
        }

        void DoClearMask()
        {
            if (!_doc.HasImage) return;
            if (!EditorUtility.DisplayDialog("Clear Mask", "현재 마스크를 모두 지울까요? Undo로 복구할 수 있습니다.", "Clear", "Cancel")) return;
            var pre = (byte[])_doc.Mask.Clone();
            System.Array.Clear(_doc.Mask, 0, _doc.Mask.Length);
            _undo.Push(pre, _doc.Mask, _doc.Width, _doc.Height);
            _doc.RebuildOverlayAll();
            Repaint();
        }

        void SetStatus(string msg)
        {
            _statusMsg = msg;
            _statusExpireAt = EditorApplication.timeSinceStartup + 3.0;
        }

        // -------- Checkerboard 배경 --------
        static void DrawCheckerboard(Rect rect)
        {
            const int CellSize = 16;
            int cols = Mathf.CeilToInt(rect.width / CellSize) + 1;
            int rows = Mathf.CeilToInt(rect.height / CellSize) + 1;
            var c1 = new Color(0.35f, 0.35f, 0.35f);
            var c2 = new Color(0.25f, 0.25f, 0.25f);
            GUI.BeginClip(rect);
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    var col = ((x + y) & 1) == 0 ? c1 : c2;
                    ForgeGfx.FilledRect(new Rect(x * CellSize, y * CellSize, CellSize, CellSize), col);
                }
            }
            GUI.EndClip();
        }
    }
}
