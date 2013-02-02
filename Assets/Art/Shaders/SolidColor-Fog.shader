Shader "Eliot/SolidColor-Fog" 

{
	Properties 
    {
        _Color ("Main Color", Color) = (1,.5,.5,1)
        _FogColor ("Fog Color", Color) = (0.5, 0.5, 0.5, 1)
        _FogNearRange ("Fog Near Range", Float) = 1000
        _FogFarRange ("Fog Far Range", FloaT) = 1500
    }
 
    SubShader 
    {
        Pass 
        {
          Color [_Color]
          Fog {
			Mode Linear
			Range [_FogNearRange], [_FogFarRange]
			Color [_FogColor]
          }
        }
    }
}