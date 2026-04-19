Shader "Custom/SimonSaysGlow"
{
    Properties
    {
        _MainTex     ("Texture",      2D)    = "white" {}
        _GlowColor   ("Glow Color",   Color) = (1,1,1,1)
        _GlowStrength("Glow Strength",Float) = 0.0
        _GlowRadius  ("Glow Radius",  Float) = 0.18
        _PulseSpeed  ("Pulse Speed",  Float) = 4.0
        _Time2       ("Time",         Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        ZWrite Off
        Blend One OneMinusSrcAlpha
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
            fixed4    _GlowColor;
            float     _GlowStrength;
            float     _GlowRadius;
            float     _PulseSpeed;
            float     _Time2;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // Distance from center — drives the bloom falloff
                float2 center = i.uv - 0.5;
                float  dist   = length(center);

                // Smooth radial glow that bleeds outward
                float glow = _GlowStrength *
                             smoothstep(_GlowRadius, 0.0, dist) *
                             (0.85 + 0.15 * sin(_Time2 * _PulseSpeed));

                // Additive bloom on top of the image
                col.rgb += _GlowColor.rgb * glow;
                col.rgb  = min(col.rgb, 2.0); // soft clamp — no blown-out white

                // Premultiply alpha for Blend One OneMinusSrcAlpha
                col.rgb *= col.a;
                return col;
            }
            ENDCG
        }
    }
}