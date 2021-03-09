Shader "PicaVoxel/PicaVoxel Diffuse with Fader" {
	Properties {
		_Tint ("Tint", Color) = (1,1,1,1)
		_FadeColor ("Fade Color", Color) = (1,1,1,1)
		_FadeAmount ("Fade Amount", Range(0,1)) = 0
	}
	SubShader {

	
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
			#pragma surface surf Lambert

			float4 _Tint;
			float _FadeAmount;
			float4 _FadeColor;

			struct Input {
				float4 color: COLOR;
			};


			void surf (Input IN, inout SurfaceOutput o) {
				o.Albedo = lerp((IN.color * _Tint), _FadeColor, _FadeAmount);
				o.Alpha = _Tint.a;
			}
		ENDCG
	
	}

	Fallback "VertexLit"
}
