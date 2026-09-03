Shader "Custom/DotInstancedPBR"
{
    Properties
    {
        [Header(Base Textures)]
        [MainTexture] _MainTex("Main Texture (Baked/Albedo)", 2D) = "white" {}
        [MainColor] _BaseColor("Tint Color", Color) = (1, 1, 1, 1)
        
        [Header(Normal Map)]
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0
        _HasBumpMap("Has Normal Map (0: No, 1: Yes)", Float) = 0.0

        [Header(PBR Settings)]
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        
        [Header(Shadow and Color Settings)]
        _EnvLightStrength("Environment Light (Anti-Grey)", Range(0.0, 1.0)) = 0.1
        _ShadowColorRetain("Shadow Color Retain (Emission)", Range(0.0, 1.0)) = 0.2
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.01

        [HideInInspector] _DotSize ("Dot Size", Float) = 0.125
    }
    SubShader
    {
        Tags { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline" 
            "UniversalMaterialType"="Lit" 
            "Queue"="Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct DotInstance
            {
                float3 localPosition;
                float4 color;
                float2 uv;
                float3 localNormal;
                float isEdge;
            };

            #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                StructuredBuffer<DotInstance> _DotDataBuffer;
            #endif

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4x4 _CustomLocalToWorld;
                float4 _MainTex_ST;
                half4 _BaseColor;
                float _DotSize;
                float _Metallic;
                float _Smoothness;
                float _EnvLightStrength;
                float _ShadowColorRetain;
                float _BumpScale;
                float _HasBumpMap;
                float _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float3 normalWS : NORMAL;
                float3 tangentWS : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float4 color : COLOR;
                float isEdge : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            void setup() {}

            Varyings vert (Attributes input, uint instanceID : SV_InstanceID)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 dotLocalCenter = float3(0, 0, 0);
                float4 dotColor = float4(1, 1, 1, 1);
                float2 dotUV = input.uv;
                float3 dotLocalNormal = float3(0, 0, -1);
                float isEdge = 0.0;

            #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                DotInstance dotInst = _DotDataBuffer[instanceID];
                dotLocalCenter = dotInst.localPosition;
                dotColor *= dotInst.color;
                dotUV = dotInst.uv;
                dotLocalNormal = dotInst.localNormal;
                isEdge = dotInst.isEdge;
            #endif

                float3 combinedLocalPos = (input.positionOS.xyz * _DotSize) + dotLocalCenter;
                float3 worldPos = mul(_CustomLocalToWorld, float4(combinedLocalPos, 1.0)).xyz;

                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);

                output.uv = TRANSFORM_TEX(dotUV, _MainTex);
                output.color = lerp(dotColor, dotColor * float4(0.85, 0.85, 0.85, 1.0), isEdge);
                output.isEdge = isEdge;

                float3 worldNormal = normalize(mul((float3x3)_CustomLocalToWorld, dotLocalNormal));
                output.normalWS = worldNormal;

                float3 t = normalize(mul((float3x3)_CustomLocalToWorld, float3(1, 0, 0)));
                if (abs(dot(worldNormal, t)) > 0.99)
                {
                    t = normalize(mul((float3x3)_CustomLocalToWorld, float3(0, 1, 0)));
                }
                t = normalize(t - worldNormal * dot(worldNormal, t));
                float3 b = normalize(cross(worldNormal, t));

                output.tangentWS = t;
                output.bitangentWS = b;

                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 albedoAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _BaseColor * input.color;
                if (albedoAlpha.a < _Cutoff)
                {
                    discard;
                }

                float3 normalWS = normalize(input.normalWS);
                float3 tangentWS = normalize(input.tangentWS);
                float3 bitangentWS = normalize(input.bitangentWS);
                float3x3 tbn = float3x3(tangentWS, bitangentWS, normalWS);

                float3 finalNormalWS = normalWS;

                if (_HasBumpMap > 0.5)
                {
                    half4 bumpMapSample = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv);
                    half3 tangentNormal = UnpackNormalScale(bumpMapSample, _BumpScale);
                    finalNormalWS = normalize(mul(tangentNormal, tbn));
                    finalNormalWS = normalize(lerp(finalNormalWS, normalWS, input.isEdge * 0.75));
                }

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = finalNormalWS; 
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.bakedGI = SampleSH(inputData.normalWS) * _EnvLightStrength;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedoAlpha.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.alpha = albedoAlpha.a;
                surfaceData.occlusion = _EnvLightStrength; 
                surfaceData.emission = albedoAlpha.rgb * _ShadowColorRetain;
                surfaceData.clearCoatMask = 0.0;
                surfaceData.clearCoatSmoothness = 0.0;

                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct DotInstance
            {
                float3 localPosition;
                float4 color;
                float2 uv;
                float3 localNormal;
                float isEdge;
            };

            #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                StructuredBuffer<DotInstance> _DotDataBuffer;
            #endif

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4x4 _CustomLocalToWorld;
                float4 _MainTex_ST;
                half4 _BaseColor;
                float _DotSize;
                float _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            void setup() {}

            Varyings ShadowPassVertex(Attributes input, uint instanceID : SV_InstanceID)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 dotLocalCenter = float3(0, 0, 0);
                float2 dotUV = input.uv;
                float3 dotLocalNormal = input.normalOS;

            #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                DotInstance dotInst = _DotDataBuffer[instanceID];
                dotLocalCenter = dotInst.localPosition;
                dotUV = dotInst.uv;
                dotLocalNormal = dotInst.localNormal;
            #endif

                float3 combinedLocalPos = (input.positionOS.xyz * _DotSize) + dotLocalCenter;
                float3 worldPos = mul(_CustomLocalToWorld, float4(combinedLocalPos, 1.0)).xyz;
                float3 worldNormal = normalize(mul((float3x3)_CustomLocalToWorld, dotLocalNormal));

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(worldPos, worldNormal, _MainLightPosition.xyz));
                output.uv = TRANSFORM_TEX(dotUV, _MainTex);

                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 albedoAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _BaseColor;
                if (albedoAlpha.a < _Cutoff)
                {
                    discard;
                }
                return 0;
            }
            ENDHLSL
        }
    }
}