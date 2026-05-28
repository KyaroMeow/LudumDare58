Shader "Sorter/UV Stain Glow"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.62, 0.05, 1, 1)
        _GlowColor ("Glow Color", Color) = (0.82, 0.22, 1, 1)
        _Reveal ("Reveal", Range(0, 1)) = 0
        _GlowIntensity ("Glow Intensity", Range(0, 12)) = 5
        _EdgePower ("Soft Edge Power", Range(0.25, 6)) = 1.15
        _PulseStrength ("Pulse Strength", Range(0, 1)) = 0.18
        _PulseSpeed ("Pulse Speed", Range(0, 8)) = 2.4
        _SparkleStrength ("Sparkle Strength", Range(0, 2)) = 0.9
        _SparkleScale ("Sparkle Scale", Range(4, 80)) = 34
        _SparkleSpeed ("Sparkle Speed", Range(0, 12)) = 5.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "RenderPipeline"="UniversalPipeline"
        }

        Blend SrcAlpha One
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _GlowColor;
                half _Reveal;
                half _GlowIntensity;
                half _EdgePower;
                half _PulseStrength;
                half _PulseSpeed;
                half _SparkleStrength;
                half _SparkleScale;
                half _SparkleSpeed;
                float4 _BaseMap_ST;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half Hash21(half2 p)
            {
                p = frac(p * half2(123.34h, 456.21h));
                p += dot(p, p + 45.32h);
                return frac(p.x * p.y);
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half reveal = saturate(_Reveal);
                half2 centeredUv = abs(input.uv - 0.5h) * 2.0h;
                half edgeDistance = max(centeredUv.x, centeredUv.y);
                half softMask = saturate(1.0h - pow(edgeDistance, _EdgePower));
                half edgeBand = smoothstep(0.42h, 0.95h, edgeDistance) * softMask;
                half pulse = 1.0h + sin(_Time.y * _PulseSpeed) * _PulseStrength;

                half2 sparkleUv = floor(input.uv * _SparkleScale);
                half sparkleSeed = Hash21(sparkleUv);
                half sparklePhase = frac(sparkleSeed + _Time.y * _SparkleSpeed * 0.18h);
                half sparkle = smoothstep(0.92h, 1.0h, sparkleSeed) *
                    smoothstep(0.0h, 0.25h, sparklePhase) *
                    (1.0h - smoothstep(0.25h, 0.75h, sparklePhase)) *
                    edgeBand * _SparkleStrength;

                half textureMask = saturate(tex.a * max(max(tex.r, tex.g), tex.b));
                half visibleReveal = smoothstep(0.02h, 1.0h, reveal);
                half alpha = saturate((0.18h + softMask * 0.82h + sparkle) * tex.a * _BaseColor.a * visibleReveal);
                half glow = (_GlowIntensity * visibleReveal * (0.45h + softMask + sparkle)) * pulse;
                half3 stainColor = lerp(_BaseColor.rgb, _GlowColor.rgb, saturate(softMask + sparkle));
                half3 color = stainColor * (0.35h + textureMask + glow);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
