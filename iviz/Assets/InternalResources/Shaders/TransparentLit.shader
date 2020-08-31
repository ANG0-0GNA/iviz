﻿Shader "iviz/TransparentLit"
{
	Properties
	{
		_Color("Diffuse Color", Color) = (1,1,1,1)
		_Smoothness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metalness", Range(0,1)) = 0.5
	}
		SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent"}
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard addshadow fullforwardshadows alpha
		#pragma target 3.0

		struct Input {
			float dummy;
		};

		half _Smoothness;
		half _Metallic;

		UNITY_INSTANCING_BUFFER_START(Props)
		UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
		UNITY_DEFINE_INSTANCED_PROP(fixed4, _EmissiveColor)
		UNITY_INSTANCING_BUFFER_END(Props)


		void surf(Input IN, inout SurfaceOutputStandard o) {
			fixed4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
			o.Albedo = color.rgb;
			o.Smoothness = _Smoothness;
			o.Alpha = color.a;
            o.Metallic = _Metallic * o.Alpha;
            o.Emission = UNITY_ACCESS_INSTANCED_PROP(Props, _EmissiveColor).rgb;
		}
		ENDCG
	}
	FallBack "Standard"
}

