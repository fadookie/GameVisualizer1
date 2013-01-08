Shader "Masked Tint - Vertex Colors" {

Properties {
	_MainTex ("Texture  (A = Tint Mask)", 2D) = ""
}

SubShader {Pass {	// iPhone 3GS and later
	GLSLPROGRAM
	varying mediump vec2 uv;
	varying lowp vec3 color;
	
	#ifdef VERTEX
	void main() {
		gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
		uv = gl_MultiTexCoord0.xy;
		color = gl_Color.rgb;
	}
	#endif
	
	#ifdef FRAGMENT
	uniform lowp sampler2D _MainTex;
	void main() {
		vec4 texture = texture2D(_MainTex, uv);
		gl_FragColor = vec4(texture.rgb * (color * texture.a + (1. - texture.a)), 1);
	}
	#endif		
	ENDGLSL
}}

SubShader {Pass {	// pre-3GS devices, including the September 2009 8GB iPod touch
	BindChannels {
		Bind "vertex", vertex
		Bind "color", color
		Bind "texcoord", texcoord
	}
	SetTexture[_MainTex] {Combine texture alpha * one - primary}				
	SetTexture[_MainTex] {Combine texture * one - previous}
}}
 
}