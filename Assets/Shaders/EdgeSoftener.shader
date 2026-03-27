Shader "Custom/EdgeSoftened" {
    Properties {
        [HDR]_BaseColor ("Base Color", Color) = (1,1,1,1)
        [HDR]_EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _EdgeSoftness ("Edge Softness", Range(0, 1)) = 0.2
        _EdgeSharpness ("Edge Sharpness", Range(1, 5)) = 2.0
    }
    
    SubShader {
        Tags { 
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend Off
            ZWrite On
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogCoord : TEXCOORD1;
            };
            
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
                half _EdgeSoftness;
                half _EdgeSharpness;
            CBUFFER_END
            
            Varyings vert(Attributes input) {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = input.uv;
                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target {
                half distFromCenter = abs(input.uv.y - 0.5) * 2.0;
                
                half coreLine = 1.0 - distFromCenter;
                coreLine = smoothstep(0.0, _EdgeSoftness, coreLine);
                
                half coreShape = saturate(coreLine);
                
                half3 color = (_BaseColor.rgb + _EmissionColor.rgb) * coreShape;
                
                color = MixFog(color, input.fogCoord);
                
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}