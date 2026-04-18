Shader "UI/EdgeFade"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FadeWidth ("Fade Width", Range(0, 0.5)) = 0.1
        [PerRendererData] _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f    { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _Color;
            float     _FadeWidth;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                o.color  = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                float fade = smoothstep(0, _FadeWidth, i.uv.x)
                           * smoothstep(0, _FadeWidth, 1.0 - i.uv.x);
                col.a *= fade;
                return col;
            }
            ENDCG
        }
    }
}
