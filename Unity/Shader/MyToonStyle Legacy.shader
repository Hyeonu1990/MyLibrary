Shader "Custom/MyToonShaderLegacy"
{
    Properties
    {
		[Toggle] _LightAffectedByNormal ("LightAffectedByNormal", Float) = 1
		[Toggle] _LightSmoothStep ("LightSmoothStep", Float) = 1
		[Toggle] _UseRampTex ("UseRampTex", Float) = 0
		[Toggle] _UseRampMap("UseRampMap(UseRampTex must be true)", Float) = 0
		_RampTex("RampTex", 2D) = "white" {}
		_ToonShade("ToonShader Cubemap(RGB)", CUBE) = "" { Texgen CubeNormal }

        _MainTex ("Albedo", 2D) = "white" {}
		_BumpMap("Normal Map", 2D) = "bump" {}
		_NormalIntensity("Normal Scale", Float) = 1
		_EmissionMap("Emission", 2D) = "black" {}
		[HDR]_EmissionColor("Emission Color", Color) = (1,1,1,0)
		[HDR]_RimColor("Rim Color", Color) = (4,4,4,0)
        _Gloss("_Gloss", Float) = 32
        _RimAmount("_RimAmount", Range( 0 , 1)) = 0.716
        _RimThreshold("_RimThreshold", Range( 0 , 1)) = 0.1
        _AmbientColor("_AmbientColor", Color) = (0.4,0.4,0.4,1)
		[HDR]_SpecularColor("Specular Color", Color) = (1,1,1,1)
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0

		//[Toggle] _OutLineTeamColor ("OutLineTeamColor", Float) = 0
		//_OutlineThickness("_OutlineThickness", Range( 0 , 1)) = 0.02
		//_OutlineColor("OutlineColor", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags{ "RenderType" = "Opaque" "IsEmissive" = "true"  }
        LOD 100
		
        Pass
        {
			Tags { "LightMode"="ForwardBase" "PassFlags" = "OnlyDirectional" }
			Cull Back
			Name "PlayerCharacter"

			CGPROGRAM
			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			//only defining to not throw compilation error over Unity 5.5
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			#pragma multi_compile_fog
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			/*ase_pragma*/
			#include "UnityStandardUtils.cginc"
			#include "UnityShaderVariables.cginc"
			#pragma multi_compile_instancing
			
			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
				float4 texcoord : TEXCOORD0;				
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 pos : SV_POSITION; // ???? ?????? UNITY_TRANSFER_FOG, TRANSFER_SHADOW ???? ???? ??????
				float3 worldPos : TEXCOORD0;
				float4 uv_texcoord : TEXCOORD1;
				float4 worldNormal : TEXCOORD2;
				half3 tspace0 : TEXCOORD3;
				half3 tspace1 : TEXCOORD4;
				half3 tspace2 : TEXCOORD5;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
				UNITY_FOG_COORDS(6)
				SHADOW_COORDS(7)
			};

			sampler2D _RampTex;
			samplerCUBE _ToonShade;
			uniform float _QualityLevel;
			uniform float _LightAffectedByNormal;
			uniform float _LightSmoothStep;	
			uniform float _UseRampTex;
			uniform float _UseRampMap;
			
			sampler2D _MainTex;
			sampler2D _BumpMap;
			uniform float _NormalIntensity;
			sampler2D _EmissionMap;
			float4 _EmissionColor;

			uniform float _Gloss;
			uniform float _RimAmount;
			uniform float _RimThreshold;
			uniform float4 _RimColor;
			uniform float4 _AmbientColor;
			uniform float4 _SpecularColor;
			half _Glossiness;
			half _Metallic;

			UNITY_INSTANCING_BUFFER_START(ToonStyleProps)
				UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)
#define _MainTex_ST_arr ToonStyleProps
				UNITY_DEFINE_INSTANCED_PROP(float4, _BumpMap_ST)
#define _BumpMap_ST_arr ToonStyleProps
				UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionMap_ST)
