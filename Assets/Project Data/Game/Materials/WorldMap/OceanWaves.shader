// World Map sea: the tiled ocean picture slowly flows, ripples and shimmers.
// A UI shader (works with Image, Mask and RectMask2D). All motion is periodic per tile,
// so the tiling stays seamless. Cost: one texture read per pixel, like UI/Default.
Shader "Conveyor Chef/UI/Ocean Waves"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _WaveStrength ("Wave Strength", Range(0, 0.03)) = 0.007
        _WaveFrequency ("Waves Per Tile", Float) = 3
        _WaveSpeed ("Wave Speed", Float) = 0.9
        _FlowSpeed ("Flow (tiles per second, XY)", Vector) = (0.006, 0.0035, 0, 0)
        _Shimmer ("Shimmer", Range(0, 0.3)) = 0.08

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float _WaveStrength;
            float _WaveFrequency;
            float _WaveSpeed;
            float4 _FlowSpeed;
            float _Shimmer;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                const float TAU = 6.2831853;
                float t = _Time.y;
                float2 uv = IN.texcoord;

                // whole waves per tile keep the tiling seamless
                float k = max(1.0, round(_WaveFrequency));
                float2 flow = uv + _FlowSpeed.xy * t;
                float2 ripple;
                ripple.x = sin(flow.y * k * TAU + t * _WaveSpeed);
                ripple.y = cos(flow.x * k * TAU + t * _WaveSpeed * 0.83);
                float2 sampleUV = frac(flow + ripple * _WaveStrength);

                // gradients from the smooth uv: no seam line where frac wraps
                half4 color = (tex2Dgrad(_MainTex, sampleUV, ddx(uv), ddy(uv)) + _TextureSampleAdd) * IN.color;

                // soft moving light bands on the water
                float s = sin((uv.x + uv.y) * 3.0 * TAU + t * 1.1) * sin((uv.x - uv.y) * 2.0 * TAU - t * 0.7);
                color.rgb += s * max(s, 0.0) * _Shimmer;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}
