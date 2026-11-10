// Additive glow for small light particles such as fireflies. Intensity pushes the colour above 1
// so the particles stay bright under dark post-processing and can trigger Bloom.
Shader "FUR/Particle Glow Additive"
{
    Properties
    {
        _MainTex ("Glow Texture", 2D) = "white" {}
        [HDR] _Color ("Tint", Color) = (1, 0.95, 0.45, 1)
        _Intensity ("Intensity", Range(0, 16)) = 4
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Intensity;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 glow = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color * _Color;
                return half4(glow.rgb * _Intensity, glow.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
