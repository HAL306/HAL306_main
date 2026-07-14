Shader "Custom/DotInstanced"
{
    Properties
    {
        [MainTexture] _MainTex ("Texture", 2D) = "white" {}
        [MainColor] _Color ("Tint Color", Color) = (1, 1, 1, 1)
        _UVScale ("UV Scale", Vector) = (1, 1, 0, 0)
        _UVOffset ("UV Offset", Vector) = (0, 0, 0, 0)
        [HideInInspector] _PixelSize ("Pixel Size", Float) = 0.05
    }
    SubShader
    {
        Tags { 
            "RenderType"="TransparentCutout" 
            "RenderPipeline"="UniversalPipeline" 
            "Queue"="AlphaTest"
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // ★ ここを追加：C#・ComputeShaderと同じデータ構造を定義
            struct PixelData
            {
                float2 position;
                float3 normal;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _PixelSize;
                float2 _UVScale;
                float2 _UVOffset;
                float4x4 _ObjectToWorldMatrix;
            CBUFFER_END

            // ★ 変更：float2 から PixelData の配列として受け取る
            StructuredBuffer<PixelData> positionBuffer;

            void setup() {}

            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                float3 localPos = v.vertex.xyz * _PixelSize;
                float2 pixelCenter = float2(0, 0);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                // ★ 変更：PixelDataとして取り出し、位置情報だけを使う（法線は無視）
                PixelData data = positionBuffer[instanceID];
                pixelCenter = data.position;
                localPos.xy += pixelCenter;
            #endif

                o.uv = pixelCenter * _UVScale + _UVOffset;
                
                float3 worldPos = mul(_ObjectToWorldMatrix, float4(localPos, 1.0)).xyz;
                o.pos = TransformWorldToHClip(worldPos);

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 finalColor = texColor * _Color;
                
                // アルファが0.5未満を切り抜く（透明対応）
                clip(finalColor.a - 0.5);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}