Shader "Hudson/PropellerBlur"
{
    Properties { _MainTex("Motion texture",2D)="white"{} _Color("Tint",Color)=(1,1,1,.32) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;float4 _Color;
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
            v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;return o;}
            fixed4 frag(v2f i):SV_Target {fixed4 c=tex2D(_MainTex,i.uv);return fixed4(c.rgb,c.a*_Color.a);}
            ENDCG
        }
    }
}
