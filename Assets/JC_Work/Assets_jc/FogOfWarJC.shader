Shader "Custom/JC/FogOfWar"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.75, 0.78, 0.85, 1.0)
        _FogDensityLow ("Fog Density - Low (Y<1)", Range(0, 1)) = 0.95
        _FogDensityMid ("Fog Density - Mid (Y 1~2)", Range(0, 1)) = 0.65
        _FogDensityHigh ("Fog Density - High (Y>2)", Range(0, 1)) = 0.30

        _NoiseScale1 ("Noise Scale (Base)", Float) = 6.0
        _NoiseScale2 ("Noise Scale (Detail)", Float) = 18.0
        _NoiseScale3 ("Noise Scale (Distortion)", Float) = 3.0

        _FlowSpeed1 ("Flow Speed (Base)", Float) = 0.02
        _FlowSpeed2 ("Flow Speed (Detail)", Float) = 0.035
        _FlowSpeed3 ("Flow Speed (Distortion)", Float) = 0.01

        _DistortionStrength ("Distortion Strength", Float) = 0.3
        _NoiseContrast ("Noise Contrast", Range(0.5, 8.0)) = 3.5

        _HeightTransition ("Height Transition Softness", Range(0.1, 2.0)) = 0.5
        _VisibleThreshold ("Visible Clear Threshold", Range(0.5, 1.0)) = 0.7

        [Toggle] _DebugMode ("Debug Mode (show visibility)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "FogOfWarPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ _DEBUGMODE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // _VisibilityTex는 Shader.SetGlobalTexture로 설정됨
            // 글로벌 텍스처는 CBUFFER 밖에서 선언
            TEXTURE2D(_VisibilityTex);
            SAMPLER(sampler_VisibilityTex);

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float _FogDensityLow;
                float _FogDensityMid;
                float _FogDensityHigh;

                float _NoiseScale1;
                float _NoiseScale2;
                float _NoiseScale3;

                float _FlowSpeed1;
                float _FlowSpeed2;
                float _FlowSpeed3;

                float _DistortionStrength;
                float _NoiseContrast;

                float _HeightTransition;
                float _VisibleThreshold;
            CBUFFER_END

            // ========== 노이즈 ==========

            float2 HashGradient(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float GradientNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float n00 = dot(HashGradient(i), f);
                float n10 = dot(HashGradient(i + float2(1, 0)), f - float2(1, 0));
                float n01 = dot(HashGradient(i + float2(0, 1)), f - float2(0, 1));
                float n11 = dot(HashGradient(i + float2(1, 1)), f - float2(1, 1));

                return lerp(lerp(n00, n10, u.x), lerp(n01, n11, u.x), u.y) * 0.5 + 0.5;
            }

            float FBM(float2 p, int octaves)
            {
                float val = 0.0;
                float amp = 0.5;
                float freq = 1.0;
                for (int i = 0; i < octaves; i++)
                {
                    val += amp * GradientNoise(p * freq);
                    freq *= 2.0;
                    amp *= 0.5;
                }
                return val;
            }

            // ========== 메인 ==========

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 sceneColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Depth → 월드 좌표
                float depth = SampleSceneDepth(input.uv);

                #if UNITY_REVERSED_Z
                    bool isSky = depth < 0.0001;
                #else
                    bool isSky = depth > 0.9999;
                #endif
                if (isSky) return sceneColor;

                float2 posNDC = input.uv * 2.0 - 1.0;
                #if UNITY_UV_STARTS_AT_TOP
                    posNDC.y = -posNDC.y;
                #endif

                float4 worldPos4 = mul(UNITY_MATRIX_I_VP, float4(posNDC, depth, 1.0));
                float3 worldPos = worldPos4.xyz / worldPos4.w;

                float2 gridUV = saturate(worldPos.xz / 64.0);

                // Visibility (0=미탐색, 0.5=탐색완료, 1.0=현재시야)
                float visibility = SAMPLE_TEXTURE2D(_VisibilityTex, sampler_VisibilityTex, gridUV).r;

                // Visible → 안개 없음
                if (visibility >= _VisibleThreshold)
                    return sceneColor;

                // ===== 안개 강도 =====
                float fogMask = 1.0 - saturate(visibility / _VisibleThreshold);

                // ===== 높이 기반 3레이어 =====
                float worldY = worldPos.y;
                float s = _HeightTransition;
                float lowFactor = 1.0 - saturate((worldY - 0.5) / s);
                float highFactor = saturate((worldY - 1.5) / s);
                float midFactor = max(0, 1.0 - lowFactor - highFactor);

                float baseDensity = lowFactor * _FogDensityLow
                                  + midFactor * _FogDensityMid
                                  + highFactor * _FogDensityHigh;

                // ===== 노이즈: 운해 =====
                float t = _Time.y;
                float2 nUV = worldPos.xz;

                // Distortion
                float2 dIn = nUV * _NoiseScale3 / 64.0 + float2(t * _FlowSpeed3, t * _FlowSpeed3 * 0.7);
                float2 dist = float2(
                    GradientNoise(dIn) - 0.5,
                    GradientNoise(dIn + float2(43, 17)) - 0.5
                ) * _DistortionStrength;

                // Base
                float2 bIn = nUV * _NoiseScale1 / 64.0 + dist + float2(t * _FlowSpeed1, t * _FlowSpeed1 * 0.3);
                float nBase = FBM(bIn, 4);

                // Detail
                float2 dIn2 = nUV * _NoiseScale2 / 64.0 + dist * 0.5 + float2(-t * _FlowSpeed2 * 0.5, t * _FlowSpeed2 * 0.8);
                float nDetail = FBM(dIn2, 3);

                // 합성
                float noise = nBase * 0.55 + nDetail * 0.45;
                noise = saturate((noise - 0.5) * _NoiseContrast + 0.5);

                // 구름 밀도 (노이즈에 의한 두꺼움/얇음)
                float cloud = lerp(0.2, 1.0, noise);

                // ===== 최종 =====
                float alpha = saturate(baseDensity * cloud * fogMask);

                half3 result = lerp(sceneColor.rgb, _FogColor.rgb, alpha);
                return half4(result, 1.0);
            }
            ENDHLSL
        }
    }
}
