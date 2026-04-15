using System.Collections.Generic;
using UnityEngine;

namespace Orora.TestMerge
{
    /// <summary>
    /// 셀 단위 시야 intensity를 시간 보간으로 관리.
    /// - RevealArea: intensity 즉시 1.0
    /// - StartRefogFade: duration 동안 1.0 → 0.0 (시각적 페이드)
    /// TMFogRenderManager가 매 프레임 intensity 배열을 읽어 텍스처 갱신.
    /// </summary>
    public class TMFogFadeAnimator : MonoBehaviour
    {
        [Header("Refog Fade Duration (s)")]
        [SerializeField, Min(0.01f)] private float refogDuration = 1.5f;

        public float RefogDuration => refogDuration;

        private float[] intensity;     // 현재 렌더용 값 [0,1]
        private float[] fadeStart;     // 페이드 시작 시각 (Time.time). 0 = 페이드 없음
        private float[] fadeFrom;
        private float[] fadeTo;
        private float[] fadeDuration;  // 셀별 지속시간 (refogDuration과 다를 수 있음)

        private int width;
        private int height;
        private bool initialized;

        public bool IsInitialized => initialized;

        public void Initialize(int gridWidth, int gridHeight)
        {
            width = gridWidth;
            height = gridHeight;
            int total = width * height;
            intensity = new float[total];
            fadeStart = new float[total];
            fadeFrom = new float[total];
            fadeTo = new float[total];
            fadeDuration = new float[total];
            initialized = true;
        }

        public float GetIntensity(int x, int y)
        {
            if (!initialized) return 0f;
            if (x < 0 || x >= width || y < 0 || y >= height) return 0f;
            return intensity[y * width + x];
        }

        public void SetImmediate(int x, int y, float value)
        {
            if (!initialized) return;
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            int idx = y * width + x;
            intensity[idx] = Mathf.Clamp01(value);
            fadeStart[idx] = 0f;
        }

        public void StartFade(int x, int y, float from, float to, float duration)
        {
            if (!initialized) return;
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            int idx = y * width + x;
            fadeFrom[idx] = from;
            fadeTo[idx] = to;
            fadeDuration[idx] = Mathf.Max(0.0001f, duration);
            fadeStart[idx] = Time.time;
        }

        public void StartRefogFade(int x, int y)
        {
            if (!initialized) return;
            float current = GetIntensity(x, y);
            StartFade(x, y, current, 0f, refogDuration);
        }

        private void Update()
        {
            if (!initialized) return;
            float now = Time.time;
            int total = intensity.Length;
            for (int i = 0; i < total; i++)
            {
                if (fadeStart[i] <= 0f) continue;
                float t = (now - fadeStart[i]) / fadeDuration[i];
                if (t >= 1f)
                {
                    intensity[i] = Mathf.Clamp01(fadeTo[i]);
                    fadeStart[i] = 0f;
                }
                else
                {
                    intensity[i] = Mathf.Lerp(fadeFrom[i], fadeTo[i], t);
                }
            }
        }

        /// <summary>전체 intensity 배열 직접 접근 (TMFogRenderManager용).</summary>
        public float[] IntensityArray => intensity;
    }
}
