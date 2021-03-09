Shader "PicaVoxel/PicaVoxel Diffuse With Offset" {
	Properties {
		_Tint ("Tint", Color) = (1,1,1,1)
		_Offset("Offset", float) = 0
	}
	SubShader {
		Offset[_Offset],[_Offset]

		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
			#pragma surface surf Lambert

			float4 _Tint;

			struct Input {
				float4 color: COLOR;
			};


			void surf (Input IN, inout SurfaceOutput o) {
				o.Albedo = (IN.color * _Tint);
				o.Alpha = _Tint.a;
			}
		ENDCG
	
	}

	Fallback "VertexLit"
}
