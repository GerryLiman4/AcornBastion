Shader "FUR/Sprite Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width (Sprite Pixels)", Range(0,8)) = 2
        [Toggle] _PaperStyle ("Comic Paper Style", Float) = 1
        _PaperTint ("Paper Tint", Color) = (1,0.97,0.88,1)
        _InkColor ("Printed Dot Color", Color) = (0.025,0.022,0.018,1)
        _DotSpacing ("Dot Spacing (Sprite Pixels)", Range(2,12)) = 3.5
        _DotRadius ("Dot Radius (Sprite Pixels)", Range(0.1,1.5)) = 0.55
        _DotDensity ("Printed Dot Density", Range(0,1)) = 0.8
        _PaperGrain ("Paper Grain", Range(0,0.25)) = 0.055
        _EdgeRoughness ("Paper Edge Roughness", Range(0,0.3)) = 0.12
        [MaterialToggle] PixelSnap ("Pixel Snap", Float) = 0
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        [HideInInspector] _SpriteMetrics ("Size and Pivot in Pixels", Vector) = (1,1,0.5,0.5)
        [HideInInspector] _UVOrigin ("UV Origin", Vector) = (0,0,0,0)
        [HideInInspector] _UVPixelX ("UV per Pixel X", Vector) = (1,0,0,0)
        [HideInInspector] _UVPixelY ("UV per Pixel Y", Vector) = (0,1,0,0)
        [HideInInspector] _PixelsPerUnit ("Pixels per Unit", Float) = 100
        [HideInInspector] _SpriteFlip ("Sprite Flip", Vector) = (1,1,0,0)
        [HideInInspector] _SpriteOpacity ("Sprite Opacity", Float) = 1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "DisableBatching"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            sampler2D _MainTex, _AlphaTex;
            float4 _SpriteMetrics, _UVOrigin, _UVPixelX, _UVPixelY, _SpriteFlip;
            float _OutlineWidth, _PixelsPerUnit, _SpriteOpacity, _EnableExternalAlpha;
            float _PaperStyle, _DotSpacing, _DotRadius, _DotDensity, _PaperGrain, _EdgeRoughness;
            fixed4 _OutlineColor, _PaperTint, _InkColor;

            struct Attributes { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 position : SV_POSITION; float2 pixel : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                // A padded quad allows the outline to extend beyond tightly trimmed sprite meshes.
                output.pixel = input.uv * (_SpriteMetrics.xy + 2 * _OutlineWidth) - _OutlineWidth;
                float2 local = (output.pixel - _SpriteMetrics.zw) / _PixelsPerUnit;
                output.position = UnityObjectToClipPos(float4(local * _SpriteFlip.xy, 0, 1));
                #ifdef PIXELSNAP_ON
                    output.position = UnityPixelSnap(output.position);
                #endif
                return output;
            }

            float SpriteAlpha(float2 pixel)
            {
                // Do not sample adjacent animation frames, even when a sprite has no padding.
                float inside = step(0, pixel.x) * step(0, pixel.y)
                    * step(pixel.x, _SpriteMetrics.x) * step(pixel.y, _SpriteMetrics.y);
                pixel = clamp(pixel, 0.5, _SpriteMetrics.xy - 0.5);
                float2 uv = _UVOrigin.xy + pixel.x * _UVPixelX.xy + pixel.y * _UVPixelY.xy;
                float alpha = tex2D(_MainTex, uv).a;
                if (_EnableExternalAlpha > 0.5) alpha = tex2D(_AlphaTex, uv).r;
                return alpha * inside;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float PaperNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), f.x),
                    lerp(Hash(cell + float2(0, 1)), Hash(cell + 1), f.x), f.y);
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.pixel;
                // Anchor the print to the pivot so changing trimmed animation frames does not slide it.
                float2 paperPixel = p - _SpriteMetrics.zw;
                float style = saturate(_PaperStyle);
                float w = _OutlineWidth * (1 - style * _EdgeRoughness * PaperNoise(paperPixel * 0.35));
                float d = w * 0.70710678;
                float edge = max(SpriteAlpha(p + float2(w, 0)), SpriteAlpha(p - float2(w, 0)));
                edge = max(edge, max(SpriteAlpha(p + float2(0, w)), SpriteAlpha(p - float2(0, w))));
                edge = max(edge, max(SpriteAlpha(p + float2(d, d)), SpriteAlpha(p - float2(d, d))));
                edge = max(edge, max(SpriteAlpha(p + float2(d, -d)), SpriteAlpha(p + float2(-d, d))));
                float alpha = saturate(edge - SpriteAlpha(p)) * _OutlineColor.a * _SpriteOpacity;
                float2 printPixel = float2(paperPixel.x + paperPixel.y, paperPixel.y - paperPixel.x) * 0.70710678;
                float spacing = max(_DotSpacing, 2);
                float2 cell = floor(printPixel / spacing);
                float2 jitter = float2(Hash(cell + 17.1), Hash(cell + 38.7)) - 0.5;
                float2 dotOffset = (frac(printPixel / spacing) - 0.5) * spacing - jitter * spacing * 0.18;
                float distanceToDot = length(dotOffset);
                float radius = min(_DotRadius, spacing * 0.35) * lerp(0.85, 1.15, Hash(cell + 9.2));
                float antialias = max(fwidth(distanceToDot) * 0.5, 0.02);
                float dots = (1 - smoothstep(radius - antialias, radius + antialias, distanceToDot))
                    * step(Hash(cell + 71.4), _DotDensity) * style;
                float3 paperColor = _OutlineColor.rgb * lerp(float3(1, 1, 1), _PaperTint.rgb, style);
                paperColor *= 1 - style * _PaperGrain * PaperNoise(paperPixel * 1.7);
                float3 color = lerp(paperColor, _InkColor.rgb, dots * _InkColor.a);
                return fixed4(color * alpha, alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
