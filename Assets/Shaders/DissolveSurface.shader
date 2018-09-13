﻿Shader "Custom/DissolveSurface" {
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0

		//Dissolve properties
		_DissolveTexture("Dissolve Texutre", 2D) = "white" {}
		_DissolveAmount("Dissolve Amount", Range(0,1)) = 0
}

SubShader{
	Tags{ "RenderType" = "Opaque" }
	LOD 200
	Cull Off //Fast way to turn your material double-sided

	CGPROGRAM
	#pragma surface surf Standard fullforwardshadows

	#pragma target 3.0

	sampler2D _MainTex;

	struct Input {
		float2 uv_MainTex;
	};

	half _Glossiness;
	half _Metallic;
	fixed4 _Color;

	//Dissolve properties
	sampler2D _DissolveTexture;
	half _DissolveAmount;

	void surf(Input IN, inout SurfaceOutputStandard o) {

		//Dissolve function
		half dissolve_value = tex2D(_DissolveTexture, IN.uv_MainTex).r;
		clip(dissolve_value - _DissolveAmount);

		//Basic shader function
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

		o.Albedo = c.rgb;
		o.Metallic = _Metallic;
		o.Smoothness = _Glossiness;
		o.Alpha = c.a;

		if (dissolve_value - _DissolveAmount < .05f) //outline width = .05f
			o.Emission = fixed3(1, 1, 1); //emits white color
	}
	
	ENDCG
	}
	FallBack "Diffuse"
}