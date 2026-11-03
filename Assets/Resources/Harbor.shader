Shader "Hudson/Harbor"
{
    Properties { _Color ("Water", Color) = (.075,.17,.19,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        struct Input {float3 worldPos;};
        fixed4 _Color;
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float2 p=IN.worldPos.xz;
            float a=sin(p.x*.17+p.y*.12+_Time.y*.5),b=sin(p.x*.37-p.y*.29+_Time.y*.7);
            o.Albedo=_Color.rgb*(1+a*.04);
            o.Normal=normalize(float3(a*.09,b*.06,1));
            o.Metallic=.4;o.Smoothness=.86;o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Standard"
}
