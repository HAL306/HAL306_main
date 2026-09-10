Shader "Custom/DotInstanced"
{
    Properties
    {
        _MainTex ("Main Texture (Baked)", 2D) = "white" {}
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Strength", Range(0, 2)) = 1.0
        _HasBumpMap ("Has Normal Map", Float) = 0.0
        _DotSize ("Dot Size", Float) = 0.125
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.01
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

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
                float _DotSize;
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
                float3 positionWS : TEXCOORD1;
                float3 geomNormalWS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float isEdge : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            void setup() {}

            Varyings vert(Attributes input, uint instanceID : SV_InstanceID)
            {
                Varyings output;
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

                output.positionCS = TransformWorldToHClip(worldPos);
                output.positionWS = worldPos;
                output.uv = TRANSFORM_TEX(dotUV, _MainTex);
                output.color = lerp(dotColor, dotColor * float4(0.85, 0.85, 0.85, 1.0), isEdge);
                output.geomNormalWS = normalize(mul((float3x3)_CustomLocalToWorld, dotLocalNormal));
                output.isEdge = isEdge;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 finalCol = texCol * input.color;
                
                if (finalCol.a < _Cutoff)
                {
                    discard;
                }

                half3 forwardWS = normalize(mul((float3x3)_CustomLocalToWorld, float3(0, 0, -1)));
                half3 rightWS   = normalize(mul((float3x3)_CustomLocalToWorld, float3(1, 0, 0)));
                half3 upWS      = normalize(mul((float3x3)_CustomLocalToWorld, float3(0, 1, 0)));

                half3 finalNormalWS;

                if (_HasBumpMap > 0.5)
                {
                    half4 normalSample = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv);
                    half3 texNormalTS = normalSample.rgb * 2.0 - 1.0;
                    texNormalTS.xy *= _BumpScale;

                    half3 worldNormalFromTex = normalize(texNormalTS.x * rightWS + texNormalTS.y * upWS + texNormalTS.z * forwardWS);
                    finalNormalWS = normalize(lerp(worldNormalFromTex, input.geomNormalWS, input.isEdge * 0.75));
                }
                else
                {
                    finalNormalWS = input.geomNormalWS;
                }

                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(finalNormalWS, mainLight.direction));
                half3 lighting = mainLight.color * (NdotL * mainLight.shadowAttenuation + 0.35);

                finalCol.rgb *= lighting;
                return finalCol;
            }
            ENDHLSL
        }
    }
}