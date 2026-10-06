// World Map 3D sea models (boats, whales): flat-shaded low-poly look, lit by the same sun as
// the water, with a cartoon ink outline to match the painted map.
// Colours come from a small palette texture (uv0), lighting from the face normals; the outline
// uses smooth normals (uv1) and sits slightly behind the model, so it only shows at the edges.
// Palette alpha below 1 marks thin double-sided parts (sails, fins): kept bright.
Shader "Conveyor Chef/Sea/Toon Model"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Palette (set by WorldMapSea3D)", 2D) = "white" {}
        _Ambient ("Ambient", Range(0, 1)) = 0.58
        _Diffuse ("Sun", Range(0, 1)) = 0.5
        _Rim ("Rim Light", Range(0, 1)) = 0.15
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

        Pass
        {
            Name "Main"
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Ambient, _Diffuse, _Rim;
            float4 _SeaSunDir;
            float4 _SeaViewDir;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float3 normal : TEXCOORD0; float2 uv : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 base = tex2D(_MainTex, i.uv);
                float3 n = normalize(i.normal);
                float3 V = normalize(_SeaViewDir.xyz);
                if (dot(n, V) < 0) n = -n;      // inside of thin parts faces the camera too

                float3 L = normalize(_SeaSunDir.xyz);
                float rim = 1.0 - saturate(dot(n, V));
                float lit = _Ambient + _Diffuse * saturate(dot(n, L)) + _Rim * rim * rim;
                if (base.a < 0.9) lit = max(lit, 0.9);

                return fixed4(saturate(base.rgb * lit), 1);
            }
            ENDCG
        }
    }
}
