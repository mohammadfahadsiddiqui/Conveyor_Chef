// World Map 3D sea effects: wakes, boat and cloud shadows, smoke and whale spray.
// One mesh: atlas texture (boat_fx.png, uv0) times a per-quad tint (uv1 = r, g, b, a).
// Queued just before transparents: this project's URP renderer draws transparent queues only on
// the TransparentFX and UI layers.
Shader "Conveyor Chef/Sea/Effects"
{
    Properties
    {
        _MainTex ("Effects Atlas", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue"="Geometry+450" "RenderType"="Transparent" "IgnoreProjector"="True" }   // after the water and models; inside URP's opaque stage, which draws every layer
        Blend SrcAlpha OneMinusSrcAlpha, Zero One     // keep the sea image opaque
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 tint : TEXCOORD1; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 tint : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.tint = v.tint;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                return fixed4(tex.rgb * i.tint.rgb, tex.a * i.tint.a);
            }
            ENDCG
        }
    }
}
