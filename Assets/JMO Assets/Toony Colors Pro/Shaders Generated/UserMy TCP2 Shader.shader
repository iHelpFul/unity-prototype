Shader "Toony Colors Pro 2/User/My TCP2 Mask Tint"
{
	Properties
	{
		[TCP2HeaderHelp(Base)]
		_BaseColor ("Color", Color) = (1,1,1,1)
		[TCP2ColorNoAlpha] _HColor ("Highlight Color", Color) = (0.75,0.75,0.75,1)
		[TCP2ColorNoAlpha] _SColor ("Shadow Color", Color) = (0.2,0.2,0.2,1)
		[MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
		[TCP2Separator]

		[TCP2Header(Ramp Shading)]
		_RampThreshold ("Threshold", Range(0.01,1)) = 0.5
		_RampSmoothing ("Smoothing", Range(0.001,1)) = 0.5
		[TCP2Separator]

		[TCP2Header(Mask Tint)]
		_Mask01 ("Mask01", 2D) = "black" {}
		_Mask02 ("Mask02", 2D) = "black" {}
		_Mask03 ("Mask03", 2D) = "black" {}

		_Color01 ("Color01", Color) = (0.7205882,0.08477508,0.08477508,1)
		_Color02 ("Color02", Color) = (0.02649222,0.3602941,0.09785674,1)
		_Color03 ("Color03", Color) = (0.07628676,0.2567445,0.6102941,1)

		_Color04 ("Color04", Color) = (1,0.6729082,0,1)
		_Color05 ("Color05", Color) = (0.3161438,0.08018869,1,1)
		_Color06 ("Color06", Color) = (0.829558,0.2311321,1,1)

		_Color07 ("Color07", Color) = (0.5660378,0.23073,0.03470988,1)
		_Color08 ("Color08", Color) = (0.3584906,0.3584906,0.3584906,1)
		_Color09_SKIN ("Color09_SKIN", Color) = (0.9622642,0.6942402,0.521983,1)

		_Color01Power ("Color01Power", Range(0,6)) = 1
		_Color02Power ("Color02Power", Range(0,6)) = 1
		_Color03Power ("Color03Power", Range(0,6)) = 1
		_Color04Power ("Color04Power", Range(0,6)) = 1
		_Color05Power ("Color05Power", Range(0,6)) = 1
		_Color06Power ("Color06Power", Range(0,6)) = 1
		_Color07Power ("Color07Power", Range(0,6)) = 1
		_Color08Power ("Color08Power", Range(0,6)) = 1
		_Color09Power ("Color09Power", Range(0,6)) = 1
		[TCP2Separator]

		[TCP2Header(Emission)]
		_EmissionMap ("Emission", 2D) = "black" {}
		[HDR] _EmissionPower ("Emission Power", Color) = (0,0,0,0)
		[TCP2Separator]

		[ToggleOff(_RECEIVE_SHADOWS_OFF)] _ReceiveShadowsOff ("Receive Shadows", Float) = 1

		[HideInInspector] __dummy__ ("unused", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"RenderType"="Opaque"
		}

		HLSLINCLUDE
		#define fixed half
		#define fixed2 half2
		#define fixed3 half3
		#define fixed4 half4

		#if UNITY_VERSION >= 202020
			#define URP_10_OR_NEWER
		#endif
		#if UNITY_VERSION >= 202120
			#define URP_12_OR_NEWER
		#endif
		#if UNITY_VERSION >= 202220
			#define URP_14_OR_NEWER
		#endif

		#define TCP2_TEX2D_WITH_SAMPLER(tex) TEXTURE2D(tex); SAMPLER(sampler##tex)
		#define TCP2_TEX2D_NO_SAMPLER(tex) TEXTURE2D(tex)
		#define TCP2_TEX2D_SAMPLE(tex, samplertex, coord) SAMPLE_TEXTURE2D(tex, sampler##samplertex, coord)
		#define TCP2_TEX2D_SAMPLE_LOD(tex, samplertex, coord, lod) SAMPLE_TEXTURE2D_LOD(tex, sampler##samplertex, coord, lod)

		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

		TCP2_TEX2D_WITH_SAMPLER(_BaseMap);
		TCP2_TEX2D_WITH_SAMPLER(_Mask01);
		TCP2_TEX2D_WITH_SAMPLER(_Mask02);
		TCP2_TEX2D_WITH_SAMPLER(_Mask03);
		TCP2_TEX2D_WITH_SAMPLER(_EmissionMap);

		CBUFFER_START(UnityPerMaterial)
			float4 _BaseMap_ST;
			float4 _Mask01_ST;
			float4 _Mask02_ST;
			float4 _Mask03_ST;
			float4 _EmissionMap_ST;

			fixed4 _BaseColor;
			float _RampThreshold;
			float _RampSmoothing;
			fixed4 _SColor;
			fixed4 _HColor;

			fixed4 _Color01;
			fixed4 _Color02;
			fixed4 _Color03;
			fixed4 _Color04;
			fixed4 _Color05;
			fixed4 _Color06;
			fixed4 _Color07;
			fixed4 _Color08;
			fixed4 _Color09_SKIN;

			float _Color01Power;
			float _Color02Power;
			float _Color03Power;
			float _Color04Power;
			float _Color05Power;
			float _Color06Power;
			float _Color07Power;
			float _Color08Power;
			float _Color09Power;

			fixed4 _EmissionPower;
		CBUFFER_END

		#define UnityObjectToClipPos TransformObjectToHClip
		#define _WorldSpaceLightPos0 _MainLightPosition

		inline half3 ComputeMaskedAlbedo(float2 uv, half3 baseRgb)
		{
			float2 uvMask01 = uv * _Mask01_ST.xy + _Mask01_ST.zw;
			float2 uvMask02 = uv * _Mask02_ST.xy + _Mask02_ST.zw;
			float2 uvMask03 = uv * _Mask03_ST.xy + _Mask03_ST.zw;

			half4 m1 = TCP2_TEX2D_SAMPLE(_Mask01, _Mask01, uvMask01);
			half4 m2 = TCP2_TEX2D_SAMPLE(_Mask02, _Mask02, uvMask02);
			half4 m3 = TCP2_TEX2D_SAMPLE(_Mask03, _Mask03, uvMask03);

			half3 tintAccum = half3(0,0,0);

			tintAccum += min(m1.r.xxx, _Color01.rgb) * _Color01Power;
			tintAccum += min(m1.g.xxx, _Color02.rgb) * _Color02Power;
			tintAccum += min(m1.b.xxx, _Color03.rgb) * _Color03Power;

			tintAccum += min(m2.r.xxx, _Color04.rgb) * _Color04Power;
			tintAccum += min(m2.g.xxx, _Color05.rgb) * _Color05Power;
			tintAccum += min(m2.b.xxx, _Color06.rgb) * _Color06Power;

			tintAccum += min(m3.r.xxx, _Color07.rgb) * _Color07Power;
			tintAccum += min(m3.g.xxx, _Color08.rgb) * _Color08Power;
			tintAccum += min(m3.b.xxx, _Color09_SKIN.rgb) * _Color09Power;

			half maskStrength = saturate(
				m1.r + m1.g + m1.b +
				m2.r + m2.g + m2.b +
				m3.r + m3.g + m3.b
			);

			half3 tinted = saturate(baseRgb * tintAccum);
			return lerp(baseRgb, tinted, maskStrength);
		}

		ENDHLSL

		Pass
		{
			Name "Main"
			Tags { "LightMode"="UniversalForward" }

			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 3.0

			#pragma shader_feature_local _ _RECEIVE_SHADOWS_OFF

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

			#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
			#pragma multi_compile _ SHADOWS_SHADOWMASK
			#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"

			#pragma multi_compile_instancing

			#pragma vertex Vertex
			#pragma fragment Fragment

			struct Attributes
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 texcoord0 : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float3 normal : NORMAL;
				float4 worldPosAndFog : TEXCOORD0;
			#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				float4 shadowCoord : TEXCOORD1;
			#endif
			#ifdef _ADDITIONAL_LIGHTS_VERTEX
				half3 vertexLights : TEXCOORD2;
			#endif
				float2 pack0 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			#if USE_FORWARD_PLUS || USE_CLUSTER_LIGHT_LOOP
				struct InputDataForwardPlusDummy
				{
					float3 positionWS;
					float2 normalizedScreenSpaceUV;
				};
			#endif

			Varyings Vertex(Attributes input)
			{
				Varyings output = (Varyings)0;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				output.pack0.xy = input.texcoord0.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);
			#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				output.shadowCoord = GetShadowCoord(vertexInput);
			#endif

				VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normal);
			#ifdef _ADDITIONAL_LIGHTS_VERTEX
				output.vertexLights = VertexLighting(vertexInput.positionWS, vertexNormalInput.normalWS);
			#endif

				output.worldPosAndFog = float4(vertexInput.positionWS.xyz, 0);
				output.normal = normalize(vertexNormalInput.normalWS);
				output.positionCS = vertexInput.positionCS;

				return output;
			}

			half4 Fragment(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				float3 positionWS = input.worldPosAndFog.xyz;
				float3 normalWS = normalize(input.normal);

				float4 baseSample = TCP2_TEX2D_SAMPLE(_BaseMap, _BaseMap, input.pack0.xy);
				float4 mainColor = _BaseColor;
				float alpha = baseSample.a * mainColor.a;

				half3 albedo = baseSample.rgb;
				albedo = ComputeMaskedAlbedo(input.pack0.xy, albedo);
				albedo *= mainColor.rgb;

				float2 uvEmission = input.pack0.xy * (_EmissionMap_ST.xy / _BaseMap_ST.xy) + (_EmissionMap_ST.zw - _BaseMap_ST.zw);
				half3 emission = (TCP2_TEX2D_SAMPLE(_EmissionMap, _EmissionMap, uvEmission).rgb * _EmissionPower.rgb);

			#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
				float4 shadowCoord = input.shadowCoord;
			#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
				float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
			#else
				float4 shadowCoord = float4(0, 0, 0, 0);
			#endif

			#if defined(URP_10_OR_NEWER)
				half4 shadowMask = half4(1,1,1,1);
				Light mainLight = GetMainLight(shadowCoord, positionWS, shadowMask);
			#else
				Light mainLight = GetMainLight(shadowCoord);
			#endif

				half3 bakedGI = SampleSH(normalWS);
				half3 indirectDiffuse = bakedGI * albedo;

				half3 lightDir = mainLight.direction;
				half3 lightColor = mainLight.color.rgb;
				half atten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;

				half ndl = saturate(dot(normalWS, lightDir));
				half rampSmooth = _RampSmoothing * 0.5;
				half3 ramp = smoothstep(_RampThreshold - rampSmooth, _RampThreshold + rampSmooth, ndl);
				ramp *= atten;

				half3 color = half3(0,0,0);
				half3 accumulatedRamp = ramp * max(lightColor.r, max(lightColor.g, lightColor.b));
				half3 accumulatedColors = ramp * lightColor.rgb;

			#ifdef _ADDITIONAL_LIGHTS
				uint pixelLightCount = GetAdditionalLightsCount();

				#if USE_FORWARD_PLUS || USE_CLUSTER_LIGHT_LOOP
					for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
					{
						CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
						Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);

						half atten2 = light.shadowAttenuation * light.distanceAttenuation;
						half3 lightColor2 = light.color.rgb;
						half3 lightDir2 = light.direction;

						half ndl2 = saturate(dot(normalWS, lightDir2));
						half3 ramp2 = smoothstep(_RampThreshold - rampSmooth, _RampThreshold + rampSmooth, ndl2);
						ramp2 *= atten2;

						accumulatedRamp += ramp2 * max(lightColor2.r, max(lightColor2.g, lightColor2.b));
						accumulatedColors += ramp2 * lightColor2.rgb;
					}

					InputDataForwardPlusDummy inputData;
					inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
					inputData.positionWS = positionWS;
				#endif

				LIGHT_LOOP_BEGIN(pixelLightCount)
				{
				#if defined(URP_10_OR_NEWER)
					Light light = GetAdditionalLight(lightIndex, positionWS, shadowMask);
				#else
					Light light = GetAdditionalLight(lightIndex, positionWS);
				#endif
					half atten2 = light.shadowAttenuation * light.distanceAttenuation;
					half3 lightColor2 = light.color.rgb;
					half3 lightDir2 = light.direction;

					half ndl2 = saturate(dot(normalWS, lightDir2));
					half3 ramp2 = smoothstep(_RampThreshold - rampSmooth, _RampThreshold + rampSmooth, ndl2);
					ramp2 *= atten2;

					accumulatedRamp += ramp2 * max(lightColor2.r, max(lightColor2.g, lightColor2.b));
					accumulatedColors += ramp2 * lightColor2.rgb;
				}
				LIGHT_LOOP_END
			#endif

			#ifdef _ADDITIONAL_LIGHTS_VERTEX
				color += input.vertexLights * albedo;
			#endif

				accumulatedRamp = saturate(accumulatedRamp);
				half3 shadowColor = (1 - accumulatedRamp.rgb) * _SColor.rgb;
				accumulatedRamp = accumulatedColors.rgb * _HColor.rgb + shadowColor;
				color += albedo * accumulatedRamp;

				color += indirectDiffuse;
				color += emission;

				return half4(color, alpha);
			}
			ENDHLSL
		}

		HLSLINCLUDE

		#if defined(SHADOW_CASTER_PASS) || defined(DEPTH_ONLY_PASS)

			#define fixed half
			#define fixed2 half2
			#define fixed3 half3
			#define fixed4 half4

			float3 _LightDirection;
			float3 _LightPosition;

			struct Attributes
			{
				float4 vertex   : POSITION;
				float3 normal   : NORMAL;
				float4 texcoord0 : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
			#if defined(DEPTH_NORMALS_PASS)
				float3 normalWS : TEXCOORD0;
			#endif
				float2 pack0 : TEXCOORD1;
			#if defined(DEPTH_ONLY_PASS)
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			#endif
			};

			float4 GetShadowPositionHClip(Attributes input)
			{
				float3 positionWS = TransformObjectToWorld(input.vertex.xyz);
				float3 normalWS = TransformObjectToWorldNormal(input.normal);

				#if _CASTING_PUNCTUAL_LIGHT_SHADOW
					float3 lightDirectionWS = normalize(_LightPosition - positionWS);
				#else
					float3 lightDirectionWS = _LightDirection;
				#endif

				float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

				#if UNITY_REVERSED_Z
					positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
				#else
					positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
				#endif

				return positionCS;
			}

			Varyings ShadowDepthPassVertex(Attributes input)
			{
				Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				#if defined(DEPTH_ONLY_PASS)
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				#endif

				output.pack0.xy = input.texcoord0.xy * _BaseMap_ST.xy + _BaseMap_ST.zw;

				#if defined(DEPTH_ONLY_PASS)
					output.positionCS = TransformObjectToHClip(input.vertex.xyz);
					#if defined(DEPTH_NORMALS_PASS)
						float3 normalWS = TransformObjectToWorldNormal(input.normal);
						output.normalWS = normalWS;
					#endif
				#elif defined(SHADOW_CASTER_PASS)
					output.positionCS = GetShadowPositionHClip(input);
				#else
					output.positionCS = float4(0,0,0,0);
				#endif

				return output;
			}

			half4 ShadowDepthPassFragment(
				Varyings input
	#if defined(DEPTH_NORMALS_PASS) && defined(_WRITE_RENDERING_LAYERS)
		#if UNITY_VERSION >= 60020000
				, out uint outRenderingLayers : SV_Target1
		#else
				, out float4 outRenderingLayers : SV_Target1
		#endif
	#endif
			) : SV_TARGET
			{
				#if defined(DEPTH_ONLY_PASS)
					UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				#endif

				float4 albedoSample = TCP2_TEX2D_SAMPLE(_BaseMap, _BaseMap, input.pack0.xy);
				float alpha = albedoSample.a * _BaseColor.a;

				#if defined(DEPTH_NORMALS_PASS)
					#if defined(_WRITE_RENDERING_LAYERS)
						#if UNITY_VERSION >= 60020000
							outRenderingLayers = EncodeMeshRenderingLayer();
						#else
							outRenderingLayers = float4(EncodeMeshRenderingLayer(GetMeshRenderingLayer()), 0, 0, 0);
						#endif
					#endif

					#if defined(URP_12_OR_NEWER)
						return float4(input.normalWS.xyz, 0.0);
					#else
						return float4(PackNormalOctRectEncode(TransformWorldToViewDir(input.normalWS, true)), 0.0, 0.0);
					#endif
				#endif

				return 0;
			}

		#endif
		ENDHLSL

		Pass
		{
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On
			ZTest LEqual

			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

			#pragma multi_compile SHADOW_CASTER_PASS
			#pragma multi_compile_instancing
			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

			#pragma vertex ShadowDepthPassVertex
			#pragma fragment ShadowDepthPassFragment

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "DepthOnly"
			Tags { "LightMode" = "DepthOnly" }

			ZWrite On
			ColorMask 0

			HLSLPROGRAM
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

			#pragma multi_compile_instancing
			#pragma multi_compile DEPTH_ONLY_PASS

			#pragma vertex ShadowDepthPassVertex
			#pragma fragment ShadowDepthPassFragment
			ENDHLSL
		}

		Pass
		{
			Name "DepthNormals"
			Tags { "LightMode" = "DepthNormals" }

			ZWrite On

			HLSLPROGRAM
			#pragma exclude_renderers gles gles3 glcore
			#pragma target 2.0

			#pragma multi_compile_instancing
			#pragma multi_compile DEPTH_ONLY_PASS
			#pragma multi_compile DEPTH_NORMALS_PASS

			#pragma vertex ShadowDepthPassVertex
			#pragma fragment ShadowDepthPassFragment
			ENDHLSL
		}
	}

	FallBack "Hidden/InternalErrorShader"
	CustomEditor "ToonyColorsPro.ShaderGenerator.MaterialInspector_SG2"
}