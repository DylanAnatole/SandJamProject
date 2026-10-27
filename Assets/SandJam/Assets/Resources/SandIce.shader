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
                // Original freeze cube (Freeze intro gif): bright cyan ice, the top face almost white, the sides a
                // saturated sky blue with the recovered frost texture showing faintly, rims lit; it reads as solid ice on
                // the dark lanes too (a mostly see-through shell turned navy over them).
                float3 n=normalize(i.normal);
                float edge=pow(1-abs(dot(n,normalize(_WorldSpaceCameraPos-i.world))),2);
                float3 frost=tex2D(_MainTex,i.uv).rgb;
                float lum=dot(frost,float3(.3,.5,.2));
                float3 ice=lerp(float3(.16,.55,1),float3(.66,.92,1),saturate((lum-.45)*2.2)); // frost streaks on the sides
                ice=lerp(ice,float3(.72,.94,1)*(.85+.3*lum),saturate(n.y*1.6-.2)); // lighter top face
                ice=lerp(ice,float3(.92,1,1),edge*.75);
                return fixed4(ice,(.9+edge*.1)*_Opacity);
            }
            ENDCG
        }
    }
}
