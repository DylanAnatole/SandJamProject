Shader "SandJamTest/IceShell"
{
    Properties { _MainTex("Recovered ice",2D)="white"{} _Opacity("Opacity",Range(0,1))=1 _Flash("Crack flash",Range(0,1))=0 }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Blend One OneMinusSrcAlpha ZWrite Off Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float _Opacity,_Flash;
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;float3 world:TEXCOORD2;};
            v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                // Original IceCube_0 material (Toony Colors, premultiplied): ice_new x pale blue (.87,.94,1), plus blue
                // emission (0,.34,.87) and a blue rim (0,.39,1, from .28 to 1.01); the texture's alpha (.7 in the middle
                // of a face, ~1 at its frosted rims) lets the cube's own colour show through the ice.
                float3 n=normalize(i.normal);
                float rim=smoothstep(.28,1.01,1-saturate(dot(n,normalize(_WorldSpaceCameraPos-i.world))));
                fixed4 frost=tex2D(_MainTex,i.uv);
                float lit=lerp(.7,1,saturate(n.y*1.5)); // toon ramp: the top face lit, the sides a step darker
                float3 ice=frost.rgb*float3(.87,.94,1)*lit+float3(0,.34,.87)+float3(0,.39,1)*rim*.5;
                ice=lerp(ice,float3(.85,.97,1),saturate(n.y*1.5)*.35); // the top face reads paler, almost white
                ice=lerp(ice,float3(1.2,1.25,1.25),_Flash*.85); // each count down flashes the ice white (Freeze gif)
                return fixed4(ice*_Opacity,frost.a*_Opacity);
            }
            ENDCG
        }
    }
}
