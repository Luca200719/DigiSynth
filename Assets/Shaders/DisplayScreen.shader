Shader "Custom/DisplayScreen" {
    Properties {
        _ScreenTexture ("Screen Content", 2D) = "black" {}
        _GlassColor ("Glass Tint", Color) = (0.9, 0.95, 1.0, 0.95)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.98
        _Reflectivity ("Reflectivity", Range(0, 1)) = 0.15
        _FresnelPower ("Fresnel Power", Range(0, 10)) = 3.0
        _Brightness ("Screen Brightness", Range(0, 3)) = 1.2
        _ScreenBrightness ("Screen Display Brightness", Range(0, 2)) = 1.0
        _EmissionStrength ("Emission Strength", Range(0, 5)) = 1.5
        
        [Toggle] _ScreenOn ("Screen On", Float) = 1
        _OffColor ("Screen Off Color", Color) = (0.02, 0.02, 0.02, 1)
        
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.3
        _ReflectionBlur ("Reflection Blur", Range(0, 1)) = 0.1
    }

    SubShader {
        Tags { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 200

        Pass {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 tangentOS : TANGENT;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
                float4 screenPos : TEXCOORD4;
            };

            TEXTURE2D(_ScreenTexture);
            SAMPLER(sampler_ScreenTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _ScreenTexture_ST;
                float4 _GlassColor;
                float _Smoothness;
                float _Reflectivity;
                float _FresnelPower;
                float _Brightness;
                float _ScreenBrightness;
                float _EmissionStrength;
                float _ScreenOn;
                float4 _OffColor;
                float _ReflectionStrength;
                float _ReflectionBlur;
            CBUFFER_END

            Varyings vert(Attributes input) {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _ScreenTexture);
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                output.screenPos = ComputeScreenPos(output.positionCS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                half4 screenColor = SAMPLE_TEXTURE2D(_ScreenTexture, sampler_ScreenTexture, input.uv);
                
                // Base color from texture
                half3 baseColor = lerp(_OffColor.rgb, screenColor.rgb * _Brightness, _ScreenOn);
                
                // Calculate brightness of the base color
                float luminance = dot(baseColor, float3(0.299, 0.587, 0.114));
                
                // Create a gradient that dims dark areas but keeps bright areas
                // Bright areas (high luminance) = no dimming, dark areas = full dimming
                float brightnessFactor = lerp(_ScreenBrightness, 1.0, saturate(luminance * 2.0));
                
                // Apply selective dimming
                half3 displayColor = baseColor * brightnessFactor;
                
                // Emission stays at full strength for bloom
                half3 emission = baseColor * _EmissionStrength * _ScreenOn;

                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);

                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                
                float3 halfDir = normalize(lightDir + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specularPower = exp2(10 * _Smoothness + 1);
                float3 specular = mainLight.color * pow(NdotH, specularPower) * _Smoothness * 0.5;

                float3 reflectionDir = reflect(-viewDirWS, normalWS);
                
                float3 ambientReflection = unity_SHAr.rgb * 0.5 + unity_SHAg.rgb * 0.5 + unity_SHAb.rgb * 0.5;
                float3 reflection = ambientReflection * _ReflectionStrength * fresnel;

                float3 glassTint = _GlassColor.rgb * 0.1;

                // Start with dimmed display color + emission for bloom
                // Emission will be picked up by bloom post-processing
                float3 finalColor = displayColor + emission;
                finalColor = lerp(finalColor, finalColor + glassTint, 0.3);
                finalColor += specular * _Reflectivity;
                finalColor += reflection;
                
                finalColor += fresnel * _GlassColor.rgb * 0.2;

                float alpha = lerp(_GlassColor.a, 1.0, fresnel * 0.3);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }

        Pass {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input) {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target {
                return 0;
            }
            ENDHLSL
        }
    }
}
