Shader "Transparent/Diffuse ZWrite" {
    Properties {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        _MinDistance("Minimum Distance", float) = 1
        _MaxDistance("Maximum Distance", float) = 2
        _StencilMask("Mask Layer", Range(0, 255)) = 1
       [Enum(CompareFunction)] _StencilComp("Mask Mode", Int) = 6
    }

    SubShader {
        Tags {"Queue"="Transparent"  "RenderType"="Transparent"}
        Cull Back
        LOD 200

         Stencil
         {
            Ref 255
            ReadMask[_StencilMask]
            Comp[_StencilComp]
         }

        // extra pass that renders to depth buffer only
        Pass {
            ZWrite On
            ColorMask 0
        }

        CGPROGRAM
        #pragma surface surf Lambert alpha

            // Use shader model 3.0 target, to get nicer looking lighting
            #pragma target 3.0

            sampler2D _MainTex;
            float _MinDistance;
            float _MaxDistance;
            fixed4 _Color;

            struct Input {
                float2 uv_MainTex;
                float3 worldPos;
            };

            void surf(Input IN, inout SurfaceOutput o) {
                // Albedo comes from a texture tinted by color
                fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
                o.Albedo = c.rgb;
                // Fade the pixels as they get close to the camera (Start fading at _MaxDistance and become fully transparent at _MinDistance)
                float distanceFromCamera = distance(IN.worldPos, _WorldSpaceCameraPos);
                float fade = saturate((distanceFromCamera - _MinDistance) / _MaxDistance);

                o.Alpha = c.a * fade;
            }
            ENDCG
    }

    Fallback "Transparent/VertexLit"
}