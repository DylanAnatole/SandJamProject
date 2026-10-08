Shader "SandJamTest/SandStream"
{
    Properties { _Color ("Sand color", Color) = (1,1,1,1) _RegionMask("Region boundary",2D)="black"{} _ClipToRegion("Clip local falling sand",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;sampler2D _RegionMask;float4x4 _WorldToBoard;float4 _BoardSize;float _ClipToRegion;
            struct input { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct output { float4 position:SV_POSITION; float2 uv:TEXCOORD0;float2 boardUV:TEXCOORD1; };
            output vert(input v){output o;o.position=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.boardUV=mul(_WorldToBoard,mul(unity_ObjectToWorld,v.vertex)).xy/max(_BoardSize.xy,float2(.001,.001));return o;}
            fixed4 frag(output i):SV_Target
            {
                if(_ClipToRegion>.5){
                clip(i.boardUV.x);clip(i.boardUV.y);clip(1-i.boardUV.x);clip(1-i.boardUV.y);
                clip(tex2D(_RegionMask,i.boardUV).a-.5);
                }
                float grain=frac(sin(floor(i.uv.x*180-_Time.y*110)*12.9898)*43758.5453);
                float edge=saturate(1-abs(i.uv.y*2-1));
                return fixed4(_Color.rgb*(.85+grain*.3),edge*(.35+grain*.6));
            }
            ENDCG
        }
    }
}
