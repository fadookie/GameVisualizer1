// Upgrade NOTE: replaced 'SeperateSpecular' with 'SeparateSpecular'

Shader "Eliot/Test/TestShader1" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Color ("Main Color", Color) = (1, 0.5, 0.5, 1)
	}
	SubShader {
		Pass {
			Material {
				Diffuse[_Color]
			}
			Lighting On
			SetTexture [_MainTex] {
				constantColor [_Color]
				Combine texture * primary DOUBLE, texture * constant
			}	
		}
	}
	SubShader {
		Pass {
			Lighting Off
			SetTexture [_MainTex] {}
		}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
			Lighting Off
		
		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			half4 c = tex2D (_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
			//o.Alpha = c.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
