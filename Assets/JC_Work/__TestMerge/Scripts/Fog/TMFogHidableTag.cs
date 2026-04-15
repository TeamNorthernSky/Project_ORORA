using UnityEngine;

namespace Orora.TestMerge
{
    /// <summary>
    /// JC FogHidableTag의 TM 버전. 자식 Renderer만 FogHidable Layer로 설정.
    /// 부모(유닛/광산 등)의 물리 Layer는 유지.
    /// 공용 규약 `FogRenderLayer.HidableLayerName`("FogHidable")을 그대로 사용.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class TMFogHidableTag : MonoBehaviour
    {
        private void OnValidate() => Apply();
        private void Awake() => Apply();

        private void Apply()
        {
            int layer = FogRenderLayer.HidableLayerIndex;
            if (layer < 0) return;

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                if (r.gameObject == gameObject) continue;
                r.gameObject.layer = layer;
            }
        }
    }
}
