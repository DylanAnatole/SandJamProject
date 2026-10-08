Shader "SandJamTest/IceShell"
{
    Properties { _MainTex("Recovered ice",2D)="white"{} _Opacity("Opacity",Range(0,1))=1 }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float _Opacity;
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;float3 world:TEXCOORD2;};
            v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy+float2(.5,.5);o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float edge=pow(1-abs(dot(normalize(i.normal),normalize(_WorldSpaceCameraPos-i.world))),2);
                float3 ice=tex2D(_MainTex,i.uv).rgb;
                return fixed4(lerp(ice,float3(.78,.95,1),edge*.7),(.72+edge*.2)*_Opacity);
            }
            ENDCG
        }
    }
}
