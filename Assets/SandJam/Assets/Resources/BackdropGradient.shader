// The full-screen backdrop gradient (vertex colours). Opaque and drawn first: Sprites/Default blended the whole
// screen every frame, and tile GPUs can skip what the board and cubes cover only for opaque draws.
Shader "SandJamTest/BackdropGradient"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Opaque" "IgnoreProjector"="True" }
        Pass
        {
            ZWrite Off Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; fixed4 color:COLOR; };
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.color=v.color; return o; }
            fixed4 frag(v2f i):SV_Target { return fixed4(i.color.rgb,1); }
            ENDCG
        }
    }
    Fallback Off
}
