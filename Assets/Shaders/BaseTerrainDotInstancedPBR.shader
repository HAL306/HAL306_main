Shader "Custom/BaseTerrainDotInstancedPBR"
{
    Properties
    {
        [HideInInspector] _DotSize ("Dot Size", Float) = 0.125

        [Header(Layer 0 (Base))]
        _Layer0_Tex ("L0 Main Tex", 2D) = "white" {}
        [Normal] _Layer0_BumpMap ("L0 Normal Map", 2D) = "bump" {}
        _Layer0_BumpScale ("L0 Normal Scale", Float) = 1.0
        _Layer0_ST ("L0 UV Tiling/Offset", Vector) = (1, 1, 0, 0)
        _Layer0_ThresholdTex ("L0 Threshold Tex", 2D) = "white" {}
        _Layer0_ThresholdParams ("L0 Threshold Params (Tiling, HasTex, 0, 0)", Vector) = (1.0, 0.0, 0, 0)
        _Layer0_RampTex ("L0 Ramp Tex", 2D) = "white" {}
        _Layer0_DistRange ("L0 Dist (Start, End, UseRamp, Active)", Vector) = (0, 0, 0, 1)
        _Layer0_BaseColor ("L0 Tint Color", Color) = (1, 1, 1, 1)
        _Layer0_Metallic ("L0 Metallic", Range(0.0, 1.0)) = 0.0
        _Layer0_Smoothness ("L0 Smoothness", Range(0.0, 1.0)) = 0.5
        _Layer0_EnvLightStrength ("L0 Environment Light", Range(0.0, 1.0)) = 0.1
        _Layer0_ShadowColorRetain ("L0 Shadow Color Retain", Range(0.0, 1.0)) = 0.2
        _Layer0_Cutoff ("L0 Alpha Cutoff", Range(0.0, 1.0)) = 0.01

        [Header(Layer 1)]
        _Layer1_Tex ("L1 Main Tex", 2D) = "white" {}
        [Normal] _Layer1_BumpMap ("L1 Normal Map", 2D) = "bump" {}
        _Layer1_BumpScale ("L1 Normal Scale", Float) = 1.0
        _Layer1_ST ("L1 UV Tiling/Offset", Vector) = (1, 1, 0, 0)
        _Layer1_ThresholdTex ("L1 Threshold Tex", 2D) = "white" {}
        _Layer1_ThresholdParams ("L1 Threshold Params (Tiling, HasTex, 0, 0)", Vector) = (1.0, 0.0, 0, 0)
        _Layer1_RampTex ("L1 Ramp Tex", 2D) = "white" {}
        _Layer1_DistRange ("L1 Dist (Start, End, UseRamp, Active)", Vector) = (0, 0, 0, 0)
        _Layer1_BaseColor ("L1 Tint Color", Color) = (1, 1, 1, 1)
        _Layer1_Metallic ("L1 Metallic", Range(0.0, 1.0)) = 0.0
        _Layer1_Smoothness ("L1 Smoothness", Range(0.0, 1.0)) = 0.5
        _Layer1_EnvLightStrength ("L1 Environment Light", Range(0.0, 1.0)) = 0.1
        _Layer1_ShadowColorRetain ("L1 Shadow Color Retain", Range(0.0, 1.0)) = 0.2
        _Layer1_Cutoff ("L1 Alpha Cutoff", Range(0.0, 1.0)) = 0.01

        [Header(Layer 2)]
        _Layer2_Tex ("L2 Main Tex", 2D) = "white" {}
        [Normal] _Layer2_BumpMap ("L2 Normal Map", 2D) = "bump" {}
        _Layer2_BumpScale ("L2 Normal Scale", Float) = 1.0
        _Layer2_ST ("L2 UV Tiling/Offset", Vector) = (1, 1, 0, 0)
        _Layer2_ThresholdTex ("L2 Threshold Tex", 2D) = "white" {}
        _Layer2_ThresholdParams ("L2 Threshold Params (Tiling, HasTex, 0, 0)", Vector) = (1.0, 0.0, 0, 0)
        _Layer2_RampTex ("L2 Ramp Tex", 2D) = "white" {}
        _Layer2_DistRange ("L2 Dist (Start, End, UseRamp, Active)", Vector) = (0, 0, 0, 0)
        _Layer2_BaseColor ("L2 Tint Color", Color) = (1, 1, 1, 1)
        _Layer2_Metallic ("L2 Metallic", Range(0.0, 1.0)) = 0.0
        _Layer2_Smoothness ("L2 Smoothness", Range(0.0, 1.0)) = 0.5
        _Layer2_EnvLightStrength ("L2 Environment Light", Range(0.0, 1.0)) = 0.1
        _Layer2_ShadowColorRetain ("L2 Shadow Color Retain", Range(0.0, 1.0)) = 0.2
        _Layer2_Cutoff ("L2 Alpha Cutoff", Range(0.0, 1.0)) = 0.01

        [Header(Layer 3)]
        _Layer3_Tex ("L3 Main Tex", 2D) = "white" {}
        [Normal] _Layer3_BumpMap ("L3 Normal Map", 2D) = "bump" {}
        _Layer3_BumpScale ("L3 Normal Scale", Float) = 1.0
        _Layer3_ST ("L3 UV Tiling/Offset", Vector) = (1, 1, 0, 0)
        _Layer3_ThresholdTex ("L3 Threshold Tex", 2D) = "white" {}
        _Layer3_ThresholdParams ("L3 Threshold Params (Tiling, HasTex, 0, 0)", Vector) = (1.0, 0.0, 0, 0)
        _Layer3_RampTex ("L3 Ramp Tex", 2D) = "white" {}
        _Layer3_DistRange ("L3 Dist (Start, End, UseRamp, Active)", Vector) = (0, 0, 0, 0)
        _Layer3_BaseColor ("L3 Tint Color", Color) = (1, 1, 1, 1)
        _Layer3_Metallic ("L3 Metallic", Range(0.0, 1.0)) = 0.0
        _Layer3_Smoothness ("L3 Smoothness", Range(0.0, 1.0)) = 0.5
        _Layer3_EnvLightStrength ("L3 Environment Light", Range(0.0, 1.0)) = 0.1
        _Layer3_ShadowColorRetain ("L3 Shadow Color Retain", Range(0.0, 1.0)) = 0.2
        _Layer3_Cutoff ("L3 Alpha Cutoff", Range(0.0, 1.0)) = 0.01
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct BaseDotInstance
            {
                float3 worldPosition;
                float4 color;
                float2 uv;
                float3 worldNormal;
                float edgeDistance;
            };

            #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                StructuredBuffer<BaseDotInstance> _DotDataBuffer;
            #endif

            TEXTURE2D(_Layer0_Tex);
            SAMPLER(sampler_Layer0_Tex);

            TEXTURE2D(_Layer0_BumpMap);
            TEXTURE2D(_Layer0_ThresholdTex);
            TEXTURE2D(_Layer0_RampTex);

            TEXTURE2D(_Layer1_Tex);
            TEXTURE2D(_Layer1_BumpMap);
            TEXTURE2D(_Layer1_ThresholdTex);
            TEXTURE2D(_Layer1_RampTex);

            TEXTURE2D(_Layer2_Tex);
            TEXTURE2D(_Layer2_BumpMap);
            TEXTURE2D(_Layer2_ThresholdTex);
            TEXTURE2D(_Layer2_RampTex);

            TEXTURE2D(_Layer3_Tex);
            TEXTURE2D(_Layer3_BumpMap);
            TEXTURE2D(_Layer3_ThresholdTex);
            TEXTURE2D(_Layer3_RampTex);

            CBUFFER_START(UnityPerMaterial)
                float _DotSize;

                float4 _Layer0_ST;
                float4 _Layer0_DistRange;
                float4 _Layer0_ThresholdParams;
                float _Layer0_BumpScale;
                half4 _Layer0_BaseColor;
                float _Layer0_Metallic;
                float _Layer0_Smoothness;
                float _Layer0_EnvLightStrength;
                float _Layer0_ShadowColorRetain;
                float _Layer0_Cutoff;

                float4 _Layer1_ST;
                float4 _Layer1_DistRange;
                float4 _Layer1_ThresholdParams;
                float _Layer1_BumpScale;
                half4 _Layer1_BaseColor;
                float _Layer1_Metallic;
                float _Layer1_Smoothness;
                float _Layer1_EnvLightStrength;
                float _Layer1_ShadowColorRetain;
                float _Layer1_Cutoff;

                float4 _Layer2_ST;
                float4 _Layer2_DistRange;
                float4 _Layer2_ThresholdParams;
                float _Layer2_BumpScale;
                half4 _Layer2_BaseColor;
                float _Layer2_Metallic;
                float _Layer2_Smoothness;
                float _Layer2_EnvLightStrength;
                float _Layer2_ShadowColorRetain;
                float _Layer2_Cutoff;

                float4 _Layer3_ST;
                float4 _Layer3_DistRange;
                float4 _Layer3_ThresholdParams;
                float _Layer3_BumpScale;
                half4 _Layer3_BaseColor;
                float _Layer3_Metallic;
                float _Layer3_Smoothness;
                float _Layer3_EnvLightStrength;
                float _Layer3_ShadowColorRetain;
                float _Layer3_Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 normalWS : NORMAL;
                float edgeDist : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            void setup() {}

            Varyings vert(Attributes input, uint instanceID : SV_InstanceID)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 worldCenter = float3(0, 0, 0);
                float2 dotUV = float2(0, 0);
                float3 worldNormal = float3(0, 0, -1);
                float edgeDist = 0.0;

            #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                BaseDotInstance dotInst = _DotDataBuffer[instanceID];
                worldCenter = dotInst.worldPosition;
                dotUV = dotInst.uv;
                worldNormal = dotInst.worldNormal;
                edgeDist = dotInst.edgeDistance;
            #endif

                float3 finalWorldPos = worldCenter + (input.positionOS.xyz * _DotSize);

                output.positionWS = finalWorldPos;
                output.positionCS = TransformWorldToHClip(finalWorldPos);
                output.uv = dotUV;
                output.normalWS = worldNormal;
                output.edgeDist = edgeDist;

                return output;
            }

            float3 BlendSoftLight(float3 baseCol, float3 blendCol)
            {
                float3 result;
                result.r = (blendCol.r < 0.5) ? (2.0 * baseCol.r * blendCol.r + baseCol.r * baseCol.r * (1.0 - 2.0 * blendCol.r))
                                              : (sqrt(baseCol.r) * (2.0 * blendCol.r - 1.0) + 2.0 * baseCol.r * (1.0 - blendCol.r));
                result.g = (blendCol.g < 0.5) ? (2.0 * baseCol.g * blendCol.g + baseCol.g * baseCol.g * (1.0 - 2.0 * blendCol.g))
                                              : (sqrt(baseCol.g) * (2.0 * blendCol.g - 1.0) + 2.0 * baseCol.g * (1.0 - blendCol.g));
                result.b = (blendCol.b < 0.5) ? (2.0 * baseCol.b * blendCol.b + baseCol.b * baseCol.b * (1.0 - 2.0 * blendCol.b))
                                              : (sqrt(baseCol.b) * (2.0 * blendCol.b - 1.0) + 2.0 * baseCol.b * (1.0 - blendCol.b));
                return saturate(result);
            }

            void EvaluateLayer(
                float2 uv, float dist,
                Texture2D colTex, float4 st, half4 tintColor,
                Texture2D bumpTex, float bumpScale,
                Texture2D threshTex, float4 threshParams,
                Texture2D rampTex,
                float4 distRange,
                float metallic, float smoothness, float envLight, float shadowRetain, float cutoff,
                inout float3 accumulatedColor, inout float3 accumulatedNormal, inout float currentAlpha,
                inout float accMetallic, inout float accSmoothness, inout float accEnvLight, inout float accShadowRetain,
                inout float finalCutoff)
            {
                // レイヤーが無効化されている場合はスキップ
                if (distRange.w < 0.5) return;

                float startD = distRange.x;
                float endD   = distRange.y;
                float useRamp = distRange.z;

                // 1. 開始距離より手前なら描画しない（下のレイヤーをそのまま残す）
                if (dist < startD) return;

                float2 layerUV = uv * st.xy + st.zw;
                float threshTiling = threshParams.x;
                float hasThresholdTex = threshParams.y;

                float opacity = 0.0;
                float threshVal = 1.0;

                // 2. 距離ブレンド判定
                if (endD > startD)
                {
                    // 終了距離を超えている場合、このレイヤーは終了しているのでスキップ（透過）
                    if (dist >= endD)
                    {
                        return;
                    }

                    // 開始〜終了の間：0.0 〜 1.0 に正規化
                    float normalizedDist = (dist - startD) / (endD - startD);

                    // 閾値テクスチャのサンプリング
                    if (hasThresholdTex > 0.5)
                    {
                        float2 threshUV = layerUV * threshTiling;
                        threshVal = SAMPLE_TEXTURE2D(threshTex, sampler_Layer0_Tex, threshUV).r;
                    }
                    else
                    {
                        threshVal = 1.0;
                    }

                    // 正規化値が閾値以下なら不透明度100%、上回っていれば0%（透過）
                    opacity = (normalizedDist <= threshVal) ? 1.0 : 0.0;
                }
                else
                {
                    // endD <= startD（距離無限）：startD以上なら全ピクセル100%不透明
                    opacity = 1.0;
                    threshVal = 1.0;
                }

                // 透過（0%）の場合は下のレイヤーを保持して終了
                if (opacity <= 0.0) return;

                // 3. カラーとテクスチャのサンプリング
                float4 layerCol = SAMPLE_TEXTURE2D(colTex, sampler_Layer0_Tex, layerUV) * tintColor;

                // 4. ランプテクスチャのソフトライト合成
                if (useRamp > 0.5)
                {
                    float uRamp = (endD > startD) ? saturate((dist - startD) / max(0.001, threshVal)) : 0.0;
                    float4 rampCol = SAMPLE_TEXTURE2D(rampTex, sampler_Layer0_Tex, float2(uRamp, 0.5));
                    float3 softLightBlend = rampCol.rgb * 2.0;
                    layerCol.rgb = BlendSoftLight(layerCol.rgb, softLightBlend);
                }

                float4 bumpSample = SAMPLE_TEXTURE2D(bumpTex, sampler_Layer0_Tex, layerUV);
                float3 layerNormal = UnpackNormalScale(bumpSample, bumpScale);

                // 5. 下位レイヤーの上に重ねる（不透明度100%のピクセルだけ上書き）
                accumulatedColor = lerp(accumulatedColor, layerCol.rgb, opacity);
                accumulatedNormal = lerp(accumulatedNormal, layerNormal, opacity);
                currentAlpha = max(currentAlpha, layerCol.a * opacity);

                accMetallic = lerp(accMetallic, metallic, opacity);
                accSmoothness = lerp(accSmoothness, smoothness, opacity);
                accEnvLight = lerp(accEnvLight, envLight, opacity);
                accShadowRetain = lerp(accShadowRetain, shadowRetain, opacity);
                finalCutoff = lerp(finalCutoff, cutoff, opacity);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 finalColor = float3(0, 0, 0);
                float3 finalNormalTS = float3(0, 0, 1);
                float finalAlpha = 0.0;
                float metallic = 0.0;
                float smoothness = 0.5;
                float envLight = 0.1;
                float shadowRetain = 0.2;
                float activeCutoff = 0.01;

                EvaluateLayer(input.uv, input.edgeDist,
                    _Layer0_Tex, _Layer0_ST, _Layer0_BaseColor,
                    _Layer0_BumpMap, _Layer0_BumpScale,
                    _Layer0_ThresholdTex, _Layer0_ThresholdParams,
                    _Layer0_RampTex, _Layer0_DistRange,
                    _Layer0_Metallic, _Layer0_Smoothness, _Layer0_EnvLightStrength, _Layer0_ShadowColorRetain, _Layer0_Cutoff,
                    finalColor, finalNormalTS, finalAlpha,
                    metallic, smoothness, envLight, shadowRetain, activeCutoff);

                EvaluateLayer(input.uv, input.edgeDist,
                    _Layer1_Tex, _Layer1_ST, _Layer1_BaseColor,
                    _Layer1_BumpMap, _Layer1_BumpScale,
                    _Layer1_ThresholdTex, _Layer1_ThresholdParams,
                    _Layer1_RampTex, _Layer1_DistRange,
                    _Layer1_Metallic, _Layer1_Smoothness, _Layer1_EnvLightStrength, _Layer1_ShadowColorRetain, _Layer1_Cutoff,
                    finalColor, finalNormalTS, finalAlpha,
                    metallic, smoothness, envLight, shadowRetain, activeCutoff);

                EvaluateLayer(input.uv, input.edgeDist,
                    _Layer2_Tex, _Layer2_ST, _Layer2_BaseColor,
                    _Layer2_BumpMap, _Layer2_BumpScale,
                    _Layer2_ThresholdTex, _Layer2_ThresholdParams,
                    _Layer2_RampTex, _Layer2_DistRange,
                    _Layer2_Metallic, _Layer2_Smoothness, _Layer2_EnvLightStrength, _Layer2_ShadowColorRetain, _Layer2_Cutoff,
                    finalColor, finalNormalTS, finalAlpha,
                    metallic, smoothness, envLight, shadowRetain, activeCutoff);

                EvaluateLayer(input.uv, input.edgeDist,
                    _Layer3_Tex, _Layer3_ST, _Layer3_BaseColor,
                    _Layer3_BumpMap, _Layer3_BumpScale,
                    _Layer3_ThresholdTex, _Layer3_ThresholdParams,
                    _Layer3_RampTex, _Layer3_DistRange,
                    _Layer3_Metallic, _Layer3_Smoothness, _Layer3_EnvLightStrength, _Layer3_ShadowColorRetain, _Layer3_Cutoff,
                    finalColor, finalNormalTS, finalAlpha,
                    metallic, smoothness, envLight, shadowRetain, activeCutoff);

                if (finalAlpha <= 0.0001 && activeCutoff > 0.0)
                {
                    discard;
                }

                float3 worldNormal = normalize(input.normalWS);
                float3 t = float3(1, 0, 0);
                if (abs(dot(worldNormal, t)) > 0.99) t = float3(0, 1, 0);
                t = normalize(t - worldNormal * dot(worldNormal, t));
                float3 b = cross(worldNormal, t);
                float3x3 tbn = float3x3(t, b, worldNormal);

                float3 blendedWorldNormal = normalize(mul(finalNormalTS, tbn));

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = blendedWorldNormal;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.bakedGI = SampleSH(inputData.normalWS) * envLight;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = finalColor;
                surfaceData.metallic = metallic;
                surfaceData.smoothness = smoothness;
                surfaceData.alpha = finalAlpha;
                surfaceData.occlusion = envLight;
                surfaceData.emission = finalColor * shadowRetain;

                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }
    }
}