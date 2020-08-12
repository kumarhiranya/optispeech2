//TONGUE Shader

Shader "Transparent/Specular/ZtestAlways" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 0)
	_Shininess ("Shininess", Range (0.01, 1)) = 0.01
	_MainTex ("Base (RGB) TransGloss (A)", 2D) = "white" {}
}

SubShader {
	Tags {"Queue"="Transparent+2" "IgnoreProjector"="True" "RenderType"="Geometry"}//Transparent"}
	Ztest Always
	//Lighting On
	LOD 300

//
	// extra pass that renders to depth buffer only
    Pass {
        ZWrite On
        ColorMask 0
    }

//

    UsePass "Transparent/Specular/FORWARD"
//
}

Fallback "Geometry/VertexLit"//Transparent/VertexLit"
}
