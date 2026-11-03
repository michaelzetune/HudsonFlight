Shader "Hudson/DaylightSky"
{
    Properties { _SunDirection ("Sun direction",Vector)=(0,1,0,0) }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _SunDirection;
            struct v2f {float4 pos:SV_POSITION;float3 ray:TEXCOORD0;};
            v2f vert(float4 v:POSITION){v2f o;o.pos=UnityObjectToClipPos(v);o.ray=v.xyz;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float3 d=normalize(i.ray);float up=saturate(d.y);
                float3 sky=lerp(float3(.63,.72,.78),float3(.12,.33,.59),pow(up,.45));
                float sun=saturate(dot(d,normalize(_SunDirection.xyz)));
                sky+=float3(1,.87,.65)*pow(sun,110)*.22;
                sky+=float3(1,.93,.8)*smoothstep(.99965,.99985,sun)*2;
                return float4(sky,1);
            }
            ENDCG
        }
    }
}
