// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "PicaVoxel/PicaVoxel Unlit" {
	Properties{
		_Tint("Tint", Color) = (1,1,1,1)
	}
		SubShader{
			Tags{ "RenderType" = "Opaque" }
			LOD 200

			Pass{
			Tags{ "LightMode" = "Always" }

			Fog{ Mode Off }
			ZWrite On
			ZTest LEqual
			Cull Back
			Lighting Off

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest

				float4 _Tint;

				struct appdata {
					float4 vertex : POSITION;
					float4 color: COLOR;
				};

				struct v2f {
					float4 vertex : POSITION;
					float4 color: COLOR;
				};

				v2f vert(appdata v) {
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.color = v.color;
					return o;
				}

				fixed4 frag(v2f i) : COLOR{
					return i.color * _Tint;
				}
			ENDCG

		}
	}
		Fallback "VertexLit"
}