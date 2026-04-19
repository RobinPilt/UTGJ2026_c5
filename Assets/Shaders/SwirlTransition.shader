Shader "Custom/SwirlTransition"
{
    Properties
    {
        _MainTex      ("Texture",       2D)    = "white" {}
        _SwirlStrength("Swirl Strength",Float) = 0.0
        _Darkness     ("Darkness",      Float) = 0.0
        _Pull         ("Center Pull",   Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
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
            float _SwirlStrength;
            float _Darkness;
            float _Pull;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Move origin to screen center
                float2 uv   = i.uv - 0.5;
                float  dist = length(uv);
                float  angle = atan2(uv.y, uv.x);

                // Vortex: more rotation closer to center — the Disney whirlpool feel
                float swirl = _SwirlStrength / (dist * 4.0 + 0.4);
                angle += swirl;

                // Pull pixels toward center as darkness rises
                float pullDist = dist * (1.0 - _Pull * 0.6);

                // Reconstruct UV
                float2 swirlUV = float2(cos(angle), sin(angle)) * pullDist + 0.5;

                fixed4 col = tex2D(_MainTex, swirlUV);

                // Darken toward black
                col.rgb *= 1.0 - _Darkness;
                col.a    = 1.0;

                return col;
            }
            ENDCG
        }
    }
}