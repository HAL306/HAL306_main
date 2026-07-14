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
                float3 worldPosition;
                float3 normal;
                float2 localPosition;
                float3 axisX;
                float3 axisY;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _PixelSize;
                float2 _UVScale;
                float2 _UVOffset;
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

                float3 localQuadOffset = input.positionOS.xyz * _PixelSize;
                float3 worldCenter = float3(0, 0, 0);
                float3 worldNormal = float3(0, 0, 1);
                float2 localCenter = float2(0, 0);
                float3 axisX = float3(1, 0, 0);
                float3 axisY = float3(0, 1, 0);
                float3 axisZ = float3(0, 0, 1);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                PixelData data = positionBuffer[instanceID];
                worldCenter = data.worldPosition; 
                worldNormal = data.normal;
                localCenter = data.localPosition;
                axisX = data.axisX;
                axisY = data.axisY;
            #endif

                axisZ = normalize(cross(axisX, axisY));
                float3 rotatedOffset = localQuadOffset.x * axisX + localQuadOffset.y * axisY + localQuadOffset.z * axisZ;

                float3 finalWorldPos = worldCenter + rotatedOffset;
                
                output.positionWS = finalWorldPos;
                output.positionCS = TransformWorldToHClip(finalWorldPos);
                output.normalWS = worldNormal;

                float3 t = float3(1, 0, 0);
                if (abs(worldNormal.x) > 0.999) 
                {
                    t = float3(0, 1, 0);
                }
                t = normalize(t - worldNormal * dot(worldNormal, t));
                float3 b = cross(worldNormal, t);

                output.tangentWS = t;
                output.bitangentWS = b;

                output.uv = localCenter * _UVScale + _UVOffset;

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

                return UniversalFragmentPBR(inputData, surfaceData);
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
                float3 worldPosition;
                float3 normal;
                float2 localPosition;
                float3 axisX;
                float3 axisY;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _PixelSize;
                float2 _UVScale;
                float2 _UVOffset;
            CBUFFER_END

            StructuredBuffer<PixelData> positionBuffer;

            void setup() {}

            Varyings ShadowPassVertex(Attributes input, uint instanceID : SV_InstanceID)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 localQuadOffset = input.positionOS.xyz * _PixelSize;
                float3 worldCenter = float3(0, 0, 0);
                float2 localCenter = float2(0, 0);
                float3 axisX = float3(1, 0, 0);
                float3 axisY = float3(0, 1, 0);
                float3 axisZ = float3(0, 0, 1);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                PixelData data = positionBuffer[instanceID];
                worldCenter = data.worldPosition;
                localCenter = data.localPosition;
                axisX = data.axisX;
                axisY = data.axisY;
            #endif

                axisZ = normalize(cross(axisX, axisY));
                float3 rotatedOffset = localQuadOffset.x * axisX + localQuadOffset.y * axisY + localQuadOffset.z * axisZ;

                float3 finalWorldPos = worldCenter + rotatedOffset;
                output.positionCS = TransformWorldToHClip(finalWorldPos);
                
                output.uv = localCenter * _UVScale + _UVOffset;

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