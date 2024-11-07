Shader "Minecraft/Cloud Shader"
{
	Properties 
	{
		_Color ("Color", Color) = (1, 1, 1, 1)
	}

	SubShader
	{
		Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
	
		ZWrite off
		Lighting off
		Fog { Mode off }

		Blend SrcAlpha OneMinusSrcAlpha

		Pass 
		{
			Stencil 
			{
				Ref 1
				Comp Greater
				Pass IncrSat
			}

			Color[_Color]
		}
	}
}