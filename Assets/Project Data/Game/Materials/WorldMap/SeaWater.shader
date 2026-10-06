// World Map 3D sea: a real wavy water surface. The swell moves the mesh up and down, the lighting
// comes from the wave slopes (sun glints, light and shade on each wave), and the painted ocean
// picture is used as the water colour so it still matches the map art.
// Wave settings come from WorldMapSea3D (globals), so boats ride exactly the same waves.
Shader "Conveyor Chef/Sea/Water"
{
    Properties
    {
        _MainTex ("Ocean Picture (tiles)", 2D) = "white" {}
        _TileSize ("Picture Tile Size (map units)", Float) = 731
        _Tint ("Tint", Color) = (1,1,1,1)
        _Flow ("Picture Flow (map units / s)", Vector) = (4, 2.5, 0, 0)
        _Distort ("Picture Wobble", Range(0, 0.05)) = 0.015
        _Shade ("Wave Light and Shade", Range(0, 2)) = 0.9
        _Specular ("Sun Glints", Range(0, 3)) = 0.6
        _Gloss ("Glint Sharpness", Range(8, 4096)) = 1000
        _GlintTilt ("Glint Facing (x, z tilt of the waves that sparkle)", Vector) = (-0.13, 0.09, 0, 0)
        _Ripple ("Small Ripples", Range(0, 2)) = 0.7
        _Foam ("Crest Foam", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _TileSize;
            fixed4 _Tint;
            float4 _Flow;
            float _Distort, _Shade, _Specular, _Gloss, _Ripple, _Foam;
            float4 _GlintTilt;

            // set by WorldMapSea3D
            float _SeaTime;
            float _SeaFlatten;          // sin(view angle): turns sea z back into map y
            float4 _SeaSunDir;          // toward the sun
            float4 _SeaViewDir;         // toward the camera
            float4 _SeaWaves[4];        // xy = direction * 2pi / length, z = height, w = speed (rad/s)

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; };

            float Height(float2 xz)
            {
                float h = 0;
                for (int i = 0; i < 4; i++)
                    h += _SeaWaves[i].z * sin(dot(_SeaWaves[i].xy, xz) + _SeaWaves[i].w * _SeaTime);
                return h;
            }

            v2f vert(appdata v)
            {
                v2f o;
                float3 w = mul(unity_ObjectToWorld, v.vertex).xyz;
                w.y += Height(w.xz);
                o.wpos = w;
                o.pos = UnityWorldToClipPos(w);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 xz = i.wpos.xz;
                float t = _SeaTime;

                // slope of the swell + fine ripples (lighting only)
                float2 grad = 0;
                float total = 0;
                for (int k = 0; k < 4; k++)
                {
                    float ph = dot(_SeaWaves[k].xy, xz) + _SeaWaves[k].w * t;
                    grad += _SeaWaves[k].xy * (_SeaWaves[k].z * cos(ph));
                    total += _SeaWaves[k].z;
                }
                float2 r1 = float2(0.071, 0.043), r2 = float2(-0.052, 0.088), r3 = float2(0.12, -0.09);
                grad += _Ripple * (r1 * 0.9 * cos(dot(r1, xz) + 2.3 * t) +
                                   r2 * 0.7 * cos(dot(r2, xz) + 2.9 * t) +
                                   r3 * 0.35 * cos(dot(r3, xz) + 3.7 * t));
                float3 n = normalize(float3(-grad.x, 1.0, -grad.y));

                // painted ocean as the water colour, in map space so it looks like the 2D map
                float2 mapPos = float2(xz.x, xz.y * _SeaFlatten);
                float2 uv = (mapPos + _Flow.xy * t) / _TileSize + n.xz * _Distort;
                fixed3 albedo = tex2D(_MainTex, uv).rgb * _Tint.rgb;

                float3 L = normalize(_SeaSunDir.xyz);
                // sparkles where a wave faces this direction (an ortho sea view never mirrors the sun otherwise)
                float3 H = normalize(float3(_GlintTilt.x, 1.0, _GlintTilt.y));

                // light and shade relative to flat water, so the picture keeps its colours
                float shade = 1.0 + _Shade * (dot(n, L) - L.y);
                float flat = pow(H.y, _Gloss);      // calm water gives no glint
                float glint = saturate((pow(saturate(dot(n, H)), _Gloss) - flat) / (1.0 - flat)) * _Specular;

                float h = Height(xz);
                float foam = smoothstep(0.55 * total, 0.95 * total, h) * _Foam;

                fixed3 col = albedo * shade + glint + foam;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
