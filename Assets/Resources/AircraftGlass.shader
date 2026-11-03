Shader "Hudson/AircraftGlass"
{
    Properties { _Color ("Glass tint",Color)=(.3,.45,.52,.10) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off
        CGPROGRAM
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0
        fixed4 _Color;
        struct Input {float3 viewDir;};
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float fresnel=pow(1-saturate(abs(normalize(IN.viewDir).z)),4);
            o.Albedo=_Color.rgb;o.Metallic=.25;o.Smoothness=.95;
            o.Alpha=_Color.a+fresnel*.25;
        }
        ENDCG
    }
}
