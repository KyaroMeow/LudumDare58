Shader "Custom/TeapotLiquid"
{
    Properties
    {
        _DeepColor ("Deep Color", Color) = (0.08, 0.28, 0.70, 1)
        _ShallowColor ("Shallow Color", Color) = (0.40, 0.78, 1, 1)
        _SurfaceColor ("Surface Color", Color) = (0.76, 0.94, 1, 1)
        _SurfaceLineColor ("Surface Line Color", Color) = (0.95, 0.99, 1, 1)
        _FoamColor ("Foam Color", Color) = (0.93, 0.99, 1, 1)
        _FillAmount ("Fill Amount", Float) = 0
        _WobbleX ("Wobble X", Range(-0.2, 0.2)) = 0
        _WobbleZ ("Wobble Z", Range(-0.2, 0.2)) = 0
        _BoundsCenter ("Bounds Center", Vector) = (0, 0, 0, 0)
        _VolumeHeight ("Volume Height", Float) = 1
        _BodyAlpha ("Body Alpha", Range(0, 1)) = 0.82
        _TopAlpha ("Top Alpha", Range(0, 1)) = 0.98
        _SurfaceThickness ("Surface Thickness", Range(0.001, 0.15)) = 0.03
        _SurfaceLineIntensity ("Surface Line Intensity", Range(0, 4)) = 1.6
        _RimPower ("Rim Power", Range(0.5, 8)) = 4.2
        _RimIntensity ("Rim Intensity", Range(0, 1)) = 0.14
        _WaveAmplitude ("Wave Amplitude", Range(0, 0.05)) = 0.0065
        _WaveFrequency ("Wave Frequency", Range(0, 12)) = 3.2
        _WaveSpeed ("Wave Speed", Range(0, 3)) = 0.45
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent-10"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Off

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
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _DeepColor;
            float4 _ShallowColor;
            float4 _SurfaceColor;
            float4 _SurfaceLineColor;
            float4 _FoamColor;
            float _FillAmount;
            float _WobbleX;
            float _WobbleZ;
            float4 _BoundsCenter;
            float _VolumeHeight;
            float _BodyAlpha;
            float _TopAlpha;
            float _SurfaceThickness;
            float _SurfaceLineIntensity;
            float _RimPower;
            float _RimIntensity;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionInputs.positionWS);
                return output;
            }

            half4 frag(Varyings input, half faceSign : VFACE) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                float3 relativePos = input.positionWS - _BoundsCenter.xyz;
                float wobbleOffset = (relativePos.x * _WobbleX) + (relativePos.z * _WobbleZ);
                float surfaceWave =
                    sin((relativePos.x * _WaveFrequency) + (_Time.y * _WaveSpeed)) +
                    cos((relativePos.z * (_WaveFrequency * 0.72)) - (_Time.y * (_WaveSpeed * 0.8)));
                surfaceWave *= _WaveAmplitude * 0.5;

                float surfaceDistance = relativePos.y + wobbleOffset + surfaceWave - _FillAmount;
                clip(-surfaceDistance);

                float height01 = saturate((relativePos.y / max(_VolumeHeight, 0.0001)) + 0.5);
                half rim = pow(1.0h - saturate(dot(normalWS, viewDirWS)), _RimPower) * _RimIntensity;
                half surfaceBand = 1.0h - smoothstep(0.0h, _SurfaceThickness, abs(surfaceDistance));
                half surfaceLine = saturate(surfaceBand * _SurfaceLineIntensity);

                bool isFrontFace = faceSign > 0.0h;
                half3 liquidColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, sqrt(height01));
                liquidColor = lerp(liquidColor, _SurfaceColor.rgb, height01 * 0.2h);
                liquidColor += rim * _SurfaceColor.rgb;

                half3 topColor = lerp(_SurfaceColor.rgb, _FoamColor.rgb, surfaceBand);

                if (isFrontFace)
                {
                    liquidColor = lerp(liquidColor, _FoamColor.rgb, surfaceBand * 0.55h);
                    liquidColor = lerp(liquidColor, _SurfaceLineColor.rgb, surfaceLine);
                    half liquidAlpha = max(_BodyAlpha, surfaceBand * 0.95h);
                    return half4(liquidColor, saturate(liquidAlpha));
                }

                topColor = lerp(topColor, _SurfaceLineColor.rgb, surfaceLine * 0.8h);
                half topAlpha = max(_TopAlpha, surfaceBand);
                return half4(topColor, saturate(topAlpha));
            }
            ENDHLSL
        }
    }
}
