Shader "Vefects/SH_Vefects_Unlit_Flipbook_URP_FIXED"
{
    Properties
    {
        [Space(13)][Header(Main Texture)][Space(13)]
        _MainTexture("Main Texture", 2D) = "white" {}
        _UVS("UV S", Vector) = (1,1,0,0)
        _UVP("UV P", Vector) = (0,0,0,0)

        [HDR]_R("R", Color) = (1,0.9719134,0.5896226,0)
        [HDR]_G("G", Color) = (1,0.7230805,0.25,0)
        [HDR]_B("B", Color) = (0.5943396,0.259371,0.09812209,0)
        [HDR]_Outline("Outline", Color) = (0.2169811,0.03320287,0.02354041,0)

        [Space(13)][Header(DisolveMapping)][Space(13)]
        _disolveMap("disolveMap", 2D) = "white" {}

        [Header(TextureProps)][Space(13)]
        _Intensity("Intensity", Range(0, 5)) = 1
        _ErosionSmoothness("Erosion Smoothness", Range(0.1, 15)) = 0.1
        _FlatColor("Flat Color", Range(0, 1)) = 0
        _UVDS1("UV D S", Vector) = (1,1,0,0)

        [Space(13)][Header(Distortion)][Space(13)]
        _DistortionTexture("Distortion Texture", 2D) = "white" {}
        _UVDP1("UV D P", Vector) = (0.1,-0.2,0,0)
        _DistortionLerp("Distortion Lerp", Range(0, 0.1)) = 0

        [Header(SecondDistortion)][Space(13)]
        _DistortionSecond("DistortionSecond", 2D) = "white" {}
        _SecondDistortionLerp("SecondDistortionLerp", Range(0.5, 1)) = 0.5
        _UVDS("UV D S", Vector) = (1,1,0,0)
        _UVDP("UV D P", Vector) = (0.1,-0.2,0,0)

        [Space(33)][Header(Pixelate)][Space(13)]
        [Toggle(_PIXELATE_ON)] _Pixelate("Pixelate", Float) = 0
        _PixelsMultiplier("Pixels Multiplier", Float) = 1
        _PixelsX("Pixels X", Float) = 32
        _PixelsY("Pixels Y", Float) = 32

        [Space(13)][Header(AR)][Space(13)]
        _Cull("Cull", Float) = 2
        _Src("Src", Float) = 5
        _Dst("Dst", Float) = 10
        _ZWrite("ZWrite", Float) = 0
        _ZTest("ZTest", Float) = 4

        [HideInInspector] _texcoord("", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "IsEmissive" = "true"
        }

        Cull [_Cull]
        ZWrite [_ZWrite]
        ZTest [_ZTest]
        Blend [_Src] [_Dst]

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma shader_feature_local _PIXELATE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTexture);
            SAMPLER(sampler_MainTexture);

            TEXTURE2D(_DistortionTexture);
            SAMPLER(sampler_DistortionTexture);

            TEXTURE2D(_DistortionSecond);
            SAMPLER(sampler_DistortionSecond);

            TEXTURE2D(_disolveMap);
            SAMPLER(sampler_disolveMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTexture_ST;
                float4 _disolveMap_ST;

                float2 _UVS;
                float2 _UVP;

                float4 _R;
                float4 _G;
                float4 _B;
                float4 _Outline;

                float _Intensity;
                float _ErosionSmoothness;
                float _FlatColor;

                float2 _UVDS1;
                float2 _UVDP1;

                float2 _UVDS;
                float2 _UVDP;

                float _DistortionLerp;
                float _SecondDistortionLerp;

                float _Pixelate;
                float _PixelsMultiplier;
                float _PixelsX;
                float _PixelsY;

                float _Cull;
                float _Src;
                float _Dst;
                float _ZWrite;
                float _ZTest;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float4 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR;
                float4 uv          : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;

                return output;
            }

            float2 PixelateUV(float2 uv)
            {
                float safePixelsX = max(_PixelsX * _PixelsMultiplier, 1.0);
                float safePixelsY = max(_PixelsY * _PixelsMultiplier, 1.0);

                float pixelWidth = 1.0 / safePixelsX;
                float pixelHeight = 1.0 / safePixelsY;

                return float2(
                    floor(uv.x / pixelWidth) * pixelWidth,
                    floor(uv.y / pixelHeight) * pixelHeight
                );
            }

            half4 frag(Varyings input) : SV_Target
            {
                float4 uvData = input.uv;
                float2 uv = uvData.xy;

                // Main UV panner
                float2 mainPannerUV = (_Time.y * _UVP) + (uv * _UVS);

                // Distortion UV panner
                float2 distortionUV = (_Time.y * _UVDP) + (uv * _UVDS);

                float2 distortionSample =
                    (SAMPLE_TEXTURE2D(_DistortionTexture, sampler_DistortionTexture, distortionUV).rg - 0.5) * 2.0;

                float2 distortionOffset = lerp(float2(0.0, 0.0), distortionSample, _DistortionLerp);
                float2 finalMainUV = mainPannerUV + distortionOffset;

                #if defined(_PIXELATE_ON)
                    finalMainUV = PixelateUV(finalMainUV);
                #endif

                float4 mainTex = SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, finalMainUV);

                // Color remap from RGB channels
                float4 colorFromB = lerp(_Outline, _B, mainTex.b);
                float4 colorFromG = lerp(colorFromB, _G, mainTex.g);
                float4 colorFromR = lerp(colorFromG, _R, mainTex.r);

                float4 vertexTintedColor = input.color * colorFromR;
                float4 finalColor = lerp(vertexTintedColor, input.color, _FlatColor);

                // Second distortion / brightness texture
                float2 secondDistortionUV = (_Time.y * _UVDP1) + (uv * _UVDS1);
                float4 secondDistortion =
                    SAMPLE_TEXTURE2D(_DistortionSecond, sampler_DistortionSecond, secondDistortionUV)
                    + _SecondDistortionLerp;

                float3 emission = (finalColor * _Intensity * secondDistortion).rgb;

                // Opacity from main alpha + vertex color + dissolve
                float mainAlpha = mainTex.a;
                float vertexAlpha = input.color.a;

                float opacityW = uvData.z;
                float opacityT = uvData.w;

                float mainAlphaStep = smoothstep(
                    opacityW,
                    opacityW + _ErosionSmoothness,
                    mainAlpha
                );

                // Equivalent to original remap node:
                // remap opacityW from [0,1] into [opacityT - 1, 1]
                float dissolveThreshold = (opacityT - 1.0) + opacityW * (1.0 - (opacityT - 1.0));

                float2 dissolveUV = uv * _disolveMap_ST.xy + _disolveMap_ST.zw;
                float dissolveSample = SAMPLE_TEXTURE2D(_disolveMap, sampler_disolveMap, dissolveUV).r;

                float dissolveMapping = smoothstep(
                    dissolveThreshold,
                    dissolveThreshold + opacityT,
                    dissolveSample
                );

                float alpha = mainAlphaStep * vertexAlpha * dissolveMapping;

                return half4(emission, alpha);
            }

            ENDHLSL
        }
    }

    FallBack Off
}