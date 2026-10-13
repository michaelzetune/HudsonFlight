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
            Cube("Atlantic & harbor",new Vector3(0,-2,0),new Vector3(200000,4,200000),new Color(.12f,.30f,.38f),null,true,GroundLayer);
            foreach (var item in data.meshes)
            {
                var obj=new GameObject(item.name);bool ground=item.kind=="land" || item.kind=="park" || item.kind=="airport";
                obj.layer=ground?GroundLayer:ObstacleLayer;
                var mesh=new Mesh {name=item.name,indexFormat=IndexFormat.UInt32};
                var vertices=new Vector3[item.vertices.Length/3];
                for(int i=0;i<vertices.Length;i++) vertices[i]=new Vector3(item.vertices[i*3],item.vertices[i*3+1],item.vertices[i*3+2]);
                mesh.vertices=vertices;mesh.triangles=item.triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
                obj.AddComponent<MeshFilter>().sharedMesh=mesh;
                obj.AddComponent<MeshRenderer>().sharedMaterial=Material(new Color(item.color[0],item.color[1],item.color[2]));
                obj.AddComponent<MeshCollider>().sharedMesh=mesh;
                obj.isStatic=true;
            }
            foreach(var r in Runways) BuildRunway(r);
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.64f,.76f,.88f);
            RenderSettings.ambientEquatorColor=new Color(.54f,.59f,.60f);
            RenderSettings.ambientGroundColor=new Color(.26f,.28f,.25f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogColor=new Color(.67f,.79f,.86f);RenderSettings.fogDensity=.000024f;
            var sun=new GameObject("Clear daylight").AddComponent<Light>();sun.type=LightType.Directional;
            sun.intensity=1.15f;sun.color=new Color(1f,.96f,.87f);sun.shadows=LightShadows.Soft;
            sun.transform.rotation=Quaternion.Euler(48,-35,0);
            QualitySettings.shadowDistance=1800;QualitySettings.antiAliasing=4;
        }
        void BuildRunway(Runway r)
        {
            var parent=new GameObject(r.airport+" runway "+r.name).transform;
            parent.position=(r.A+r.B)/2;parent.rotation=Quaternion.Euler(0,r.Heading,0);
            float length=Vector3.Distance(r.A,r.B);
            Cube("Runway",Vector3.zero,new Vector3(r.width,.3f,length),new Color(.19f,.21f,.22f),parent,true,GroundLayer);
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
            var root=new GameObject("C172P").transform;
            Color white=new Color(.9f,.91f,.87f),red=new Color(.65f,.13f,.12f),dark=new Color(.07f,.11f,.13f);
            Cube("Fuselage",new Vector3(0,0,0),new Vector3(1.1f,1.15f,5.5f),white,root);
            Cube("Cowling",new Vector3(0,-.18f,2.7f),new Vector3(.92f,.8f,1.4f),white,root);
            Cube("Windshield",new Vector3(0,.35f,1.2f),new Vector3(1.12f,.7f,.07f),dark,root);
            Cube("Wing",new Vector3(0,.75f,.2f),new Vector3(10.9f,.14f,1.5f),white,root);
            Cube("Tailplane",new Vector3(0,.25f,-2.5f),new Vector3(3.4f,.1f,.85f),white,root);
            Cube("Tail fin",new Vector3(0,.9f,-2.55f),new Vector3(.12f,1.4f,1f),red,root);
            foreach(float x in new[]{-1.2f,1.2f})
            {
                Cube("Main gear",new Vector3(x*.7f,-.65f,-.1f),new Vector3(.08f,.9f,.08f),dark,root);
                Cube("Wheel",new Vector3(x,-1.08f,-.1f),new Vector3(.16f,.44f,.44f),dark,root);
                Cube("Accent",new Vector3(Mathf.Sign(x)*.56f,-.13f,-.8f),new Vector3(.015f,.13f,3.7f),red,root);
            }
            Cube("Nose gear",new Vector3(0,-.9f,2),new Vector3(.15f,.7f,.28f),dark,root);
            Cube("Propeller",new Vector3(0,0,3.46f),new Vector3(.12f,1.9f,.06f),dark,root);
            return root;
        }
    }
}
