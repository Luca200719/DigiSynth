Shader "UI/ForceLinearGrading"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,0.99607843137,1,1)

        // Small adjustment controls
        _Brightness ("Brightness", Range(0.9, 1.1)) = 1.016
        _Desaturate ("Desaturate", Range(0, 0.2)) = 0.05
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _Color;
            float _Brightness;
            float _Desaturate;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                // Slight brightness lift
                col.rgb *= _Brightness;

                // Slight desaturation toward neutral gray
                float gray = dot(col.rgb, float3(0.3333, 0.3333, 0.3333));
                col.rgb = lerp(col.rgb, gray.xxx, _Desaturate);

                return col * _Color;
            }

            ENDHLSL
        }
    }
}
