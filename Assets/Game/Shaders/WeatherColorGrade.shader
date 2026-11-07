Shader "Hidden/FUR/Weather Color Grade"
{
    Properties { _MainTex ("Source", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Exposure, _Contrast, _Saturation, _SplitToning, _Haze, _Vignette, _Aspect;
            float4 _ColorFilter, _ShadowTint, _HighlightTint, _HazeColor, _VignetteColor;

            fixed4 frag(v2f_img input) : SV_Target
            {
                float4 source = tex2D(_MainTex, input.uv);
                float3 color = source.rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                    color = LinearToGammaSpace(color);
                #endif
                color *= exp2(_Exposure);
                color = (color - .5) * (1 + _Contrast) + .5;
                float luminance = dot(color, float3(.2126, .7152, .0722));
                color = lerp(luminance.xxx, color, _Saturation);
                color *= _ColorFilter.rgb;
                float3 tone = lerp(_ShadowTint.rgb, _HighlightTint.rgb, smoothstep(.1, .85, luminance));
                color *= lerp(float3(1, 1, 1), tone, _SplitToning);
                color = lerp(color, _HazeColor.rgb, _Haze);
                float2 position = (input.uv - .5) * float2(_Aspect, 1);
                float vignette = smoothstep(.25, .95, length(position)) * _Vignette;
                color = saturate(lerp(color, _VignetteColor.rgb, vignette));
                #ifndef UNITY_COLORSPACE_GAMMA
                    color = GammaToLinearSpace(color);
                #endif
                return float4(color, source.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
