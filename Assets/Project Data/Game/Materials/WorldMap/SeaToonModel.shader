// World Map 3D sea models (boats, whales): flat-shaded low-poly look, lit by the same sun as
// the water. Colours come from a small palette texture (uv0), lighting from the face normals.
// Palette alpha below 1 marks thin double-sided parts (sails, fins): kept bright.
// The ink outline is a second material (Sea/Toon Outline): URP draws one untagged pass per shader.
Shader "Conveyor Chef/Sea/Toon Model"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Palette (set by WorldMapSea3D)", 2D) = "white" {}
        _Ambient ("Ambient", Range(0, 1)) = 0.58
        _Diffuse ("Sun", Range(0, 1)) = 0.5
        _Rim ("Rim Light", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

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
