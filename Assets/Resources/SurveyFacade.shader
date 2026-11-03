Shader "Hudson/SurveyFacade"
{
    Properties { _MainTex ("UV reference", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        struct Input {float2 uv_MainTex;float4 color:COLOR;};
        float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float glass=step(.9,IN.color.a),brick=1-step(.1,IN.color.a);
            float2 p=IN.uv_MainTex,f=frac(p);
            float2 edge=max(fwidth(p)*.6,.018);
            float left=lerp(.19,.045,glass),right=1-left;
            float bottom=lerp(.22,.045,glass),top=lerp(.79,.955,glass);
            float window=smoothstep(left-edge.x,left+edge.x,f.x)*(1-smoothstep(right-edge.x,right+edge.x,f.x))*smoothstep(bottom-edge.y,bottom+edge.y,f.y)*(1-smoothstep(top-edge.y,top+edge.y,f.y));
            window=lerp(window,lerp(.35,.82,glass),saturate(max(edge.x,edge.y)*2));
            float variation=hash(floor(p));
            float3 baseColor=max(GammaToLinearSpace(IN.color.rgb),float3(.018,.021,.025));
            float3 glazing=lerp(float3(.043,.065,.078),float3(.10,.15,.18),variation);
            glazing=lerp(glazing,baseColor*(.7+variation*.35),glass);
            float2 masonry=frac(p*float2(10,18)+float2(floor(p.y*18)*.5,0));
            float mortar=(1-smoothstep(.025,.065,min(masonry.x,masonry.y)))*brick;
            mortar*=1-saturate(max(fwidth(p.x)*10,fwidth(p.y)*18)*2);
            baseColor=lerp(baseColor,baseColor*1.15,mortar*.35);
            o.Albedo=lerp(baseColor,glazing,window);
            o.Metallic=window*lerp(.12,.38,glass);
            o.Smoothness=lerp(.18,lerp(.48,.7,glass),window);
            o.Occlusion=lerp(1,.83,window*(1-glass));
        }
        ENDCG
    }
    Fallback "Standard"
}
