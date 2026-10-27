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
        _Albedo ("Use the texture as colour (props like the padlock)", Range(0,1)) = 0
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
            struct v2f { float4 pos:SV_POSITION; };
            v2f vert(appdata_base v)
            {
                v2f o;float3 p=mul(unity_ObjectToWorld,v.vertex).xyz;
                p+=normalize(UnityObjectToWorldNormal(v.normal))*_OutlineWidth;
                o.pos=mul(UNITY_MATRIX_VP,float4(p,1));
                // No outline (sand inside the glass, back rows): every vertex goes outside the view, so the GPU drops
                // the hull before shading it (a per-pixel clip shaded the whole hull and then threw it away).
                if(_OutlineWidth<=.0001)o.pos=float4(2,2,2,1);
                return o;
            }
            fixed4 frag(v2f i):SV_Target { return _OutlineColor; }
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
            sampler2D _MainTex;float4 _MainTex_ST;fixed4 _Color;float _GrainStrength,_Gloss,_ShadeStrength,_GrainContrast,_Albedo;
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
                fixed3 tex=tex2D(_MainTex,i.uv).rgb;
                float sampleGrain=saturate((tex.r-.75)*_GrainContrast+.75);
                float grain=lerp(1,.60+.43*sampleGrain,_GrainStrength);
                float highlight=pow(saturate(dot(n,normalize(l+view))),28)*_Gloss;
                float rim=pow(1-saturate(dot(n,view)),3)*.07;
                // No clip / discard anywhere in this opaque shader: on tile GPUs (iPhone, most Android) one would turn
                // off hidden-surface removal for every cube, rail and pad drawn with it.
                return fixed4(_Color.rgb*lerp(1,tex,_Albedo)*grain*(lighting+rim)+highlight*.45,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
