Shader "Sorter/URP/Paper Bottom Wind"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        [Toggle] _AlphaClip("Alpha Clip", Float) = 0
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.33

        [Header(Wind Animation)]
        _WindDirection("Wind Direction (Object Space)", Vector) = (1,0,0,0)
        _WindAmplitude("Wind Amplitude", Range(0, 0.03)) = 0.006
        _FlutterAmplitude("Flutter Amplitude", Range(0, 0.02)) = 0.002
        _WindFrequency("Wind Frequency", Range(0, 5)) = 1.0
        _FlutterFrequency("Flutter Frequency", Range(0, 12)) = 3.0
        _NoiseStrength("Noise Strength", Range(0, 2)) = 0.6
        _BottomStart("Bottom Start Y", Range(-1, 1)) = -0.5
        _BottomEnd("Bottom End Y", Range(-1, 1)) = 0.05
        _EdgeFlutter("Edge Flutter", Range(0, 1)) = 0.35
        _BackfaceTint("Backface Tint", Range(0.7, 1.0)) = 0.92
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            Name "UnlitForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _WindDirection;
                float _AlphaClip;
                float _Cutoff;
                float _WindAmplitude;
                float _FlutterAmplitude;
                float _WindFrequency;
                float _FlutterFrequency;
                float _NoiseStrength;
                float _BottomStart;
                float _BottomEnd;
                float _EdgeFlutter;
                float _BackfaceTint;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float3 ApplyWindOffset(float3 positionOS, float2 uv)
            {
                float mask = 1.0 - smoothstep(_BottomStart, _BottomEnd, positionOS.y);
                float edgeMask = pow(saturate(abs(uv.x - 0.5) * 2.0), 1.5) * _EdgeFlutter + (1.0 - _EdgeFlutter);

                float time = _Time.y;
                float baseWave = sin(time * _WindFrequency + positionOS.y * 4.5 + uv.x * 2.4);
                float flutterWave = sin(time * _FlutterFrequency + positionOS.y * 11.0 + uv.x * 8.0);
                float noise = (Hash21(positionOS.xy + floor(time * 0.7)) * 2.0 - 1.0) * _NoiseStrength;

                float motion = baseWave * _WindAmplitude + flutterWave * _FlutterAmplitude + noise * (_FlutterAmplitude * 0.35);
                float3 windDir = _WindDirection.xyz;
                float windLength = length(windDir);
                windDir = windLength > 0.0001 ? windDir / windLength : float3(1.0, 0.0, 0.0);

                return positionOS + windDir * (motion * mask * edgeMask);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 animatedPositionOS = ApplyWindOffset(input.positionOS.xyz, input.uv);
                VertexPositionInputs positionInputs = GetVertexPositionInputs(animatedPositionOS);

                output.positionCS = positionInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 frag(Varyings input, FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                if (_AlphaClip > 0.5)
                {
                    clip(color.a - _Cutoff);
                }

                #if defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
                bool frontFace = true;
                #else
                bool frontFace = IS_FRONT_VFACE(isFrontFace, true, false);
                #endif

                if (!frontFace)
                {
                    color.rgb *= _BackfaceTint;
                }

                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }
    }
}
