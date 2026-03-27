Shader "Custom/Passepartout" {
    Properties{
        _Color("Color", Color) = (0, 0, 0, 1)
    }

        SubShader{
            Tags {
                "RenderType" = "Transparent"
                "RenderPipeline" = "UniversalPipeline"
                "Queue" = "Transparent"
            }

            Stencil {
                Ref 1
                Comp Equal
                Pass Keep
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Back

            Pass {
                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                CBUFFER_START(UnityPerMaterial)
                    float4 _Color;
                CBUFFER_END

                struct Attributes {
                    float4 positionOS : POSITION;
                };

                struct Varyings {
                    float4 positionHCS : SV_POSITION;
                };

                Varyings vert(Attributes IN) {
                    Varyings OUT;
                    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                    return OUT;
                }

                half4 frag(Varyings IN) : SV_Target {
                    return _Color;
                }
                ENDHLSL
            }
    }
}
