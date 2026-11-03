using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HudsonFlight
{
    public sealed class WorldBuilder
    {
        public Runway[] Runways { get; private set; }
        public readonly List<Vector3[]> Coastlines = new List<Vector3[]>();
        public const int GroundLayer=8, ObstacleLayer=9;
        readonly Dictionary<string,Material> materials = new Dictionary<string,Material>();
        public Material Material(Color color)
        {
            string key=color.ToString();
            if (!materials.TryGetValue(key,out var material))
            {
                material=new Material(Resources.Load<Material>("FlightSurface")); material.color=color;
                material.SetFloat("_Glossiness",.14f); materials[key]=material;
            }
            return material;
        }
        public GameObject Cube(string name, Vector3 position, Vector3 scale, Color color,
                               Transform parent=null, bool collider=false, int layer=ObstacleLayer)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;
            obj.transform.SetParent(parent,false);obj.transform.localPosition=position;
            obj.transform.localScale=scale;obj.GetComponent<Renderer>().sharedMaterial=Material(color);
            obj.layer=layer;
            if (!collider) UnityEngine.Object.Destroy(obj.GetComponent<Collider>());
            return obj;
        }
        public void Build()
        {
            string root=Path.Combine(Application.streamingAssetsPath,"Scenery");
            Runways=JsonUtility.FromJson<RunwayCollection>(File.ReadAllText(Path.Combine(root,"runways.json"))).runways;
            var data=JsonUtility.FromJson<SceneryCollection>(File.ReadAllText(Path.Combine(root,"world.json")));
            var water=Cube("Atlantic & harbor",new Vector3(0,-2,0),new Vector3(200000,4,200000),new Color(.12f,.30f,.38f),null,true,GroundLayer);
            water.GetComponent<Renderer>().sharedMaterial=new Material(Resources.Load<Shader>("Harbor"));
            var facade=new Material(Resources.Load<Shader>("CityFacade"));
            foreach (var item in data.meshes)
            {
                var obj=new GameObject(item.name);bool ground=item.kind=="land" || item.kind=="park" || item.kind=="airport";
                obj.layer=ground?GroundLayer:ObstacleLayer;
                var mesh=new Mesh {name=item.name,indexFormat=IndexFormat.UInt32};
                var vertices=new Vector3[item.vertices.Length/3];
                for(int i=0;i<vertices.Length;i++) vertices[i]=new Vector3(item.vertices[i*3],item.vertices[i*3+1],item.vertices[i*3+2]);
                mesh.vertices=vertices;mesh.triangles=item.triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
                if(item.styles!=null && item.styles.Length==vertices.Length)
                {
                    var colors=new Color32[vertices.Length];
                    for(int i=0;i<colors.Length;i++)colors[i]=new Color32((byte)(item.styles[i]&255),(byte)(item.styles[i]>>8),0,255);
                    mesh.colors32=colors;
                }
                obj.AddComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=obj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial=item.kind=="building"?facade:Material(new Color(item.color[0],item.color[1],item.color[2]));
                if(item.kind=="airport" || item.kind=="park")renderer.enabled=false;
                obj.AddComponent<MeshCollider>().sharedMesh=mesh;
                obj.isStatic=true;
            }
            VisualAssets.Aerial();
            SurveyBuildings.Build();
            VisualAssets.Landmarks();
            foreach(var r in Runways) BuildRunway(r);
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.48f,.57f,.69f);
            RenderSettings.ambientEquatorColor=new Color(.38f,.43f,.48f);
            RenderSettings.ambientGroundColor=new Color(.21f,.22f,.20f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogColor=new Color(.66f,.74f,.8f);RenderSettings.fogDensity=.000032f;
            var sun=new GameObject("Clear daylight").AddComponent<Light>();sun.type=LightType.Directional;
            sun.intensity=1.2f;sun.color=new Color(1f,.96f,.89f);sun.shadows=LightShadows.Soft;
            sun.transform.rotation=Quaternion.Euler(38,-48,0);sun.shadowBias=.035f;sun.shadowNormalBias=.2f;
            var sky=new Material(Resources.Load<Shader>("DaylightSky"));sky.SetVector("_SunDirection",-sun.transform.forward);RenderSettings.skybox=sky;RenderSettings.sun=sun;
            QualitySettings.shadowDistance=2800;QualitySettings.shadowResolution=ShadowResolution.VeryHigh;QualitySettings.shadowCascades=4;QualitySettings.antiAliasing=4;
            QualitySettings.anisotropicFiltering=AnisotropicFiltering.ForceEnable;
            var probe=new GameObject("Daylight reflection").AddComponent<ReflectionProbe>();
            probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;
            probe.cullingMask=0;probe.clearFlags=ReflectionProbeClearFlags.Skybox;probe.size=Vector3.one*150000;probe.resolution=128;
            DynamicGI.UpdateEnvironment();
        }
        void BuildRunway(Runway r)
        {
            var parent=new GameObject(r.airport+" runway "+r.name).transform;
            parent.position=(r.A+r.B)/2;parent.rotation=Quaternion.Euler(0,r.Heading,0);
            float length=Vector3.Distance(r.A,r.B);
            var pavement=Cube("Runway",Vector3.zero,new Vector3(r.width,.3f,length),new Color(.19f,.21f,.22f),parent,true,GroundLayer);
            pavement.GetComponent<Renderer>().sharedMaterial=new Material(Resources.Load<Shader>("RunwaySurface"));
            Color white=new Color(.89f,.87f,.79f);
            for(float z=-length/2+65;z<length/2-65;z+=60)
                Cube("Centerline",new Vector3(0,.17f,z),new Vector3(.9f,.025f,30),white,parent);
            for(int side=-1;side<=1;side+=2)
            {
                Cube("Edge stripe",new Vector3(side*(r.width/2-1),.17f,0),new Vector3(.5f,.025f,length-10),white,parent);
                for(float z=-length/2+25;z<length/2;z+=60)
                    Cube("Edge light",new Vector3(side*(r.width/2+1),.3f,z),new Vector3(.6f,.4f,.6f),white,parent);
                for(int end=-1;end<=1;end+=2)
                {
                    for(int stripe=0;stripe<4;stripe++)
                        Cube("Threshold stripe",new Vector3(side*(4+stripe*3),.17f,end*(length/2-30)),new Vector3(1.8f,.025f,25),white,parent);
                    Cube("Aiming point",new Vector3(side*9,.17f,end*(length/2-300)),new Vector3(5,.025f,45),white,parent);
                }
            }
            string[] ends=r.name.Split('/');
            for(int end=0;end<2;end++)
            {
                var text=new GameObject("Runway number").AddComponent<TextMesh>();text.text=ends[end];
                text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.GetComponent<Renderer>().sharedMaterial=text.font.material;
                text.fontSize=100;text.characterSize=.16f;text.anchor=TextAnchor.MiddleCenter;text.color=white;
                text.transform.SetParent(parent,false);text.transform.localPosition=new Vector3(0,.2f,(end==0?-1:1)*(length/2-85));
                text.transform.localRotation=Quaternion.Euler(90,end==0?0:180,0);
            }
        }
        public Transform BuildAircraft()
        {
            return VisualAssets.Aircraft();
        }
    }
}
