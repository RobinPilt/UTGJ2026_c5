Shader "Custom/URP_GlowOutline"
{
    Properties
    {
        [HDR] _OutlineColor ("Glow Color", Color) = (0, 1, 1, 1)
        _OutlineThickness ("Thickness", Range(0.0, 0.2)) = 0.02
    }
    SubShader
    {
        // We render just after normal geometry to ensure it draws correctly
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+1" }
        LOD 100

        Pass
        {
            Name "Outline"
            // THE SECRET SAUCE: Cull Front means we only draw the inside of this expanded mesh,
            // so it sits perfectly behind your actual character mesh.
            Cull Front 
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineThickness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Push the vertices outward along their local normals
                float3 expandedPosition = input.positionOS.xyz + (input.normalOS * _OutlineThickness);
                
                // Convert to clip space for the camera
                output.positionCS = TransformObjectToHClip(expandedPosition);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Output the pure HDR color
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}