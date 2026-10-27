// SandToonDepth without its outline pass, for blocks drawn with no outline (back rows, sand inside the glass): one
// draw per block instead of two. The look comes from the same TOON pass.
Shader "SandJamTest/SandToonDepthPlain"
{
    Properties
    {
        _MainTex ("Sand grain", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _GrainStrength ("Grain strength", Range(0,1)) = 0.85
        _OutlineColor ("Outline", Color) = (0.045,0.025,0.07,1)
        _OutlineWidth ("Outline world width", Float) = 0.014
        _ShadeStrength ("Depth shading", Range(0,1)) = 1
        _GrainContrast ("Grain contrast", Range(0,3)) = 1
        _Gloss ("Soft highlight", Range(0,1)) = 0.2
        _Albedo ("Use the texture as colour (props like the padlock)", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        UsePass "SandJamTest/SandToonDepth/TOON"
    }
}
