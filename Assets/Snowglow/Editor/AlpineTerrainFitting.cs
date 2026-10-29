using System;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Snowglow.Editor {
 // All mesh authoring happens before Play. Shared source/catalog meshes stay intact.
 public static class AlpineTerrainFitting {
  const string Folder=CatalogPreparation.Root+"/SceneMeshes/AlpineMountains";
  static void Require(bool value,string message){if(!value)throw new InvalidOperationException("Alpine terrain fitting: "+message);}
  static CatalogPlacement[] Mountains(Scene scene)=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CatalogPlacement>()).Where(p=>(p.placementId??"").StartsWith("pinefall-mountain-",StringComparison.Ordinal)).ToArray();
  static Vector3[] Vertices(Mesh mesh){
   using(var data=MeshUtility.AcquireReadOnlyMeshData(mesh))
   using(var values=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp)){data[0].GetVertices(values);return values.ToArray();}
  }
  static int[] Triangles(Mesh mesh){
   using(var data=MeshUtility.AcquireReadOnlyMeshData(mesh)){
    Require(data[0].subMeshCount==1,"expected one material per mesh");
    using(var values=new NativeArray<int>(data[0].GetSubMesh(0).indexCount,Allocator.Temp)){data[0].GetIndices(values,0);return values.ToArray();}
   }
  }
  static float Ground(Terrain terrain,Vector3 at)=>terrain.SampleHeight(at)+terrain.transform.position.y;
  static float BaseY(CatalogPlacement placement,CatalogAsset entry)=>placement.transform.TransformPoint(new Vector3(0,entry.localBounds.min.y,0)).y;
  static float Embed(CatalogPlacement placement,CatalogAsset entry,Terrain terrain){
   var source=entry.prefab.GetComponentsInChildren<MeshFilter>(true).Single(m=>m.name=="Rock");var target=placement.GetComponentsInChildren<MeshFilter>(true).Single(m=>m.name=="Rock");
   var vertices=Vertices(source.sharedMesh);var triangles=Triangles(source.sharedMesh);float bottom=BaseY(placement,entry),embed=2;
   var world=vertices.Select(v=>target.transform.TransformPoint(v)).ToArray();var low=world.Select(v=>Mathf.Abs(v.y-bottom)<.02f).ToArray();
   // A base edge can bridge a terrain hollow even when both endpoints touch.
   // Check side-wall base edges and tuck them below the full sampled edge.
   for(int t=0;t<triangles.Length;t+=3){
    int a=triangles[t],b=triangles[t+1],c=triangles[t+2];if(low[a]&&low[b]&&low[c])continue;
    int[] face={a,b,c};
    for(int edge=0;edge<3;edge++){
     int first=face[edge],second=face[(edge+1)%3];if(!low[first]||!low[second])continue;
     float start=Ground(terrain,world[first]),end=Ground(terrain,world[second]);
     for(int sample=0;sample<=32;sample++){
      float along=sample/32f;embed=Mathf.Max(embed,Mathf.Lerp(start,end,along)-Ground(terrain,Vector3.Lerp(world[first],world[second],along))+.75f);
     }
    }
   }
   Require(embed<entry.localBounds.size.y*placement.transform.lossyScale.y*.15f,"base requires more than 15% burial: "+placement.placementId);return embed;
  }
  static Mesh CreateMesh(Mesh source,Transform target,CatalogPlacement placement,CatalogAsset entry,Terrain terrain,float embed){
   var positions=Vertices(source);float bottom=BaseY(placement,entry);
   for(int i=0;i<positions.Length;i++){
    var world=target.TransformPoint(positions[i]);float height=world.y-bottom;
    world.y=Ground(terrain,world)+height-embed;positions[i]=target.InverseTransformPoint(world);
   }
   Vector2[] uv;
   using(var data=MeshUtility.AcquireReadOnlyMeshData(source))
   using(var values=new NativeArray<Vector2>(data[0].vertexCount,Allocator.Temp)){data[0].GetUVs(0,values);uv=values.ToArray();}
   var mesh=new Mesh{name=placement.placementId+"-"+target.name+"-terrain-fit",vertices=positions,uv=uv};mesh.SetTriangles(Triangles(source),0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
  }
  public static void Fit(Scene scene,CatalogAsset[] entries){
   Require(!Application.isPlaying,"Editor-only authoring");Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
   var terrain=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Terrain>()).Single();var report=new StringBuilder("Terrain fitting: source-relative heights follow local Terrain; original X/Z, yaw and scale retained.\n");
   foreach(var placement in Mountains(scene)){
    var entry=entries.Single(a=>a.id==placement.assetId);var slot=placement.transform.parent;var at=slot.position;at.y=Ground(terrain,at);slot.position=at;
    float embed=Embed(placement,entry,terrain);
    foreach(var target in placement.GetComponentsInChildren<MeshFilter>(true)){
     var source=entry.prefab.GetComponentsInChildren<MeshFilter>(true).Single(m=>m.name==target.name);var mesh=CreateMesh(source.sharedMesh,target.transform,placement,entry,terrain,embed);
     string path=Folder+"/"+placement.placementId+"-"+target.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
     if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}else{saved=mesh;AssetDatabase.CreateAsset(saved,path);}
     target.sharedMesh=saved;
    }
    // Conservative background collision remains a box, adjusted to fitted bounds.
    var bounds=CatalogValidation.BoundsOf(placement.gameObject);
    foreach(var box in placement.GetComponentsInChildren<BoxCollider>(true)){
     box.center=box.transform.InverseTransformPoint(bounds.center);var scale=box.transform.lossyScale;box.size=new Vector3(bounds.size.x/Mathf.Abs(scale.x),bounds.size.y/Mathf.Abs(scale.y),bounds.size.z/Mathf.Abs(scale.z));
    }
    report.AppendLine(placement.placementId+": buried base "+embed.ToString("F2")+"m; source height "+(entry.localBounds.size.y*placement.transform.lossyScale.y).ToString("F2")+"m.");
   }
   AssetDatabase.SaveAssets();Directory.CreateDirectory("Logs/AlpineMountains");File.WriteAllText("Logs/AlpineMountains/TerrainFit.txt",report.ToString());Validate(scene,entries);
  }
  public static void Validate(Scene scene,CatalogAsset[] entries){
   var terrain=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Terrain>()).Single();var report=new StringBuilder("PASS: fitted mesh validation.\n");
   foreach(var placement in Mountains(scene)){
    var entry=entries.Single(a=>a.id==placement.assetId);float minCap=float.PositiveInfinity,maxCap=float.NegativeInfinity,maxBase=float.NegativeInfinity;
    foreach(var target in placement.GetComponentsInChildren<MeshFilter>(true)){
     var source=entry.prefab.GetComponentsInChildren<MeshFilter>(true).Single(m=>m.name==target.name);var original=Vertices(source.sharedMesh);var fitted=Vertices(target.sharedMesh);
     Require(original.Length==fitted.Length&&source.sharedMesh.GetIndexCount(0)==target.sharedMesh.GetIndexCount(0),"geometry count changed");Require(AssetDatabase.GetAssetPath(target.sharedMesh).StartsWith(Folder+"/",StringComparison.Ordinal),"mesh is not a persisted scene copy");
     float bottom=BaseY(placement,entry);
     for(int i=0;i<fitted.Length;i++){
      var world=target.transform.TransformPoint(fitted[i]);float relief=world.y-Ground(terrain,world);
      if(target.name=="SnowCap"){minCap=Mathf.Min(minCap,relief);maxCap=Mathf.Max(maxCap,relief);}
      else if(Mathf.Abs(target.transform.TransformPoint(original[i]).y-bottom)<.02f)maxBase=Mathf.Max(maxBase,relief);
     }
    }
    float height=entry.localBounds.size.y*placement.transform.lossyScale.y;
    Require(maxBase<-.5f&&minCap>0&&maxCap>height*.85f,"buried cap or floating base: "+placement.placementId);
    report.AppendLine(placement.placementId+": cap relief "+minCap.ToString("F2")+".."+maxCap.ToString("F2")+"m; highest base vertex "+maxBase.ToString("F2")+"m; height "+height.ToString("F2")+"m.");
   }
   File.WriteAllText("Logs/AlpineMountains/TerrainFitValidation.txt",report.ToString());
  }
 }
}
