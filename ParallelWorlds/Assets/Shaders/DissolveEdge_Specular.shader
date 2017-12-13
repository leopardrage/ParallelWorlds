Shader "Custom/DissolveEdge (Specular)" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_NormalMap ("NormalMap", 2D) = "bump" {}
		_SpecularMap ("SpecularMap", 2D) = "white" {}

		[Header(Dissolve)]
		[Enum(Off,0,Front,1,Back,2)] _CullMode ("Culling Mode", int) = 0
		[Enum(Off,0,On,1)] _ZWrite("ZWrite", int) = 0
		_Progress("Progress",Range(0,1)) = 0
		_DissolveTex("Dissolve Texture", 2D) = "white" {}
		_Edge("Edge",Range(0.01,0.5)) = 0.01

		[Header(Edge Color)]
		[Toggle(EDGE_COLOR)] _UseEdgeColor("Edge Color?", Float) = 1
		[HideIfDisabled(EDGE_COLOR)][NoScaleOffset] _EdgeAroundRamp("Edge Ramp", 2D) = "white" {}
		[HideIfDisabled(EDGE_COLOR)]_EdgeAround("Edge Color Range",Range(0,0.5)) = 0
		[HideIfDisabled(EDGE_COLOR)]_EdgeAroundPower("Edge Color Power",Range(1,5)) = 1
		[HideIfDisabled(EDGE_COLOR)]_EdgeAroundHDR("Edge Color HDR",Range(1,3)) = 1
		[HideIfDisabled(EDGE_COLOR)]_EdgeDistortion("Edge Distortion",Range(0,1)) = 0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		Cull[_CullMode] ZWrite[_ZWrite]
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf StandardSpecular fullforwardshadows

		#pragma shader_feature EDGE_COLOR

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _NormalMap;
		sampler2D _SpecularMap;

		struct Input {
			float2 uv_MainTex;
			float2 uv_NormalMap;
			float2 uv_SpecularMap;
			float2 uv_DissolveTex;
		};

		half _Glossiness;
		fixed4 _Color;

		// Dissolve stuff
		sampler2D _DissolveTex;
		fixed _Edge;
		fixed _Progress;

		// Edge Color
		#ifdef EDGE_COLOR
			sampler2D _EdgeAroundRamp;
			fixed _EdgeAround;
			float _EdgeAroundPower;
			float _EdgeAroundHDR;
			fixed _EdgeDistortion;
		#endif

		// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
		// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
		// #pragma instancing_options assumeuniformscaling
		UNITY_INSTANCING_CBUFFER_START(Props)
			// put more per-instance properties here
		UNITY_INSTANCING_CBUFFER_END

		void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			//o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Normal = UnpackNormal (tex2D (_NormalMap, IN.uv_NormalMap));
			o.Specular = tex2D (_SpecularMap, IN.uv_SpecularMap).rgb;
			o.Alpha = c.a;

			fixed4 dissolveColor = tex2D(_DissolveTex, IN.uv_DissolveTex);
			fixed dissolve = dissolveColor.r;
			fixed progress = _Progress;

			fixed edge = lerp(dissolve + _Edge, dissolve - _Edge, progress);
			fixed alpha = smoothstep(progress + _Edge, progress - _Edge, edge);

			//Edge
			#ifdef EDGE_COLOR
				//Edge Around Factor
				fixed edgearound = lerp( dissolve + _EdgeAround, dissolve - _EdgeAround, progress);
				edgearound = smoothstep( progress + _EdgeAround, progress - _EdgeAround, edgearound);
				edgearound = pow(edgearound, _EdgeAroundPower);

				//Edge Around Distortion
				fixed avoid = 0.15f;
				fixed distort = edgearound*alpha*avoid;
				float2 cuv = lerp( IN.uv_MainTex, IN.uv_MainTex + distort - avoid, progress * _EdgeDistortion);
				dissolveColor = tex2D(_MainTex, cuv);

				//Edge Around Color
				fixed3 ca = tex2D(_EdgeAroundRamp, fixed2(1-edgearound, 0)).rgb;
				ca = (dissolveColor.rgb + ca)*ca*_EdgeAroundHDR;
				dissolveColor.rgb = lerp( ca, dissolveColor.rgb, edgearound);

				o.Albedo = dissolveColor.rgb;
			#endif

			clip(alpha - 0.5);
		}
		ENDCG
	}
	FallBack "Diffuse"
}
