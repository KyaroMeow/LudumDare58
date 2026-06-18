Shader "Sorter/UI/Tutorial Neon Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1, 0.75, 0.15, 1)
        _OutlineSize ("Outline Size", Range(0.5, 4)) = 1.5
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "TutorialNeonOutline"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _OutlineColor;
            float _OutlineSize;
            float4 _ClipRect;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 stepUv = _MainTex_TexelSize.xy * _OutlineSize;
                fixed centerAlpha = tex2D(_MainTex, input.texcoord).a;
                fixed surroundingAlpha = 0;
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord + float2(stepUv.x, 0)).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord - float2(stepUv.x, 0)).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord + float2(0, stepUv.y)).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord - float2(0, stepUv.y)).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord + stepUv).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord - stepUv).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord + float2(stepUv.x, -stepUv.y)).a);
                surroundingAlpha = max(surroundingAlpha, tex2D(_MainTex, input.texcoord + float2(-stepUv.x, stepUv.y)).a);

                fixed outlineAlpha = saturate(surroundingAlpha - centerAlpha);
                #ifdef UNITY_UI_CLIP_RECT
                outlineAlpha *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif
                fixed4 result = _OutlineColor;
                result.a *= outlineAlpha * input.color.a;
                return result;
            }
            ENDCG
        }
    }
}
