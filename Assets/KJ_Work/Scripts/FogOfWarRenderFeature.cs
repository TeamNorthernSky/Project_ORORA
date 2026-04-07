using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FogOfWarRenderFeature : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        private Material fogMaterial;
        private RTHandle source;
        private RTHandle tempCopy;

        public CustomRenderPass(Material mat)
        {
            fogMaterial = mat;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Unity 2022+ 에서는 AddRenderPasses 단계가 아닌 OnCameraSetup 내부에서 렌더 타겟을 가져오는 것이 안전합니다.
            source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0; // Color pass only
            RenderingUtils.ReAllocateIfNeeded(ref tempCopy, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_TempFogCopy");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (fogMaterial == null || source == null) return;
            
            var volume = VolumeManager.instance.stack.GetComponent<FogOfWarVolume>();
            if (volume == null || !volume.IsActive()) return;

            CommandBuffer cmd = CommandBufferPool.Get("FogOfWar Blit");

            // Apply Volume overloads
            fogMaterial.SetColor("_FogColor", volume.fogColor.value);
            if (volume.noiseTexture.value != null)
                fogMaterial.SetTexture("_NoiseTex", volume.noiseTexture.value);
                
            // Combine Vignette data
            Vector4 playerUV = Shader.GetGlobalVector("_PlayerWorldPos");
            playerUV.z = volume.vignetteOuterRadius.value;
            playerUV.w = volume.vignetteInnerRadius.value;
            Shader.SetGlobalVector("_PlayerWorldPos", playerUV);

            // Unity 2022+ 에서는 cmd.Blit 대신 Blitter (Core) 를 사용하여 RTHandle 간 복사를 수행합니다.
            Blitter.BlitCameraTexture(cmd, source, tempCopy, fogMaterial, 0);
            Blitter.BlitCameraTexture(cmd, tempCopy, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            tempCopy?.Release();
        }
    }

    private CustomRenderPass m_ScriptablePass;
    private Material m_Material;
    public Shader shader;

    public override void Create()
    {
        if (shader != null)
        {
            m_Material = CoreUtils.CreateEngineMaterial(shader);
        }
        
        m_ScriptablePass = new CustomRenderPass(m_Material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType != CameraType.Game || m_Material == null) return;
        
        // OnCameraSetup 내부에서 Source 타겟을 할당하도록 변경했으므로 여기선 Pass만 큐에 넣습니다.
        renderer.EnqueuePass(m_ScriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
        m_ScriptablePass?.Dispose();

        if (Application.isPlaying && m_Material != null)
            Destroy(m_Material);
        else if (m_Material != null)
            DestroyImmediate(m_Material);
            
        base.Dispose(disposing);
    }
}
