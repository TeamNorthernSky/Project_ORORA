using UnityEngine;

namespace Orora.TestMerge
{
    /// <summary>
    /// DH QuarterViewCameraFollower의 TM 버전. 카메라에 부착.
    /// 쿼터뷰 오프셋 유지 + 마우스 휠 줌 + 에지 스크롤 + Y키 리센터.
    /// </summary>
    [DisallowMultipleComponent]
    public class TMQuarterViewCameraFollower : MonoBehaviour
    {
        [Header("Follow")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private bool followEnabled = true;
        [SerializeField] private float followDelay = 0.5f;

        [Header("Offsets")]
        [SerializeField] private Vector3 positionOffset = new Vector3(0f, 12f, -8.6f);

        [Header("Smoothing")]
        [SerializeField] private float rotationLerp = 10f;

        [Header("Fixed Rotation")]
        [SerializeField] private Vector3 fixedEulerAngles = new Vector3(55f, 0f, 0f);

        [Header("Zoom")]
        [SerializeField] private float minZoomY = 5f;
        [SerializeField] private float maxZoomY = 30f;
        [SerializeField] private float zoomSpeed = 200f;

        [Header("Edge Scrolling (JC 기준)")]
        [SerializeField] private bool edgeScrollEnabled = true;
        [Tooltip("화면 가장자리로부터 스크롤이 시작되는 픽셀 영역(각 축). JC 기본 120.")]
        [SerializeField] private float edgeThreshold = 120f;
        [Tooltip("가장자리 근처 진입 시 최저 스크롤 속도. JC 기본 5.")]
        [SerializeField] private float minScrollSpeed = 5f;
        [Tooltip("가장자리 완전 도달 시 최고 스크롤 속도. JC 기본 30.")]
        [SerializeField] private float maxScrollSpeed = 30f;
        [SerializeField] private float edgeAcceleration = 10f;
        [SerializeField] private float edgeLimitRange = 50f;
        [SerializeField] private bool invertVerticalEdgeScroll = false;

        [Header("UI Margin (우측 UI 패널)")]
        [Tooltip("우측 UI 패널 너비(px). 카메라 viewport와 에지 스크롤 판정 영역 모두에 반영.")]
        [SerializeField] private float rightUiMargin = 160f;

        [Header("Edge Arrow Cursor (JC 스타일)")]
        [Tooltip("마우스 화면 경계에 가까워지면 화살표 커서를 표시. JC MapCameraController와 동일 절차적 생성.")]
        [SerializeField] private bool showEdgeArrow = true;
        [Tooltip("Inspector에 직접 할당하지 않으면 Awake에서 자동 생성.")]
        [SerializeField] private Texture2D[] arrowCursors = new Texture2D[8];
        [SerializeField] private int arrowCursorSize = 32;

        [Header("Reset")]
        [SerializeField] private KeyCode resetKey = KeyCode.Y;

        private Vector3 followVelocity;
        private Vector3 panOffset;
        private Vector3 edgeScrollVelocity;
        private float defaultZoomY;
        private float defaultZoomZ;
        private bool cursorOverridden;
        private int lastCursorIndex = -1;
        private Camera cachedCamera;

        public void SetFollowTarget(Transform target) { followTarget = target; }
        public void SetFollowEnabled(bool enabled) { followEnabled = enabled; }

        public void RecenterOnFollowTarget()
        {
            panOffset = Vector3.zero;
            edgeScrollVelocity = Vector3.zero;
        }

        private void Awake()
        {
            positionOffset.x = 0f;
            defaultZoomY = positionOffset.y;
            defaultZoomZ = positionOffset.z;
            cachedCamera = GetComponent<Camera>();
            EnsureArrowCursors();
            ApplyCameraViewport();
        }

        /// <summary>카메라 viewport를 좌측 UI 패널만큼 제외하고 세팅. Screen 크기 변경 시에도 반영 필요.</summary>
        private void ApplyCameraViewport()
        {
            if (cachedCamera == null) cachedCamera = GetComponent<Camera>();
            if (cachedCamera == null) return;
            float w = Screen.width;
            if (w <= 0f) return;
            float widthNorm = Mathf.Clamp01(1f - rightUiMargin / w);
            cachedCamera.rect = new Rect(0f, 0f, widthNorm, 1f);
        }

        private void OnDestroy()
        {
            if (cursorOverridden)
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        private void Update()
        {
            ApplyCameraViewport();
            HandleZoomInput();
            HandleEdgeScrolling();
            HandleEdgeArrowCursor();
            HandleResetInput();
        }

        private void LateUpdate()
        {
            if (!followEnabled || followTarget == null) return;

            Vector3 anchor = followTarget.position;
            Vector3 desired = anchor + panOffset + positionOffset;
            desired.x = anchor.x + panOffset.x;
            desired.y = positionOffset.y;
            desired.z = anchor.z + panOffset.z + positionOffset.z;

            float smoothTime = Mathf.Max(0.01f, followDelay);
            float tRot = 1f - Mathf.Exp(-rotationLerp * Time.deltaTime);

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref followVelocity, smoothTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(fixedEulerAngles), tRot);
        }

        private void HandleZoomInput()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(scroll, 0f)) return;

