Shader "SandJamTest/OriginalSandPreview"
{
    Properties { _MainTex ("Original sand texture", 2D) = "white" {} _Color ("Original palette", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct Varying { float4 position : SV_POSITION; float3 normal : TEXCOORD0; float2 uv : TEXCOORD1; };
            sampler2D _MainTex;
            float4 _Color;
            Varying vert(Input v) { Varying o; o.position = UnityObjectToClipPos(v.vertex); o.normal = UnityObjectToWorldNormal(v.normal); o.uv = v.uv; return o; }
            fixed4 frag(Varying i) : SV_Target
            {
                float light = saturate(dot(normalize(i.normal), normalize(float3(-.4,.7,-.6))));
                float grain = lerp(.80, 1.0, tex2D(_MainTex, i.uv).r);
                return fixed4(_Color.rgb * grain * (.63 + light * .48), 1);
            }
            ENDCG
        }
    }
}
