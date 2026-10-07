// World Map 3D sea models: cartoon ink outline. Drawn as the model's second material; uses smooth
// normals (uv1) and sits slightly behind the model, so it only shows round the edges.
Shader "Conveyor Chef/Sea/Toon Outline"
{
    Properties
    {
        _OutlineWidth ("Outline Width (model units)", Range(0, 0.1)) = 0.035
        _OutlineColor ("Outline Colour", Color) = (0.15, 0.13, 0.2, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "Outline"
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _OutlineWidth;
            fixed4 _OutlineColor;
            float4 _SeaViewDir;

            struct appdata { float4 vertex : POSITION; float3 smooth : TEXCOORD1; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                float scale = length(unity_ObjectToWorld._m00_m10_m20);
                float3 w = mul(unity_ObjectToWorld, float4(v.vertex.xyz + v.smooth * _OutlineWidth, 1)).xyz;
                // pushed away from the camera: hidden behind the model except round its edges
                w -= normalize(_SeaViewDir.xyz) * (0.15 * scale);
                o.pos = UnityWorldToClipPos(w);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target { return _OutlineColor; }
            ENDCG
        }
    }
}
