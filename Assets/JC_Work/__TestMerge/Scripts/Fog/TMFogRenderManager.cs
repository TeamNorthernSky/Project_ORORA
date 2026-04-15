using UnityEngine;

namespace Orora.TestMerge
{
    /// <summary>
    /// 안개 렌더링 매니저.
    /// - TMGridManager의 시야 상태 + TMFogFadeAnimator의 intensity를 Texture2D에 반영
    /// - Shader global로 _VisibilityCurrentTex (R8), _VisibilityExploredTex (RGBAFloat) 세팅
    /// - 기존 JC URP Render Feature (FogOfWarJC, FogStencilPrepass, FogHidableClipPass) 들이 이 글로벌을 샘플링
    ///
    /// 본 매니저는 JC PlayFogManager의 3-레이어 시간감쇠 로직을 대체하여,
    /// 턴 기반 1.5s 페이드 (TMFogFadeAnimator) 모델을 따른다.
    /// explored 채널은 "한 번이라도 탐색된 셀"을 모든 레이어에 균일하게 마킹.
    /// </summary>
    public class TMFogRenderManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMGridManager gridManager;
        [SerializeField] private TMFogFadeAnimator fadeAnimator;
        [SerializeField] private TMFogRevealerRegistry revealerRegistry;
        [SerializeField] private TMTurnManager turnManager;

        [Header("Fog Toggle")]
        [SerializeField] private bool fogEnabled = true;

        private Texture2D currentTex;   // R8
        private Texture2D exploredTex;  // RGBA32 (4 byte per pixel)

        private byte[] currentBuffer;
        private byte[] exploredBuffer;

        private int width;
        private int height;
        private bool initialized;

        private bool revealDirty;
        private bool exploredDirty;

        private static readonly int CurrentTexGlobalId = Shader.PropertyToID("_VisibilityCurrentTex");
        private static readonly int ExploredTexGlobalId = Shader.PropertyToID("_VisibilityExploredTex");

        public bool FogEnabled
        {
            get => fogEnabled;
            set => fogEnabled = value;
        }

        public void Initialize()
        {
            if (gridManager == null)
            {
                Debug.LogError("[TMFogRenderManager] gridManager 참조 누락");
                return;
            }

            width = gridManager.Width;
            height = gridManager.Height;

            if (width <= 0 || height <= 0)
            {
                Debug.LogError($"[TMFogRenderManager] 잘못된 그리드 크기 {width}x{height}");
                return;
            }

            currentTex = new Texture2D(width, height, TextureFormat.R8, false, true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "TMFog_Current",
            };
            exploredTex = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "TMFog_Explored",
            };

            currentBuffer = new byte[width * height];
            exploredBuffer = new byte[width * height * 4];

            if (fadeAnimator != null && !fadeAnimator.IsInitialized)
                fadeAnimator.Initialize(width, height);

            UploadInitial();

            Shader.SetGlobalTexture(CurrentTexGlobalId, currentTex);
            Shader.SetGlobalTexture(ExploredTexGlobalId, exploredTex);

            if (gridManager != null)
                gridManager.CellVisibilityChanged += HandleCellVisibilityChanged;

            if (revealerRegistry != null)
                revealerRegistry.AnyRevealerChanged += HandleRevealerChanged;

