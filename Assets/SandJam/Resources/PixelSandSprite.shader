Shader "SandJamTest/PixelSandSprite"
{
    Properties { [PerRendererData] _MainTex ("Sand grid", 2D) = "white" {}
        _BoardSurface ("Rounded board glass", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex;
            float _BoardSurface;
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 color=tex2D(_MainTex,i.uv)*i.color;
                if(_BoardSurface>.5)
                {
                // Rounded glass inset. Keep the original grid and colours untouched.
                float2 p=abs(i.uv-.5)*float2(.75,1);
                float2 corner=max(p-float2(.348,.473),0);
                clip(.027-length(corner));
                float reflection=pow(saturate(1-abs(i.uv.x-i.uv.y*.48-.22)*18),12)*.035;
                color.rgb=lerp(color.rgb,1,reflection);
                }
                return color;
            }
            ENDCG
        }
    }
}
