Shader "Custom/DotInstancedPBR"
{
    Properties
    {
        [Header(Base Textures)]
        [MainTexture] _BaseMap("Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Tint Color", Color) = (1, 1, 1, 1)
        
        [Header(Normal Map)]
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0

        [Header(UV Settings)]
        _UVScale ("UV Scale", Vector) = (1, 1, 0, 0)
        _UVOffset ("UV Offset", Vector) = (0, 0, 0, 0)
        
        [Header(PBR Settings)]
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        
        [Header(Shadow and Color Settings)]
        _EnvLightStrength("Environment Light (Anti-Grey)", Range(0.0, 1.0)) = 0.1
        _ShadowColorRetain("Shadow Color Retain", Range(0.0, 1.0)) = 0.2

        [HideInInspector] _PixelSize ("Pixel Size", Float) = 0.05
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
                
                float3 tangentWS : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct PixelData
            {
                float2 position;
                float3 normal;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            TEXTURE2D(_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _PixelSize;
                float2 _UVScale;
                float2 _UVOffset;
                float4x4 _ObjectToWorldMatrix;
                float _Metallic;
                float _Smoothness;
                float _EnvLightStrength;
                float _ShadowColorRetain;
                float _BumpScale;
            CBUFFER_END

            StructuredBuffer<PixelData> positionBuffer;

            void setup() {}

            Varyings vert (Attributes input, uint instanceID : SV_InstanceID)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 localPos = input.positionOS.xyz * _PixelSize;
                float2 pixelCenter = float2(0, 0);
                float3 localNormal = float3(0, 0, 1);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                PixelData data = positionBuffer[instanceID];
                pixelCenter = data.position;
                localNormal = data.normal; 
                localPos.xy += pixelCenter;
            #endif

                output.uv = pixelCenter * _UVScale + _UVOffset;
                
                float3 worldPos = mul(_ObjectToWorldMatrix, float4(localPos, 1.0)).xyz;
                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);

                output.normalWS = normalize(mul((float3x3)_ObjectToWorldMatrix, localNormal));

                float3 t = float3(1, 0, 0);
                if (abs(localNormal.x) > 0.999) 
                {
                    t = float3(0, 1, 0);
                }
                t = normalize(t - localNormal * dot(localNormal, t));
                float3 b = cross(localNormal, t);

                output.tangentWS = normalize(mul((float3x3)_ObjectToWorldMatrix, t));
                output.bitangentWS = normalize(mul((float3x3)_ObjectToWorldMatrix, b));

                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv*0.2f) * _BaseColor;
                clip(albedoAlpha.a - 0.5);

                half4 bumpMapSample = SAMPLE_TEXTURE2D(_BumpMap, sampler_BaseMap, input.uv);
                half3 tangentNormal = UnpackNormalScale(bumpMapSample, _BumpScale);

                float3 normalWS = normalize(input.normalWS);
                float3 tangentWS = normalize(input.tangentWS);
                float3 bitangentWS = normalize(input.bitangentWS);
                float3x3 tbn = float3x3(tangentWS, bitangentWS, normalWS);

                float3 finalNormalWS = normalize(mul(tangentNormal, tbn));

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = finalNormalWS; 
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);

                inputData.bakedGI = SampleSH(inputData.normalWS) * _EnvLightStrength;

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedoAlpha.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.alpha = albedoAlpha.a;
                surfaceData.occlusion = _EnvLightStrength; 
                surfaceData.emission = albedoAlpha.rgb * _ShadowColorRetain;
                surfaceData.clearCoatMask = 0.0;
                surfaceData.clearCoatSmoothness = 0.0;

                half4 finalColor = UniversalFragmentPBR(inputData, surfaceData);

                return finalColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

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

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct PixelData
            {
                float2 position;
                float3 normal;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _PixelSize;
                float2 _UVScale;
                float2 _UVOffset;
                float4x4 _ObjectToWorldMatrix;
            CBUFFER_END

            StructuredBuffer<PixelData> positionBuffer;

            void setup() {}

            Varyings ShadowPassVertex(Attributes input, uint instanceID : SV_InstanceID)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 localPos = input.positionOS.xyz * _PixelSize;
                float2 pixelCenter = float2(0, 0);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                PixelData data = positionBuffer[instanceID];
                pixelCenter = data.position;
                localPos.xy += pixelCenter;
            #endif

                output.uv = pixelCenter * _UVScale + _UVOffset;
                
                float3 worldPos = mul(_ObjectToWorldMatrix, float4(localPos, 1.0)).xyz;
                output.positionCS = TransformWorldToHClip(worldPos);

                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                
                half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                clip(albedoAlpha.a - 0.5);
                
                return 0;
            }
            ENDHLSL
        }
    }
}