            initialized = true;
            Debug.Log($"[TMFogRenderManager] 초기화 완료 ({width}x{height})");
        }

        private void OnDestroy()
        {
            if (gridManager != null)
                gridManager.CellVisibilityChanged -= HandleCellVisibilityChanged;

            if (revealerRegistry != null)
                revealerRegistry.AnyRevealerChanged -= HandleRevealerChanged;

            if (currentTex != null) Destroy(currentTex);
            if (exploredTex != null) Destroy(exploredTex);
        }

        private void HandleCellVisibilityChanged(Vector2Int grid)
        {
            revealDirty = true;
            exploredDirty = true;
        }

        /// <summary>
        /// Revealer(파티 등)가 위치 변경 시 호출. 이동 중 매 셀 진입마다 트리거되어 실시간 reveal 수행.
        /// Refog는 별개(턴 기반) — TMTurnFogController에서 관리.
        /// </summary>
        private void HandleRevealerChanged()
        {
            if (!initialized) return;
            RevealAll();
        }

        /// <summary>
        /// Revealer 목록으로부터 RevealArea 호출 + 그리드 visibility 갱신.
        /// TMPartyFogRevealer 등이 위치 변경 시 RevealAll()을 명시적으로 호출.
        /// </summary>
        public void RevealAll()
        {
            if (gridManager == null || revealerRegistry == null) return;
            int day = turnManager != null ? turnManager.CurrentDay : 1;

            var revealers = revealerRegistry.Revealers;
            for (int i = 0; i < revealers.Count; i++)
            {
                var r = revealers[i];
                if (r == null || !r.IsActive) continue;
                gridManager.RevealArea(r.GridPosition, r.Radius, day);
            }

            // Reveal된 셀의 intensity를 즉시 1.0으로 설정
            if (fadeAnimator != null)
            {
                for (int i = 0; i < revealers.Count; i++)
                {
                    var r = revealers[i];
                    if (r == null || !r.IsActive) continue;
                    ApplyRevealIntensity(r.GridPosition, r.Radius);
                }
            }

            revealDirty = true;
            exploredDirty = true;
        }

        private void ApplyRevealIntensity(Vector2Int center, int radius)
        {
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                for (int x = center.x - radius; x <= center.x + radius; x++)
                {
                    if (x < 0 || x >= width || y < 0 || y >= height) continue;
                    fadeAnimator.SetImmediate(x, y, 1f);
                }
            }
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            UpdateCurrentTex();
            if (exploredDirty) UpdateExploredTex();
        }

        private void UpdateCurrentTex()
        {
            // fogEnabled=false 이면 전체를 255로 채워서 완전 시야
            if (!fogEnabled)
            {
                for (int i = 0; i < currentBuffer.Length; i++)
                    currentBuffer[i] = 255;
                currentTex.LoadRawTextureData(currentBuffer);
                currentTex.Apply(false);
                return;
            }

            // fadeAnimator가 매 프레임 intensity를 갱신하므로 항상 업로드
            float[] src = fadeAnimator != null ? fadeAnimator.IntensityArray : null;
            if (src == null) return;

            int total = width * height;
            for (int i = 0; i < total; i++)
                currentBuffer[i] = (byte)(Mathf.Clamp01(src[i]) * 255f + 0.5f);

            currentTex.LoadRawTextureData(currentBuffer);
            currentTex.Apply(false);
        }

        private void UpdateExploredTex()
        {
            // shader expectation: RGB = 레이어별 시야 (low/mid/high), A = explored flag
            // TM 모델: 3 레이어 구분 없이 모두 동일하게 "explored 여부" 마킹
            int total = width * height;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int cellIdx = y * width + x;
                    int byteIdx = cellIdx * 4;
                    bool explored = gridManager.IsExplored(new Vector2Int(x, y));
                    byte v = (byte)(explored ? 255 : 0);
                    exploredBuffer[byteIdx + 0] = v; // R
                    exploredBuffer[byteIdx + 1] = v; // G
                    exploredBuffer[byteIdx + 2] = v; // B
                    exploredBuffer[byteIdx + 3] = v; // A (isExplored flag)
                }
            }

            exploredTex.LoadRawTextureData(exploredBuffer);
            exploredTex.Apply(false);
            exploredDirty = false;
        }

        private void UploadInitial()
        {
            for (int i = 0; i < currentBuffer.Length; i++) currentBuffer[i] = 0;
            for (int i = 0; i < exploredBuffer.Length; i++) exploredBuffer[i] = 0;
            currentTex.LoadRawTextureData(currentBuffer);
            currentTex.Apply(false);
            exploredTex.LoadRawTextureData(exploredBuffer);
            exploredTex.Apply(false);
        }
    }
}
