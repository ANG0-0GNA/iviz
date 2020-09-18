Shader "iviz/Line"
{
	Properties
	{
	}

	SubShader
	{
		Cull Off
		Tags { "RenderType"="Opaque" }

		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ USE_TEXTURE
			#pragma multi_compile_instancing

			sampler2D _IntensityTexture;
			float _IntensityCoeff;
			float _IntensityAdd;

			float4x4 _LocalToWorld;
			float4x4 _WorldToLocal;
			float4 _Front;
			float4 _Tint;
			
			float _Scale;

            static const float3 Quads[8] =
            {
                float3(0.5, 0, 1),
                float3(0, 0.5, 1),
                float3(0, 0.5, 0),
                float3(-0.5, 0 , 0),

                float3(-0.5, 0 , 0),
                float3(0, -0.5, 0),
                float3(0, -0.5, 1),
                float3(0.5, 0, 1),
            };

			struct Line {
				float3 A;
#if USE_TEXTURE
				float intensityA;
#else
				uint colorA;
#endif
				float3 B;
#if USE_TEXTURE
				float intensityB;
#else
				uint colorB;
#endif
			};

			StructuredBuffer<Line> _Lines;

			struct v2f
			{
				float4 position : SV_POSITION;
				half3 color : COLOR;
			};

			v2f vert(uint id : SV_VertexID, uint inst : SV_InstanceID)
			{
				unity_ObjectToWorld = _LocalToWorld;
				unity_WorldToObject = _WorldToLocal;

				float3 V = Quads[id];
				V.xy *= _Scale;
				
				float3 A = _Lines[inst].A;
				float3 B = _Lines[inst].B;
				float3 BA = B - A;
                float3 right = normalize(BA);

				float3 mid = (B + A) / 2;
				float3 front = mid - _Front;
                float3 up = normalize(cross(front, right));

                float3 p = right * V.x + up * V.y + BA * V.z + A;

				v2f o;
				o.position = UnityObjectToClipPos(float4(p, 1));

	#if USE_TEXTURE
				float intensityA = _Lines[inst].intensityA;
				half4 rgbaA = tex2Dlod(_IntensityTexture, float4(intensityA * _IntensityCoeff + _IntensityAdd, 0, 0, 0));
				float intensityB = _Lines[inst].intensityB;
				half4 rgbaB = tex2Dlod(_IntensityTexture, float4(intensityB * _IntensityCoeff + _IntensityAdd, 0, 0, 0));
    #else
				uint cA = _Lines[inst].colorA;
				half4 rgbaA = half4(
					(cA >>  0) & 0xff,
					(cA >>  8) & 0xff,
					(cA >> 16) & 0xff,
					255
					) / 255.0;
				uint cB = _Lines[inst].colorB;
				half4 rgbaB = half4(
					(cB >>  0) & 0xff,
					(cB >>  8) & 0xff,
					(cB >> 16) & 0xff,
					255
					) / 255.0;
	#endif
				half4 diffuse = (rgbaB - rgbaA) * V.z + rgbaA;
				diffuse *= _Tint;
				
                o.color = diffuse;				
				return o;
			}

			half4 frag(half3 color : COLOR) : SV_Target
			{
				return half4(color, 1);
			}

			ENDCG
		}
	}
}
