using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HudsonFlight
{
    [Serializable] public class AerialTile {public float x,z,size;public int resolution;public string file;}
    [Serializable] public class AerialCollection {public AerialTile[] tiles;}
    [Serializable] public class AircraftPart
    {public string name,texture;public float[] vertices,uv,color;public int[] triangles;public float alpha;public bool glass;}
    [Serializable] public class AircraftCollection {public AircraftPart[] meshes;}

    public static class VisualAssets
    {
        static readonly Dictionary<string,Texture2D> TextureCache=new Dictionary<string,Texture2D>();
        static string Root=>Path.Combine(Application.streamingAssetsPath,"Visuals");
        public static Texture2D Texture(string file)
        {
            if(TextureCache.TryGetValue(file,out var cached))return cached;
            var t=new Texture2D(2,2,TextureFormat.RGB24,true);
            if(!t.LoadImage(File.ReadAllBytes(Path.Combine(Root,file))))throw new IOException("Invalid texture: "+file);
            t.name=file;t.anisoLevel=8;t.wrapMode=TextureWrapMode.Clamp;t.Apply(true,true);TextureCache[file]=t;return t;
        }
        public static void Aerial()
        {
            var collection=JsonUtility.FromJson<AircraftCollection>(File.ReadAllText(Path.Combine(Root,"aerial-ground.json")));
            foreach(var t in collection.meshes)
            {
                var go=new GameObject("USGS ortho "+t.name);go.isStatic=true;
                var vertices=new Vector3[t.vertices.Length/3];var uv=new Vector2[vertices.Length];
                for(int i=0;i<vertices.Length;i++){vertices[i]=new Vector3(t.vertices[i*3],t.vertices[i*3+1],t.vertices[i*3+2]);uv[i]=new Vector2(t.uv[i*2],t.uv[i*2+1]);}
                var mesh=new Mesh {indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=t.triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                var material=new Material(Resources.Load<Material>("FlightSurface"));material.mainTexture=Texture(t.texture);
                material.color=Color.white;material.SetFloat("_Glossiness",.07f);
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            }
        }
        public static Transform Aircraft()=>Models("aircraft","aircraft.json",true);
        public static void Landmarks()=>Models("landmarks","landmarks.json",false);
        static Transform Models(string folder,string file,bool aircraft)
        {
            var root=new GameObject(aircraft?"C172P detailed · c172p-team":"NYC landmarks · FlightGear contributors").transform;
            var collection=JsonUtility.FromJson<AircraftCollection>(File.ReadAllText(Path.Combine(Root,folder,file)));
            var textures=new Dictionary<string,Texture2D>();var materials=new Dictionary<string,Material>();
            foreach(var part in collection.meshes)
            {
                var go=new GameObject(part.name);go.transform.SetParent(root,false);
                var mesh=new Mesh {name=part.name,indexFormat=IndexFormat.UInt32};
                var vertices=new Vector3[part.vertices.Length/3];var uv=new Vector2[vertices.Length];
                for(int i=0;i<vertices.Length;i++){vertices[i]=new Vector3(part.vertices[i*3],part.vertices[i*3+1],part.vertices[i*3+2]);uv[i]=new Vector2(part.uv[i*2],part.uv[i*2+1]);}
                // Spin around the propeller hub rather than the aircraft origin.
                if(aircraft && part.name=="Propeller")
                {
                    Vector3 pivot=new Vector3(0,-.05f,1.8f);go.transform.localPosition=pivot;
                    for(int i=0;i<vertices.Length;i++)vertices[i]-=pivot;
                }
                mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=part.triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
                go.AddComponent<MeshFilter>().sharedMesh=mesh;
                bool blurredProp=part.name=="Propeller.Fast";
                string key=part.texture+"/"+part.alpha+"/"+string.Join(",",part.color);
                if(!materials.TryGetValue(key,out var material))
                {
                    material=part.glass?new Material(Resources.Load<Shader>("AircraftGlass")):blurredProp?new Material(Resources.Load<Shader>("PropellerBlur")):new Material(Resources.Load<Material>("FlightSurface"));
                    material.color=new Color(part.color[0],part.color[1],part.color[2],part.alpha);
                    if(part.glass)material.color=new Color(.3f,.45f,.52f,.08f);
                    if(blurredProp)material.color=new Color(1,1,1,.32f);
                    if(!part.glass && !string.IsNullOrEmpty(part.texture))
                    {
                        if(!textures.TryGetValue(part.texture,out var texture)){texture=Texture(folder+"/"+part.texture);textures[part.texture]=texture;}
                        material.mainTexture=texture;
                    }
                    if(!part.glass && !blurredProp){material.SetFloat("_Glossiness",aircraft?.38f:.23f);material.SetFloat("_Metallic",aircraft?.08f:.02f);}
                    if(part.alpha<.99f && !part.glass && !blurredProp)
                    {
                        material.SetFloat("_Mode",3);material.SetInt("_SrcBlend",(int)BlendMode.One);material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite",0);material.EnableKeyword("_ALPHAPREMULTIPLY_ON");material.renderQueue=3000;
                    }
                    materials[key]=material;
                }
                var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
                if(part.glass || blurredProp)renderer.shadowCastingMode=ShadowCastingMode.Off;
                if(!aircraft){go.isStatic=true;go.layer=WorldBuilder.ObstacleLayer;go.AddComponent<MeshCollider>().sharedMesh=mesh;}
            }
            return root;
        }
    }
}
