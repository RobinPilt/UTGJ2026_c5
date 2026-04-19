Shader "Custom/MenuButtonBlob"
{
    Properties
    {
        _Color          ("Blob Color",      Color) = (0.05, 0.05, 0.08, 1.0)
        _Radius         ("Radius",          Float) = 0.35
        _Softness       ("Edge Softness",   Float) = 0.35
        _NoiseScale     ("Noise Scale",     Float) = 3.5
        _NoiseStrength  ("Noise Strength",  Float) = 0.08
        _BreathSpeed    ("Breath Speed",    Float) = 0.8
        _BreathAmount   ("Breath Amount",   Float) = 0.015
        _ChromaStr      ("Chroma Str",      Float) = 0.004
        _Time2          ("Time",            Float) = 0.0
        _HoverGlow      ("Hover Glow",      Float) = 0.0
        _GlowColor      ("Glow Color",      Color) = (0.4, 0.3, 0.2, 1.0)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            fixed4 _Color;
            fixed4 _GlowColor;
            float  _Radius;
            float  _Softness;
            float  _NoiseScale;
            float  _NoiseStrength;
            float  _BreathSpeed;
            float  _BreathAmount;
            float  _ChromaStr;
            float  _Time2;
            float  _HoverGlow;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                return o;
            }

            // ── Smooth noise ──────────────────────────────────────────
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float smoothNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f); // smoothstep

                float a = hash(i);
                float b = hash(i + float2(1,0));
                float c = hash(i + float2(0,1));
                float d = hash(i + float2(1,1));

                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // ── 1. Aspect-correct distance from center ─────────────
                float2 centered = i.uv - 0.5;
                // Stretch horizontally — blob is wider than tall
                centered.x     *= 0.55;
                centered.y     *= 0.9;

                // ── 2. Animated noise warps the blob edge ──────────────
                float2 noiseUV  = i.uv * _NoiseScale + _Time2 * 0.12;
                float  noiseUV2 = smoothNoise(noiseUV + float2(1.7, 9.2));
                float  noise    = smoothNoise(noiseUV) * 0.6 + noiseUV2 * 0.4;

                // ── 3. Breathing — radius pulses slowly ────────────────
                float breath = sin(_Time2 * _BreathSpeed) * _BreathAmount;

                // ── 4. Final distance with noise and breath ────────────
                float dist   = length(centered);
                float wobble = noise * _NoiseStrength;
                float radius = _Radius + breath + wobble;

                // ── 5. Soft edge alpha ─────────────────────────────────
                float alpha = 1.0 - smoothstep(
                    radius - _Softness,
                    radius,
                    dist
                );

                // ── 6. Hover glow — rim brightens on hover ─────────────
                // Inner rim glow using inverted distance
                float rimDist  = smoothstep(0.0, radius * 0.7, dist);
                float rimGlow  = rimDist * (1.0 - rimDist) * 4.0;
                float3 glowCol = _GlowColor.rgb * rimGlow * _HoverGlow;

                // ── 7. Subtle chromatic fringe at edge ─────────────────
                float edgeFactor = smoothstep(radius - _Softness * 1.5,
                                              radius - _Softness * 0.3,
                                              dist);
                float3 col = _Color.rgb + glowCol;
                col.r     += edgeFactor * _ChromaStr * 2.0;
                col.b     -= edgeFactor * _ChromaStr;

                // ── 8. Dark center gradient — not fully flat ───────────
                float centerDark = 1.0 - dist * 0.4;
                col             *= centerDark;

                return fixed4(col, alpha * _Color.a);
            }
            ENDCG
        }
    }
}