Shader "Hudson/CityFacade"
{
    Properties { _Color ("Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        struct Input { float3 worldPos; float3 worldNormal; float4 color:COLOR; };
        float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float3 n=IN.worldNormal;
            float2 p=float2(dot(IN.worldPos.xz,float2(-n.z,n.x)),IN.worldPos.y);
            float seed=IN.color.r, modern=step(.56,IN.color.g);
            float2 cell=p/float2(lerp(2.8,1.7,modern),3.25);
            float2 f=frac(cell);
            float window=smoothstep(.13,.16,f.x)*(1-smoothstep(.80,.83,f.x))*smoothstep(.21,.24,f.y)*(1-smoothstep(.78,.81,f.y));
            window=lerp(window,.4,smoothstep(.8,2.5,max(fwidth(cell.x),fwidth(cell.y))));
            float variation=hash(floor(cell)+seed*119);
            float3 stone=lerp(float3(.29,.25,.22),float3(.59,.55,.47),seed);
            float3 frame=lerp(stone,float3(.23,.28,.31),modern);
            float3 glass=lerp(float3(.09,.14,.17),float3(.31,.40,.45),variation);
            glass+=float3(.025,.035,.05)*sin(IN.worldPos.y*.045);
            float noise=hash(floor(IN.worldPos.xz*3)+floor(IN.worldPos.y*2))*.035;
            float roof=step(.65,abs(n.y));
            float3 roofColor=lerp(float3(.20,.21,.20),float3(.43,.42,.39),seed)+noise;
            o.Albedo=lerp(lerp(frame,glass,window)+noise,roofColor,roof);
            o.Metallic=lerp(window*lerp(.12,.5,modern),.04,roof);
            o.Smoothness=lerp(lerp(.18,.68,window),.12,roof);
            o.Occlusion=lerp(1,.8,window);
        }
        ENDCG
    }
    Fallback "Standard"
}
