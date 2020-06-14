
Shader "iviz/PointCloud2"
{
	Properties
	{
		_IntensityTexture("Color Texture", 2D) = "white"
	}

	SubShader
	{
		Cull Off

		Pass
		{
			CGPROGRAM
			#include "UnityCG.cginc"

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ USE_TEXTURE
			#pragma multi_compile_instancing

			sampler2D _IntensityTexture;
			float _IntensityCoeff;
			float _IntensityAdd;
			float4x4 _LocalToWorld;
			float4x4 _WorldToLocal;
	        float4 _Tint;

			struct Point {
				float3 position;
#if USE_TEXTURE
				float intensity;
#else
				int intensity;
#endif
			};

			StructuredBuffer<float2> _Quad;
			StructuredBuffer<Point> _Points;

			struct v2f
			{
				float4 position : SV_POSITION;
				half3 color : COLOR;
			};

			v2f vert(uint id : SV_VertexID, uint inst : SV_InstanceID)
			{
				unity_ObjectToWorld = _LocalToWorld;
				unity_WorldToObject = _WorldToLocal;

				float2 extent = abs(UNITY_MATRIX_P._11_22);
				float2 quadVertex = _Quad[id];
				Point centerPoint = _Points[inst];
				float4 centerVertex = float4(centerPoint.position, 1);

				v2f o;
				o.position = UnityObjectToClipPos(centerVertex) + float4(quadVertex * extent, 0, 0);
	#if USE_TEXTURE
				float centerIntensity = centerPoint.intensity;
				o.color = tex2Dlod(_IntensityTexture, float4(centerIntensity * _IntensityCoeff + _IntensityAdd, 0, 0, 0));
	#else
				int centerIntensity = centerPoint.intensity;
				half3 rgb = half3(
					(centerIntensity >>  0) & 0xff,
					(centerIntensity >>  8) & 0xff,
					(centerIntensity >> 16) & 0xff
					) / 255.0;
				o.color = rgb;
	#endif
				o.color *= _Tint;
				return o;
			}

			half4 frag(v2f i) : SV_Target
			{
				return half4(i.color, 1);
			}

			ENDCG
		}
	}
}
