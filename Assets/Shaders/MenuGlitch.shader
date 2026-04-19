Shader "Custom/MenuGlitch"
{
    Properties
    {
        _MainTex            ("Texture",             2D)    = "white" {}
        _ChromaStrength     ("Chroma Strength",     Float) = 0.005
        _GlitchIntensity    ("Glitch Intensity",    Float) = 0.0
        _GlitchSpeed        ("Glitch Speed",        Float) = 1.0
        _BlockSize          ("Block Size",          Float) = 0.05
        _ScanlineStrength   ("Scanline Strength",   Float) = 0.04
        _Time2              ("Time",                Float) = 0.0
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
            float _ChromaStrength;
            float _GlitchIntensity;
            float _GlitchSpeed;
            float _BlockSize;
            float _ScanlineStrength;
            float _Time2;

            // ── Hash functions ────────────────────────────────────────
            float hash(float n) { return frac(sin(n) * 43758.5453); }
            float hash2(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float  t  = _Time2 * _GlitchSpeed;

                // ── 1. Horizontal block glitch ────────────────────────
                // Quantise Y into blocks — each block can shift horizontally
                float blockY    = floor(uv.y / _BlockSize);
                float blockTime = floor(t * 3.0);

                // Only some blocks glitch at any given time
                float glitchSeed   = hash2(float2(blockY, blockTime));
                float shouldGlitch = step(0.92, glitchSeed); // 8% of blocks glitch

                // How far to shift this block
                float shift = (hash2(float2(blockY + 0.5, blockTime)) - 0.5)
                              * _GlitchIntensity * shouldGlitch;

                uv.x += shift;

                // ── 2. Occasional full-row tear ───────────────────────
                float tearY    = floor(uv.y * 80.0);
                float tearTime = floor(t * 8.0);
                float tearSeed = hash2(float2(tearY, tearTime));
                float tear     = step(0.97, tearSeed) * _GlitchIntensity;

                uv.x += (hash(tearSeed) - 0.5) * tear * 0.3;

                // ── 3. Chromatic aberration ───────────────────────────
                // Aberration intensifies in glitched blocks
                float chromaBoost = 1.0 + shouldGlitch * abs(shift) * 8.0;
                float chroma      = _ChromaStrength * chromaBoost;

                float2 rUV = float2(uv.x + chroma, uv.y);
                float2 gUV = uv;
                float2 bUV = float2(uv.x - chroma, uv.y);

                // Clamp so we don't sample outside the texture
                rUV = clamp(rUV, 0.0, 1.0);
                bUV = clamp(bUV, 0.0, 1.0);

                float r = tex2D(_MainTex, rUV).r;
                float g = tex2D(_MainTex, gUV).g;
                float b = tex2D(_MainTex, bUV).b;
                float a = tex2D(_MainTex, gUV).a;

                fixed4 col = fixed4(r, g, b, a);

                // ── 4. Subtle scanlines ───────────────────────────────
                float scanWave = sin(i.uv.y * 400.0);
                col.rgb       *= 1.0 - _ScanlineStrength * (scanWave * scanWave);

                // ── 5. Occasional full flash ──────────────────────────
                float flashTime = floor(t * 12.0);
                float flash     = step(0.98, hash(flashTime)) * _GlitchIntensity * 0.15;
                col.rgb        += flash;

                return col;
            }
            ENDCG
        }
    }
}