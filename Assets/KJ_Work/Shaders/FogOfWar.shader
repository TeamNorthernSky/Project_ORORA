Shader "Hidden/FogOfWar"
{
    Properties
    {
        // Unity 2022+ Blitter uses _BlitTexture instead of _MainTex
        _BlitTexture("Source", 2D) = "white" {}
        _FogTex("Fog Map", 2D) = "black" {}
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _FogColor("Fog Color", Color) = (0.1, 0.1, 0.1, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        ZWrite Off ZTest Always Blend Off Cull Off

        Pass
        {
            Name "FogOfWarPass"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // Blit.hlsl에서 _BlitTexture와 sampler_BlitTexture 등을 자동으로 정의해 주며, 
            // Vert() 버텍스 쉐이더와 Varyings 구조체를 제공합니다.
            // 단, 샘플러는 프로젝트 셋업에 따라 누락될 수 있으므로 명시적으로 추가합니다.
            SAMPLER(sampler_BlitTexture);

            TEXTURE2D(_FogTex);               SAMPLER(sampler_FogTex);
            TEXTURE2D(_NoiseTex);             SAMPLER(sampler_NoiseTex);

            float4 _FogColor;
            float4 _MapBounds; // x: minX, y: minZ, z: width, w: height
            float4 _PlayerWorldPos; // x: worldX, y: worldZ, z: Outer Radius, w: Inner Radius
            
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                // 화면 픽셀 복사 (Blit.hlsl 에 정의된 _BlitTexture 사용)
                half4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv);
                
                float rawDepth = SampleSceneDepth(uv);
                
                // 스카이박스(원경) 제외 처리:
                // 안개 효과가 스카이박스까지 덮어서 하늘이 완전히 단색으로 사라지는 현상을 방지합니다.
                #if UNITY_REVERSED_Z
                if (rawDepth < 0.000001) return sceneColor;
                #else
                if (rawDepth > 0.999999) return sceneColor;
                #endif

                float3 worldPos = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);

                float2 fogUV = float2(worldPos.x - _MapBounds.x, worldPos.z - _MapBounds.y) / _MapBounds.zw;

                float visibility = 0.0;
                
                if(fogUV.x >= 0.0 && fogUV.x <= 1.0 && fogUV.y >= 0.0 && fogUV.y <= 1.0)
                {
                    visibility = SAMPLE_TEXTURE2D(_FogTex, sampler_FogTex, fogUV).r;
                }

                // 노이즈(일렁임) 추가
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, fogUV * 5.0 + _Time.y * 0.05).r;
                visibility *= lerp(0.8, 1.2, noise);

                // 플레이어 월드 좌표 기준 비네트(Vignette) 추가 적용 (보간된 위치 기반)
                float dist = length(worldPos.xz - _PlayerWorldPos.xy);
                float vignette = 1.0 - smoothstep(_PlayerWorldPos.w, _PlayerWorldPos.z, dist);

                visibility = max(visibility, vignette);
                visibility = saturate(visibility);

                // 안개 공식 수정: 
                // 시야가 밝지 않은 곳(1.0 - visibility)에 대해서만 알파(_FogColor.a) 비율만큼 안개를 덮습니다.
                float fogDensity = (1.0 - visibility) * _FogColor.a;
                half3 finalColor = lerp(sceneColor.rgb, _FogColor.rgb, fogDensity);

                return half4(finalColor, sceneColor.a);
            }
            ENDHLSL
        }
    }
}
