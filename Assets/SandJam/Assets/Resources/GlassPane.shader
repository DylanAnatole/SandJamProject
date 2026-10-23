Shader "SandJamTest/GlassPane"
{
    // Frosted glass walls of a draining cube (original waiting-slot boxes): faint white over the sand, brighter
    // towards the silhouette, slightly tinted by the cube colour. Drawn before ordinary transparent objects so the
    // count label stays crisp on top.
    Properties { _Color("Tint",Color)=(1,1,1,1) _Base("Face opacity",Range(0,1))=.26 _Edge("Edge opacity",Range(0,1))=.55 }
    SubShader
    {
        Tags {"Queue"="Transparent-100" "RenderType"="Transparent" "IgnoreProjector"="True"}
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;float _Base,_Edge;
            struct v2f {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 world:TEXCOORD1;};
            v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float facing=abs(dot(normalize(i.normal),normalize(_WorldSpaceCameraPos-i.world)));
                float edge=pow(1-facing,2.2);
                float3 c=lerp(_Color.rgb,float3(1,1,1),.6+.4*edge);
                return fixed4(c,saturate(_Base+_Edge*edge)*_Color.a);
            }
            ENDCG
        }
    }
}
