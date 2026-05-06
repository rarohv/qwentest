Shader "Custom/RealisticWater"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.08, 0.32, 0.58, 0.65)
        _DepthColor ("Depth Color", Color) = (0.02, 0.07, 0.18, 1.0)
        _Transparency ("Transparency", Range(0, 1)) = 0.55
        _RefractionStrength ("Refraction Strength", Range(0, 0.05)) = 0.015
        _WaveSpeed ("Wave Speed", Float) = 1.2
        _WaveScale ("Wave Scale", Float) = 4.0
        _WaveAmplitude ("Wave Amplitude", Range(0, 0.5)) = 0.06
        _SpecularColor ("Specular Color", Color) = (1.0, 0.7, 0.35, 1.0)
        _SpecularPower ("Specular Power", Float) = 128.0
        _SpecularIntensity ("Specular Intensity", Range(0, 3)) = 1.5
        _FresnelPower ("Fresnel Power", Float) = 4.0
        _FoamColor ("Foam Color", Color) = (0.85, 0.9, 0.95, 1.0)
        _FoamThreshold ("Foam Threshold", Range(0, 1)) = 0.5
        _FoamIntensity ("Foam Intensity", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+1"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "WaterForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                float3 viewDirWS : TEXCOORD4;
                float waveHeight : TEXCOORD5;
            };

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _DepthColor;
                half _Transparency;
                half _RefractionStrength;
                half _WaveSpeed;
                half _WaveScale;
                half _WaveAmplitude;
                half4 _SpecularColor;
                half _SpecularPower;
                half _SpecularIntensity;
                half _FresnelPower;
                half4 _FoamColor;
                half _FoamThreshold;
                half _FoamIntensity;
            CBUFFER_END

            half ComputeWaveHeight(half2 pos, half time)
            {
                half h = 0;
                h += sin(pos.x * _WaveScale + time * _WaveSpeed) * 0.45;
                h += sin(pos.y * _WaveScale * 1.3 + time * _WaveSpeed * 0.7) * 0.28;
                h += sin((pos.x + pos.y) * _WaveScale * 0.7 + time * _WaveSpeed * 1.1) * 0.18;
                h += sin((pos.x * 0.8 - pos.y * 1.2) * _WaveScale * 0.9 + time * _WaveSpeed * 0.5) * 0.14;
                h += sin(pos.x * _WaveScale * 2.1 + pos.y * _WaveScale * 1.7 + time * _WaveSpeed * 1.5) * 0.08;
                return h;
            }

            half3 ComputeWaveNormal(half2 pos, half time)
            {
                half eps = 0.05;
                half hL = ComputeWaveHeight(pos - half2(eps, 0), time);
                half hR = ComputeWaveHeight(pos + half2(eps, 0), time);
                half hD = ComputeWaveHeight(pos - half2(0, eps), time);
                half hU = ComputeWaveHeight(pos + half2(0, eps), time);

                half3 n;
                n.x = (hL - hR) / (2.0 * eps);
                n.z = (hD - hU) / (2.0 * eps);
                n.y = 2.0;
                return normalize(n);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);

                half time = _Time.y;
                half2 worldXZ = output.positionWS.xz * 0.1;
                half height = ComputeWaveHeight(worldXZ, time);
                output.positionWS.y += height * _WaveAmplitude;
                output.waveHeight = height;

                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.viewDirWS = _WorldSpaceCameraPos.xyz - output.positionWS;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half time = _Time.y;
                half2 worldXZ = input.positionWS.xz * 0.1;

                half3 waveNormal = ComputeWaveNormal(worldXZ, time);
                half3 normalWS = normalize(waveNormal);

                half3 viewDir = normalize(input.viewDirWS);

                Light mainLight = GetMainLight();
                half3 lightDir = mainLight.direction;
                half3 halfDir = normalize(lightDir + viewDir);

                half NdotH = max(dot(normalWS, halfDir), 0.0);
                half specular = pow(NdotH, _SpecularPower) * _SpecularIntensity;
                half3 specColor = _SpecularColor.rgb * specular * mainLight.color;

                half NdotV = max(dot(normalWS, viewDir), 0.0);
                half fresnel = pow(1.0 - NdotV, _FresnelPower);

                half2 screenUV = input.screenPos.xy / input.screenPos.w;
                half2 refractOffset = waveNormal.xz * _RefractionStrength;
                half2 refractedUV = saturate(screenUV + refractOffset);

                half3 background = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, refractedUV).rgb;

                half3 waterColor = lerp(_BaseColor.rgb, _DepthColor.rgb, fresnel * 0.6);
                half3 finalColor = lerp(background, waterColor, _Transparency + fresnel * 0.3);

                finalColor += specColor;

                half3 eveningAmbient = half3(0.12, 0.08, 0.18);
                finalColor += eveningAmbient * _BaseColor.rgb;

                half NdotL = max(dot(normalWS, lightDir), 0.0) * 0.35 + 0.65;
                finalColor *= NdotL;

                half foam = smoothstep(_FoamThreshold, _FoamThreshold + 0.2, input.waveHeight);
                finalColor += foam * _FoamColor.rgb * _FoamIntensity;

                half alpha = saturate(_Transparency + fresnel * 0.4);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
