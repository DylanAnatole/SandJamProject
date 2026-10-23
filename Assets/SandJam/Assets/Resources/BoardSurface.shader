Shader "SandJamTest/BoardSurface"
{
    Properties { _MainTex ("Original sand", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct App { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            struct Varying { float4 position:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            sampler2D _MainTex;
            Varying vert(App i) { Varying o; o.position=UnityObjectToClipPos(i.vertex); o.color=i.color; o.uv=i.uv; return o; }
            fixed4 frag(Varying i):SV_Target { return fixed4(i.color.rgb * lerp(.90,1,tex2D(_MainTex,i.uv).r),1); }
            ENDCG
        }
    }
}
