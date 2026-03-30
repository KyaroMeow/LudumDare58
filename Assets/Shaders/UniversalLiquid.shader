Shader "Custom/UniversalLiquid"
{
    Properties
    {
        _DeepColor ("Deep Color", Color) = (0.08, 0.28, 0.70, 1)
        _ShallowColor ("Shallow Color", Color) = (0.40, 0.78, 1, 1)
        _SurfaceColor ("Surface Color", Color) = (0.76, 0.94, 1, 1)
        _FillAmount ("Fill Amount", Float) = 0
        _WobbleX ("Wobble X", Range(-0.2, 0.2)) = 0
        _WobbleZ ("Wobble Z", Range(-0.2, 0.2)) = 0
        _BoundsCenter ("Bounds Center", Vector) = (0, 0, 0, 0)
        _VolumeHeight ("Volume Height", Float) = 1
        _BodyAlpha ("Body Alpha", Range(0, 1)) = 0.82
        _TopAlpha ("Top Alpha", Range(0, 1)) = 0.98
        _SurfaceThickness ("Surface Thickness", Range(0.001, 0.15)) = 0.03
        _WaveAmplitude ("Wave Amplitude", Range(0, 0.05)) = 0.0065
        _WaveFrequency ("Wave Frequency", Range(0, 12)) = 3.2
        _WaveSpeed ("Wave Speed", Range(0, 3)) = 0.45
        _UseWorldSpaceData ("Use World Space Data", Float) = 0
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
                float3 positionOS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _DeepColor;
            float4 _ShallowColor;
            float4 _SurfaceColor;
            float _FillAmount;
            float _WobbleX;
            float _WobbleZ;
            float4 _BoundsCenter;
            float _VolumeHeight;
            float _BodyAlpha;
            float _TopAlpha;
            float _SurfaceThickness;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;
            float _UseWorldSpaceData;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input, half faceSign : VFACE) : SV_Target
            {
                float3 relativePos = (_UseWorldSpaceData > 0.5f)
                    ? (input.positionWS - _BoundsCenter.xyz)
                    : (input.positionOS - _BoundsCenter.xyz);
                float wobbleOffset = (relativePos.x * _WobbleX) + (relativePos.z * _WobbleZ);
                float surfaceWave =
                    sin((relativePos.x * _WaveFrequency) + (_Time.y * _WaveSpeed)) +
                    cos((relativePos.z * (_WaveFrequency * 0.72)) - (_Time.y * (_WaveSpeed * 0.8)));
                surfaceWave *= _WaveAmplitude * 0.5;

                float surfaceDistance = relativePos.y + wobbleOffset + surfaceWave - _FillAmount;
                clip(-surfaceDistance);

                float height01 = saturate((relativePos.y / max(_VolumeHeight, 0.0001)) + 0.5);
                half surfaceBand = 1.0h - smoothstep(0.0h, _SurfaceThickness, abs(surfaceDistance));

                bool isFrontFace = faceSign > 0.0h;
                half3 liquidColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, sqrt(height01));
                liquidColor = lerp(liquidColor, _SurfaceColor.rgb, height01 * 0.14h);

                half3 topColor = lerp(liquidColor, _SurfaceColor.rgb, 0.45h + surfaceBand * 0.35h);

                if (isFrontFace)
                {
                    liquidColor = lerp(liquidColor, _SurfaceColor.rgb, surfaceBand * 0.2h);
                    return half4(liquidColor, saturate(_BodyAlpha));
                }

                return half4(topColor, saturate(_TopAlpha));
            }
            ENDHLSL
        }
    }
}
