// Simplified Alpha Blended Particle shader. Differences from regular Alpha Blended Particle one:
// - no Tint color
// - no Smooth particle support
// - no AlphaTest
// - no ColorMask

Shader "Eliot/Mobile/Particle/Alpha-clip" {
Properties {
	_MainTex ("Particle Texture", 2D) = "white" {}
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Blend SrcAlpha OneMinusSrcAlpha
	Cull Off Lighting Off ZWrite Off Fog { Color (0,0,0,0) }
	
	BindChannels {
		Bind "Color", color
		Bind "Vertex", vertex
		Bind "TexCoord", texcoord
	}
	
	SubShader {
		CGPROGRAM
		#pragma surface surf Lambert
	      struct Input {
	          float2 uv_MainTex;
	          float3 worldPos;
	      };
	      sampler2D _MainTex;
	      void surf (Input IN, inout SurfaceOutput o) {
	          clip (frac((IN.worldPos.y+IN.worldPos.z*0.1) * 5) - 0.5);
	          o.Albedo = tex2D (_MainTex, IN.uv_MainTex).rgb;
	          o.Alpha = tex2D (_MainTex, IN.uv_MainTex).a;
	          //o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));
	      }
		ENDCG
	}
	SubShader {
		Pass {
			SetTexture [_MainTex] {
				combine texture * primary
			}
		}
	}
}
}