            float oldY = Mathf.Max(0.0001f, positionOffset.y);
            float newY = Mathf.Clamp(oldY - scroll * zoomSpeed * Time.deltaTime, minZoomY, maxZoomY);
            float scale = newY / oldY;

            // y와 z를 같은 비율로 스케일 → 광축 각도(피치 55°) 유지 → 초점이 항상 플레이어 위치
            positionOffset.y = newY;
            positionOffset.z *= scale;
        }

        private void HandleEdgeScrolling()
        {
            // 마우스가 화면 밖이거나 스크롤 비활성 → 즉시 정지 (관성 없음).
            // 카메라의 follow 기능은 LateUpdate에서 별개로 계속 동작.
            if (!edgeScrollEnabled || !IsMouseInsideScreen())
            {
                edgeScrollVelocity = Vector3.zero;
                return;
            }

            Vector3 mp = Input.mousePosition;
            Vector2 input = new Vector2(
                EvaluateEdgeInputX(mp.x),
                EvaluateEdgeInput(mp.y, Screen.height));
            if (invertVerticalEdgeScroll) input.y *= -1f;

            Vector3 right = transform.right; right.y = 0f; right.Normalize();
            Vector3 forward = transform.forward; forward.y = 0f; forward.Normalize();

            Vector3 desired = (right * input.x) + (forward * input.y);
            if (desired.sqrMagnitude > 1f) desired.Normalize();

            float intensity = Mathf.Max(Mathf.Abs(input.x), Mathf.Abs(input.y));
            float speed = Mathf.Lerp(minScrollSpeed, maxScrollSpeed, intensity);
            desired *= speed;

            float blend = 1f - Mathf.Exp(-Mathf.Max(0.01f, edgeAcceleration) * Time.deltaTime);
            edgeScrollVelocity = Vector3.Lerp(edgeScrollVelocity, desired, blend);
            edgeScrollVelocity.y = 0f;

            panOffset += edgeScrollVelocity * Time.deltaTime;
            panOffset.x = Mathf.Clamp(panOffset.x, -edgeLimitRange, edgeLimitRange);
            panOffset.z = Mathf.Clamp(panOffset.z, -edgeLimitRange, edgeLimitRange);
            panOffset.y = 0f;
        }

        private void HandleEdgeArrowCursor()
        {
            if (!showEdgeArrow || !edgeScrollEnabled || !IsMouseInsideScreen())
            {
                RestoreDefaultCursor();
                return;
            }

            Vector3 mp = Input.mousePosition;
            float x = EvaluateEdgeInputX(mp.x);
            float y = EvaluateEdgeInput(mp.y, Screen.height);

            int dir = DirectionIndex(x, y);
            if (dir < 0 || arrowCursors == null || dir >= arrowCursors.Length || arrowCursors[dir] == null)
            {
                RestoreDefaultCursor();
                return;
            }

            if (dir != lastCursorIndex)
            {
                float half = arrowCursorSize * 0.5f;
                Cursor.SetCursor(arrowCursors[dir], new Vector2(half, half), CursorMode.Auto);
                cursorOverridden = true;
                lastCursorIndex = dir;
            }
        }

        private void RestoreDefaultCursor()
        {
            if (!cursorOverridden) return;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            cursorOverridden = false;
            lastCursorIndex = -1;
        }

        /// <summary>0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW (JC 규약과 동일).</summary>
        private static int DirectionIndex(float x, float y)
        {
            bool hasX = !Mathf.Approximately(x, 0f);
            bool hasY = !Mathf.Approximately(y, 0f);
            if (hasY && !hasX) return y > 0 ? 0 : 4;
            if (hasX && !hasY) return x > 0 ? 2 : 6;
            if (x > 0 && y > 0) return 1;
            if (x > 0 && y < 0) return 3;
            if (x < 0 && y < 0) return 5;
            if (x < 0 && y > 0) return 7;
            return -1;
        }

        // ---------------------------------------------------------------------
        // Arrow Cursor 자동 생성 (JC MapCameraController 포팅)
        // ---------------------------------------------------------------------

        private void EnsureArrowCursors()
        {
            if (arrowCursors == null || arrowCursors.Length != 8)
                arrowCursors = new Texture2D[8];

            Vector2[] directions =
            {
                new Vector2(0, 1),
                new Vector2(1, 1).normalized,
                new Vector2(1, 0),
                new Vector2(1, -1).normalized,
                new Vector2(0, -1),
                new Vector2(-1, -1).normalized,
                new Vector2(-1, 0),
                new Vector2(-1, 1).normalized,
            };
            for (int i = 0; i < 8; i++)
            {
                if (arrowCursors[i] == null)
                    arrowCursors[i] = CreateArrowTexture(directions[i], arrowCursorSize);
            }
        }

