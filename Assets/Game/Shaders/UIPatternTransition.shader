// Screen transition: a tiled pattern that grows across the screen in the order given by a gradient texture.
// Ported from "Transition with a pattern texture" by Ultipuk (CC0):
// https://godotshaders.com/shader/transition-with-a-pattern-texture/
Shader "FUR/UI Pattern Transition"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tile Color", Color) = (1,1,1,1)
        _Progress ("Progress", Range(0, 1)) = 0
        _Width ("Transition Width", Range(0.01, 1)) = 0.5
        [NoScaleOffset] _TileTex ("Tile Pattern (alpha)", 2D) = "white" {}
        _TilePixelSize ("Tile Size (pixels at 1080p)", Float) = 72
        _TileGrownScale ("Tile Grown Scale", Range(0, 16)) = 2
        [NoScaleOffset] _TransitionTex ("Transition Order (red: dark goes first)", 2D) = "gray" {}
        [Toggle] _Invert ("Invert Order", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Overlay" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "UnityCG.cginc"

            struct Attributes { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 position : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            sampler2D _TileTex, _TransitionTex;
            fixed4 _Color;
            float _Progress, _Width, _TilePixelSize, _TileGrownScale, _Invert;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.color = input.color * _Color;
                output.uv = input.uv;
                return output;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                // Tiles stay the same size on screen at any resolution.
                float tilePixels = max(_TilePixelSize * _ScreenParams.y / 1080.0, 1.0);
                float2 tileUV = frac(input.position.xy / tilePixels);

                float part = _Progress * (1.0 + 2.0 * _Width) - _Width;
                float order = tex2D(_TransitionTex, input.uv).r;
                if (_Invert > 0.5) order = 1.0 - order;
                float window = saturate(_Width + (part - order) / _Width);

                tileUV = (tileUV * 2.0 - 1.0) / max(window * _TileGrownScale, 1e-4);
                tileUV = (tileUV + 1.0) * 0.5;
                float inside = step(0.0, tileUV.x) * step(tileUV.x, 1.0) * step(0.0, tileUV.y) * step(tileUV.y, 1.0);

                fixed4 tile = tex2D(_TileTex, saturate(tileUV)) * input.color;
                tile.a *= inside;
                return tile;
            }
            ENDCG
        }
    }
    Fallback Off
}
