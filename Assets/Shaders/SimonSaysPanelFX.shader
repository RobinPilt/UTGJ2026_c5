Shader "Custom/SimonSaysPanelFX"
{
    Properties
    {
        _MainTex        ("Texture",           2D)    = "white" {}
        _ScanlineStrength("Scanline Strength", Float) = 0.15
        _ScanlineCount  ("Scanline Count",    Float) = 180.0
        _CurvatureX     ("Curvature X",       Float) = 0.05
        _CurvatureY     ("Curvature Y",       Float) = 0.08
        _VignetteStr    ("Vignette Strength", Float) = 0.45
        _VignetteSmooth ("Vignette Smoothness",Float) = 0.35
        _GrainStrength  ("Grain Strength",    Float) = 0.06
        _GrainScale     ("Grain Scale",       Float) = 300.0
        _Time2          ("Time",              Float) = 0.0
        _AberrationStr  ("Aberration Str",    Float) = 0.0
        _FlashStr       ("Flash Strength",    Float) = 0.0
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

            sampler2D _MainTex;
            float _ScanlineStrength;
            float _ScanlineCount;
            float _CurvatureX;
            float _CurvatureY;
            float _VignetteStr;
            float _VignetteSmooth;
            float _GrainStrength;
            float _GrainScale;
            float _Time2;
            float _AberrationStr;
            float _FlashStr;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                return o;
            }

            // ── Pseudo-random noise ───────────────────────────────────
            float rand(float2 co)
            {
                return frac(sin(dot(co, float2(127.1, 311.7))) * 43758.5453);
            }

            // ── CRT barrel distortion ─────────────────────────────────
            float2 CRTCurve(float2 uv, float cx, float cy)
            {
                uv = uv * 2.0 - 1.0;
                uv.x *= 1.0 + uv.y * uv.y * cx;
                uv.y *= 1.0 + uv.x * uv.x * cy;
                return uv * 0.5 + 0.5;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // ── 1. Barrel curve ───────────────────────────────────────────
                float2 uv = CRTCurve(i.uv, _CurvatureX, _CurvatureY);

                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                    return fixed4(0, 0, 0, 1);

                // ── 2. Chromatic aberration ───────────────────────────────────
                float2 dir  = (uv - 0.5) * _AberrationStr;
                float  rCol = tex2D(_MainTex, uv + dir).r;
                float  gCol = tex2D(_MainTex, uv       ).g;
                float  bCol = tex2D(_MainTex, uv - dir ).b;
                fixed4 col  = fixed4(rCol, gCol, bCol, tex2D(_MainTex, uv).a);

                // ── 3. Moving scanlines ───────────────────────────────────────
                // Time offset scrolls lines downward continuously
                float scrollUV   = uv.y + _Time2 * 0.04;
                float scanWave   = sin(scrollUV * _ScanlineCount * 3.14159);

                // Sharpen the wave into distinct dark bands using power
                float scanSharp  = pow(abs(scanWave), 0.4);
                float scan       = 1.0 - _ScanlineStrength * (1.0 - scanSharp);
                col.rgb         *= scan;

                // Secondary finer static lines for texture depth
                float fineLine   = sin(uv.y * _ScanlineCount * 6.28318);
                col.rgb         *= 1.0 - (_ScanlineStrength * 0.3) * (fineLine * fineLine);

                // ── 4. Film grain ─────────────────────────────────────────────
                float2 grainUV = uv * _GrainScale;
                float  grain   = rand(grainUV + frac(_Time2 * 0.07));

                // Grain flickers rapidly — use faster time offset
                float  grain2  = rand(grainUV * 1.3 + frac(_Time2 * 0.13 + 0.5));
                col.rgb       += (lerp(grain, grain2, frac(_Time2 * 8.0)) - 0.5)
                                 * _GrainStrength;

                // ── 5. Vignette ───────────────────────────────────────────────
                float2 vigUV = uv - 0.5;
                float  vDist = dot(vigUV, vigUV);
                float  vign  = smoothstep(_VignetteStr,
                                           _VignetteStr - _VignetteSmooth,
                                           vDist);
                col.rgb *= vign;

                // ── 6. Subtle phosphor flicker (entire screen dims slightly) ──
                float flicker = 1.0 - 0.025 * sin(_Time2 * 73.0);
                col.rgb      *= flicker;

                // ── 7. White flash (wrong press) ──────────────────────────────
                col.rgb = lerp(col.rgb, float3(1,1,1), _FlashStr);

                col.a = tex2D(_MainTex, uv).a;
                return col;
            }
            ENDCG
        }
    }
}