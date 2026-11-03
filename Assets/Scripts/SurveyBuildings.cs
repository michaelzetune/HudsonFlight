using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HudsonFlight
{
    [Serializable] public class SurveyChunk {public string file,texture;public int vertices,triangles;}
    [Serializable] public class SurveyCollection {public SurveyChunk[] chunks;public int buildings,facadeTagsMatched,landmarkReplacements;}
    public static class SurveyBuildings
    {
        public static void Build()
        {
            string root=Path.Combine(Application.streamingAssetsPath,"Visuals/survey");
            var survey=JsonUtility.FromJson<SurveyCollection>(File.ReadAllText(Path.Combine(root,"survey.json")));
            var facade=new Material(Resources.Load<Shader>("SurveyFacade"));
            var roofs=new Dictionary<string,Material>();
            foreach(var chunk in survey.chunks)
            {
                using(var reader=new BinaryReader(File.OpenRead(Path.Combine(root,chunk.file))))
                {
                    if(reader.ReadUInt32()!=0x4d534648)throw new IOException("Invalid survey mesh: "+chunk.file);
                    int count=reader.ReadInt32(),indices=reader.ReadInt32();
                    if(count!=chunk.vertices || indices!=chunk.triangles*3)throw new IOException("Survey mesh count mismatch");
                    var vertices=new Vector3[count];var uv=new Vector2[count];var colors=new Color32[count];var triangles=new int[indices];
                    for(int i=0;i<count;i++)vertices[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                    for(int i=0;i<count;i++)uv[i]=new Vector2(reader.ReadSingle(),reader.ReadSingle());
                    for(int i=0;i<count;i++)colors[i]=new Color32(reader.ReadByte(),reader.ReadByte(),reader.ReadByte(),reader.ReadByte());
                    for(int i=0;i<indices;i++)triangles[i]=reader.ReadInt32();
                    var mesh=new Mesh {name=chunk.file,indexFormat=IndexFormat.UInt32};
                    mesh.vertices=vertices;mesh.uv=uv;mesh.colors32=colors;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
                    var go=new GameObject("NYC survey "+chunk.file);go.isStatic=true;go.layer=WorldBuilder.ObstacleLayer;
                    go.AddComponent<MeshFilter>().sharedMesh=mesh;
                    Material material=facade;
                    if(!string.IsNullOrEmpty(chunk.texture))
                    {
                        if(!roofs.TryGetValue(chunk.texture,out material))
                        {
                            material=new Material(Resources.Load<Material>("FlightSurface"));material.mainTexture=VisualAssets.Texture(chunk.texture);
                            material.color=Color.white;material.SetFloat("_Glossiness",.09f);roofs[chunk.texture]=material;
                        }
                    }
                    go.AddComponent<MeshRenderer>().sharedMaterial=material;
                    go.AddComponent<MeshCollider>().sharedMesh=mesh;
                }
            }
            Debug.Log($"CITY SURVEY: {survey.buildings} buildings, {survey.facadeTagsMatched} recorded facade styles, {survey.landmarkReplacements} replaced by landmark models");
        }
    }
}
