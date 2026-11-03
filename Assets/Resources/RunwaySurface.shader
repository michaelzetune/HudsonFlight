Shader "Hudson/RunwaySurface"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        struct Input {float3 worldPos;};
        float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float2 p=IN.worldPos.xz;
            float detail=lerp(hash(floor(p*28)),.5,saturate(length(fwidth(p))*14));
            float grain=detail*.025+hash(floor(p*2))*.012;
            float seams=step(.025,frac(p.x/5))*step(.012,frac(p.y/8));
            o.Albedo=float3(.135,.145,.15)+grain-seams*.009;
            o.Smoothness=.15;o.Metallic=0;
        }
        ENDCG
    }
    Fallback "Standard"
}
