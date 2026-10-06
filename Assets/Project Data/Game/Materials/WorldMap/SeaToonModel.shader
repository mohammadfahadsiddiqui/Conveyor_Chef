// World Map 3D sea models (boats, whales): flat-shaded low-poly look from the vertex colours,
// lit by the same sun as the water, with a cartoon ink outline to match the painted map.
// Vertex colour alpha below 1 marks thin double-sided parts (sails, fins): kept bright.
Shader "Conveyor Chef/Sea/Toon Model"
{
    Properties
    {
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
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _OutlineWidth;
            fixed4 _OutlineColor;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(float4(v.vertex.xyz + v.normal * _OutlineWidth, 1));
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
            #pragma target 3.0
            #include "UnityCG.cginc"

            float _Ambient, _Diffuse, _Rim;
            float4 _SeaSunDir;
            float4 _SeaViewDir;

            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; fixed4 color : COLOR; };

            v2f vert(appdata v)
            {
                v2f o;
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityWorldToClipPos(o.wpos);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // flat face normal from screen derivatives, turned to face the camera
                float3 n = normalize(cross(ddy(i.wpos), ddx(i.wpos)));
                float3 V = normalize(_SeaViewDir.xyz);
                if (dot(n, V) < 0) n = -n;

                float3 L = normalize(_SeaSunDir.xyz);
                float rim = 1.0 - saturate(dot(n, V));
                float lit = _Ambient + _Diffuse * saturate(dot(n, L)) + _Rim * rim * rim;
                if (i.color.a < 0.9) lit = max(lit, 0.9);

                return fixed4(saturate(i.color.rgb * lit), 1);
            }
            ENDCG
        }
    }
}