#define _EmissionMap_ST_arr ToonStyleProps
			UNITY_INSTANCING_BUFFER_END(ToonStyleProps)

			v2f vert (appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				float3 _worldNormal = UnityObjectToWorldNormal(v.normal);
				o.worldNormal.xyz = _worldNormal;
				o.worldNormal.w = 0;
				o.uv_texcoord.xy = v.texcoord.xy;
				o.uv_texcoord.zw = 0;				
				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				half3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 wBitangent = cross(_worldNormal, wTangent) * tangentSign;
				o.tspace0 = half3(wTangent.x, wBitangent.x, _worldNormal.x);
				o.tspace1 = half3(wTangent.y, wBitangent.y, _worldNormal.y);
				o.tspace2 = half3(wTangent.z, wBitangent.z, _worldNormal.z);
				UNITY_TRANSFER_FOG(o, o.pos);
				TRANSFER_SHADOW(o);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 _Albedo_result;
				fixed3 _normalmap;
				fixed3 _Emission_result;
				float _glossiness;
				float _rimAmount;
				float _rimThreshold;
				fixed4 _ambientColor; // ?????? ???? Ambient
				fixed4 _specularColor;
				fixed4 _rimColor;
				float _reflect;
				float _metallic;
				//fixed4 _toonRamp;

				// ase common template code
				/*ase_frag_code:i=v2f*/

				float4 _MainTex_ST_Instance = UNITY_ACCESS_INSTANCED_PROP(_MainTex_ST_arr, _MainTex_ST);
				float2 uv_MainTex = i.uv_texcoord.xy * _MainTex_ST_Instance.xy + _MainTex_ST_Instance.zw;
				_Albedo_result = tex2D( _MainTex, uv_MainTex );
				
				float4 _BumpMap_ST_Instance = UNITY_ACCESS_INSTANCED_PROP(_BumpMap_ST_arr, _BumpMap_ST);
				float2 uv_BumpMap = i.uv_texcoord.xy * _BumpMap_ST_Instance.xy + _BumpMap_ST_Instance.zw;
				_normalmap = UnpackScaleNormal( tex2D( _BumpMap, uv_BumpMap ), _NormalIntensity );

				float4 _EmissionMap_ST_Instance = UNITY_ACCESS_INSTANCED_PROP(_EmissionMap_ST_arr, _EmissionMap_ST);
				float2 uv_EmissionMap = i.uv_texcoord.xy * _EmissionMap_ST_Instance.xy + _EmissionMap_ST_Instance.zw;
				_Emission_result = tex2D( _EmissionMap, uv_EmissionMap ).rgb * _EmissionColor;

				_glossiness = _Gloss;
				_rimAmount = _RimAmount;
				_rimThreshold = _RimThreshold;
				_rimColor = _RimColor;
				_ambientColor = _AmbientColor;
				_specularColor = _SpecularColor;
				_metallic = _Metallic;
				_reflect = _Glossiness;
				
				#if defined(LIGHTMAP_ON) && UNITY_VERSION < 560
					float3 worldlightDir = 0;
				#else
					float3 worldlightDir = _WorldSpaceLightPos0.xyz;//normalize(UnityWorldSpaceLightDir(i.worldPos));
				#endif
				float3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
				float3 worldNormal = normalize(i.worldNormal.xyz);
				float3 mapNormal = worldNormal;
				mapNormal.x = dot(i.tspace0, _normalmap);
				mapNormal.y = dot(i.tspace1, _normalmap);
				mapNormal.z = dot(i.tspace2, _normalmap);

				float3 halfV = Unity_SafeNormalize(worldViewDir + worldlightDir);
				float NdotL = (dot(_LightAffectedByNormal ? mapNormal : worldNormal, worldlightDir));
				float NdotH = (dot(_LightAffectedByNormal ? mapNormal : worldNormal, halfV));//saturate(dot(worldNormal, halfV));
				float NdotV = (dot(_LightAffectedByNormal ? mapNormal : worldNormal , worldViewDir));
				float VdotH = (dot(worldViewDir , halfV));
				float LdotH = (dot(worldlightDir, halfV));

				///Toon
				float4 col = 0;
				{
					//???? ?????? ?????? ?????? ?????? ??????
					float shadow = SHADOW_ATTENUATION(i);
					float lightIntensity = _LightSmoothStep ? smoothstep(0, 0.01, NdotL * shadow) : NdotL * shadow;
					//float4 light = lightIntensity * _LightColor0;

					//?????? : Ramp?????? ???? ???? ?????? ???? ???????? ??????
					float halfLambert = NdotL * 0.5f + 0.5f;
					halfLambert *= shadow;
					half4 _toonRamp = _UseRampMap ? texCUBE(_ToonShade, mapNormal) : tex2D(_RampTex, float2(halfLambert, 0));
					float4 light = _LightColor0 * (_UseRampTex ? _toonRamp : lightIntensity);
					//light.rgb += ShadeSH9(half4(_LightAffectedByNormal ? mapNormal : worldNormal, 1.0)) * _toonRamp; //Light Probe, Ambient ????
					
					float specularIntensity = pow(NdotH * (_UseRampTex ? halfLambert : lightIntensity), _glossiness * _glossiness);
					float specularIntensitySmooth = smoothstep(0.005, 0.01, specularIntensity);
					float4 specular = specularIntensitySmooth * _specularColor;
					
					float rimDot = 1 - NdotV;//dot(worldViewDir, worldNormal);
					float rimIntensity = rimDot * pow(NdotL, _rimThreshold);
					rimIntensity = smoothstep(_rimAmount - 0.01, _rimAmount + 0.01, rimIntensity);
					float4 rim = rimIntensity * _rimColor * (_UseRampTex ? halfLambert : lightIntensity);
					
					half3 worldRefl = reflect(-worldViewDir, _LightAffectedByNormal ? mapNormal : worldNormal);
					half4 skyData = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, worldRefl, i.pos.w * 0.03);
					half3 skyColor = DecodeHDR (skyData, unity_SpecCube0_HDR);
					
					_Albedo_result.rgb += lerp(float3(0, 0, 0), skyColor, _reflect) * _metallic;					
					col = _Albedo_result * (_ambientColor + light + specular + rim);
					col.a = _Albedo_result.a;
				}

				//emission
				//return float4(_Emission_result, 1);

				col.rgb += _Emission_result;
				
				// fog
				{                
					UNITY_APPLY_FOG(i.fogCoord, col);
				}

				return col;
			}
			ENDCG
        }


		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}