        private static Texture2D CreateArrowTexture(Vector2 dir, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color[size * size];
            var clear = new Color(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            var arrowColor = new Color(1f, 1f, 1f, 1f);
            var outlineColor = new Color(0.1f, 0.1f, 0.1f, 1f);

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Vector2 tip = center + dir * 13f;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            Vector2 tailEnd = center - dir * 8f;

            DrawLineThick(pixels, size, center - dir * 1f, tailEnd, 1.5f, arrowColor);

            Vector2 headBase = center + dir * 4f;
            Vector2 wing1 = headBase + perp * 6f;
            Vector2 wing2 = headBase - perp * 6f;

            DrawLineThick(pixels, size, tip, wing1, 1.5f, arrowColor);
            DrawLineThick(pixels, size, tip, wing2, 1.5f, arrowColor);
            DrawLineThick(pixels, size, wing1, wing2, 1.2f, arrowColor);
            FillTriangle(pixels, size, tip, wing1, wing2, arrowColor);

            var outlined = new Color[pixels.Length];
            System.Array.Copy(pixels, outlined, pixels.Length);
            for (int y = 1; y < size - 1; y++)
            {
                for (int x = 1; x < size - 1; x++)
                {
                    if (pixels[y * size + x].a >= 0.5f) continue;
                    bool near = false;
                    for (int dy = -1; dy <= 1 && !near; dy++)
                        for (int dx = -1; dx <= 1 && !near; dx++)
                            if (pixels[(y + dy) * size + (x + dx)].a > 0.5f) near = true;
                    if (near) outlined[y * size + x] = outlineColor;
                }
            }

            tex.SetPixels(outlined);
            tex.Apply();
            return tex;
        }

        private static void DrawLineThick(Color[] pixels, int size, Vector2 a, Vector2 b, float thickness, Color col)
        {
            float dist = Vector2.Distance(a, b);
            int steps = Mathf.CeilToInt(dist * 2);
            int radius = Mathf.CeilToInt(thickness);
            float t2 = thickness * thickness;
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, steps == 0 ? 0f : (float)i / steps);
                int baseX = Mathf.RoundToInt(p.x);
                int baseY = Mathf.RoundToInt(p.y);
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (dx * dx + dy * dy > t2) continue;
                        int px = baseX + dx, py = baseY + dy;
                        if (px < 0 || px >= size || py < 0 || py >= size) continue;
                        pixels[py * size + px] = col;
                    }
                }
            }
        }

        private static void FillTriangle(Color[] pixels, int size, Vector2 v0, Vector2 v1, Vector2 v2, Color col)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(v0.x, Mathf.Min(v1.x, v2.x))));
            int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(v0.x, Mathf.Max(v1.x, v2.x))));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(v0.y, Mathf.Min(v1.y, v2.y))));
            int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(v0.y, Mathf.Max(v1.y, v2.y))));
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    if (PointInTriangle(p, v0, v1, v2)) pixels[y * size + x] = col;
                }
            }
        }

        private static bool PointInTriangle(Vector2 p, Vector2 v0, Vector2 v1, Vector2 v2)
        {
            float d1 = Sign(p, v0, v1);
            float d2 = Sign(p, v1, v2);
            float d3 = Sign(p, v2, v0);
            bool hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
            bool hasPos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(hasNeg && hasPos);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
            => (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);

        private void HandleResetInput()
        {
            if (!Input.GetKeyDown(resetKey)) return;
            RecenterOnFollowTarget();
            positionOffset = new Vector3(0f, defaultZoomY, defaultZoomZ);
        }

        private float EvaluateEdgeInput(float mouseAxis, float screenSize)
        {
            float safe = Mathf.Max(1f, edgeThreshold);
            if (mouseAxis <= safe) return -Mathf.Clamp01((safe - mouseAxis) / safe);
            if (mouseAxis >= screenSize - safe) return Mathf.Clamp01((mouseAxis - (screenSize - safe)) / safe);
            return 0f;
        }

        /// <summary>X축 전용: 우측 UI 패널 너머부터를 게임 viewport로 간주 (viewport x ∈ [0, Screen.width - rightUiMargin]).</summary>
        private float EvaluateEdgeInputX(float mouseX)
        {
            float safe = Mathf.Max(1f, edgeThreshold);
            float vpMin = 0f;
            float vpMax = Screen.width - rightUiMargin;
            if (mouseX <= vpMin + safe) return -Mathf.Clamp01((vpMin + safe - mouseX) / safe);
            if (mouseX >= vpMax - safe) return Mathf.Clamp01((mouseX - (vpMax - safe)) / safe);
            return 0f;
        }

        private bool IsMouseInsideScreen()
        {
            var mp = Input.mousePosition;
            float vpMax = Screen.width - rightUiMargin;
            return mp.x >= 0f && mp.x <= vpMax && mp.y >= 0f && mp.y <= Screen.height;
        }
    }
}
