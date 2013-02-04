// Unlit shader. Simplest possible textured shader.
// - no lighting
// - no lightmap support
// - no per-material color

Shader "Eliot/Unlit/Texture-zignore" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
}

SubShader {
	Tags { "RenderType"="Opaque" }
	LOD 100
	Ztest always
	Zwrite off
	
	Pass {
		Lighting Off
		SetTexture [_MainTex] { combine texture } 
	}
}
}
