Shader "SandJamTest/SandToonDepth"
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
        _FillEnabled ("Character fill enabled", Float) = 0
        _FillAmount ("Remaining sand", Range(0,1)) = 1
        _FillBottom ("Fill world bottom", Float) = 0
        _FillTop ("Fill world top", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "OUTLINE"
            Cull Front ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _OutlineWidth; fixed4 _OutlineColor;
            float _FillEnabled,_FillAmount,_FillBottom,_FillTop;
            struct v2f { float4 pos:SV_POSITION; float height:TEXCOORD0; };
            v2f vert(appdata_base v)
            {
                v2f o;float3 p=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.height=saturate((p.y-_FillBottom)/max(.001,_FillTop-_FillBottom));
                p+=normalize(UnityObjectToWorldNormal(v.normal))*_OutlineWidth;
                o.pos=mul(UNITY_MATRIX_VP,float4(p,1));return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                clip(_OutlineWidth-.0001);
                // Do not let the back-face outline fill the emptied, transparent interior.
                if(_FillEnabled>.5 && _FillAmount<.999)
                { clip(_FillAmount-.001);clip(_FillAmount-i.height); }
                return _OutlineColor;
            }
            ENDCG
        }
        Pass
        {
            Name "TOON"
            Cull Back ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float4 _MainTex_ST;fixed4 _Color;float _GrainStrength,_Gloss,_ShadeStrength,_GrainContrast;
            float _FillEnabled,_FillAmount,_FillBottom,_FillTop;
            struct v2f { float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float2 uv:TEXCOORD1;float3 world:TEXCOORD2; };
            v2f vert(appdata_base v)
            {
                v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);
                o.uv=TRANSFORM_TEX(v.texcoord,_MainTex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float3 n=normalize(i.normal),l=normalize(float3(-.45,.8,-.7));
                float3 view=normalize(_WorldSpaceCameraPos-i.world);
                float diffuse=dot(n,l);
                float lighting=.53+.40*smoothstep(-.2,.65,diffuse)+.12*smoothstep(.65,.96,diffuse);
                lighting=lerp(1,lighting,_ShadeStrength);
                float sampleGrain=saturate((tex2D(_MainTex,i.uv).r-.75)*_GrainContrast+.75);
                float grain=lerp(1,.60+.43*sampleGrain,_GrainStrength);
                float highlight=pow(saturate(dot(n,normalize(l+view))),28)*_Gloss;
                float rim=pow(1-saturate(dot(n,view)),3)*.07;
                float3 filled=_Color.rgb*grain*(lighting+rim)+highlight*.45;
                float height=saturate((i.world.y-_FillBottom)/max(.001,_FillTop-_FillBottom));
                float fill=smoothstep(height-.025,height+.025,_FillAmount);
                fill=lerp(fill,1,step(.999,_FillAmount));
                fill*=step(.001,_FillAmount);
                // Discard the empty interior instead of painting a misleading grey colour.
                float edge=smoothstep(.38,.78,1-abs(dot(n,view)));
                edge=max(edge,smoothstep(.91,.98,height)*.85);
                if(_FillEnabled>.5) clip(max(fill,edge)-.5);
                float3 shell=_Color.rgb*(.8+.2*lighting);
                return fixed4(lerp(filled,lerp(shell,filled,fill),_FillEnabled),1);
            }
            ENDCG
        }
    }
    Fallback Off
}
