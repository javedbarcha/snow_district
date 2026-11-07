using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace Snowglow.Editor {
 public static class GeometryAudit {
 const string Request="Logs/GeometryAudit.request",Folder="Reports/GeometryAudit";
 [Serializable]public class Row{public string path,category,assetId,owner,mesh,meshId;public long vertices,triangles;public int slots;public bool active,enabled,castsShadows,transparent;public string[] materials;}
 [Serializable]public class Entry{public string id,name,kind,prefab;public long vertices,triangles;public int renderers,slots,materials,colliders,placements;}
 [Serializable]public class TerrainRow{public string name;public int resolution;public long fullGridVertices,fullGridTriangles;public float pixelError,basemapDistance;public bool enabled,instanced;}
 [Serializable]public class Cull{public string owner;public float distance;}
 [Serializable]public class Particle{public string path;public int maxParticles,meshVertices,meshTriangles;public bool active;}
 [Serializable]public class Data{public string unity,target,scene,sceneHash;public List<Row> rows=new List<Row>();public List<Entry> catalog=new List<Entry>();public List<TerrainRow> terrains=new List<TerrainRow>();public List<Cull> culling=new List<Cull>();public List<Particle> particles=new List<Particle>();public int colliders,enabledColliders,lights,enabledLights,unmeasuredRenderers;}
 static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
 static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
 static Mesh GetMesh(Renderer r){if(r is SkinnedMeshRenderer sk)return sk.sharedMesh;var f=r.GetComponent<MeshFilter>();return f?f.sharedMesh:null;}
 static long Tris(Mesh m){long sum=0;for(int i=0;i<m.subMeshCount;i++)if(m.GetTopology(i)==MeshTopology.Triangles)sum+=(long)m.GetIndexCount(i)/3;return sum;}
 static string Category(string id,string path){
  if(id.StartsWith("alpine-tree-"))return "Trees and wood (LOD3)";
  if(id.Contains("mountain"))return "Mountains";
  if(id.Contains("home")||id.Contains("cottage")||id.Contains("colonial")||id.Contains("barn"))return "Homes";
  if(id.Contains("sign"))return "Sign boards";
  if(id.Contains("cloud"))return "Clouds";
  if(id.Contains("street")||id.Contains("lamp"))return "Street lights";
  if(id.Contains("snowman"))return "Snowmen";
  if(id.Contains("garage")||id.Contains("fuel-station")||id.Contains("disposal-yard"))return "Facilities";
  if(id.Contains("traction"))return "Traction ramp";
  if(id.Contains("town-vehicle")||id.Contains("parked"))return "Parked vehicles";
  if(!string.IsNullOrEmpty(id))return "Other catalog scenery";
  if(path.Contains("Snowfield")||path.Contains("SNOWFIELD")||path.Contains("Snow |")||path.Contains("Snow field"))return "Interactive snow";
  if(path.Contains("Road")||path.Contains("road")||path.Contains("Curb")||path.Contains("curb"))return "Roads, curbs and markings";
  if(path.Contains("Fence")||path.Contains("fence")||path.Contains("Foundation")||path.Contains("foundation"))return "Lot foundations and fences";
  return "Other authored scenery";
 }
 [InitializeOnLoadMethod]static void Register(){EditorApplication.update+=Pending;}
 static void Pending(){if(!File.Exists(Request)){EditorApplication.update-=Pending;return;}if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;EditorApplication.update-=Pending;File.Delete(Request);try{Run();}catch(Exception e){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/Failure.txt",e.ToString());Debug.LogException(e);}}
 [MenuItem("Snowglow/Audit/Saved scene geometry inventory (read only)")]
 public static void Run(){
  Directory.CreateDirectory(Folder);if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play for saved asset audit");
  string scenePath=SceneValidation.ScenePath;string before=Hash128.Compute(File.ReadAllBytes(scenePath)).ToString();var active=SceneManager.GetActiveScene();bool dirty=active.isDirty;Scene scene=default;
  try{
   scene=EditorSceneManager.OpenPreviewScene(scenePath);var d=new Data{unity=Application.unityVersion,target=EditorUserBuildSettings.activeBuildTarget.ToString(),scene=scenePath,sceneHash=before};
   var placements=All<CatalogPlacement>(scene);
   foreach(var r in All<Renderer>(scene)){
    if(r is ParticleSystemRenderer)continue;
    Mesh m=null;if(r is SkinnedMeshRenderer skin)m=skin.sharedMesh;else{var filter=r.GetComponent<MeshFilter>();if(filter)m=filter.sharedMesh;}
    if(!m){d.unmeasuredRenderers++;continue;}
    var p=r.GetComponentInParent<CatalogPlacement>();var machine=r.GetComponentInParent<MachineController>();var snow=r.GetComponentInParent<SnowField>();string id=p?p.assetId:"",path=PathOf(r.transform);
    var mats=r.sharedMaterials;AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m,out string guid,out long local);
    d.rows.Add(new Row{path=path,category=machine?"Playable machinery":snow?"Interactive snow":Category(id,path),assetId=id,owner=p?p.placementId:machine?machine.name:path,mesh=AssetDatabase.GetAssetPath(m),meshId=guid+":"+local,vertices=m.vertexCount,triangles=Tris(m),slots=mats.Length,materials=mats.Where(x=>x).Select(x=>AssetDatabase.GetAssetPath(x)).Distinct().ToArray(),active=r.gameObject.activeInHierarchy,enabled=r.enabled,castsShadows=r.shadowCastingMode!=ShadowCastingMode.Off,transparent=mats.Any(x=>x&&x.renderQueue>=3000)});
   }
   foreach(var guid in AssetDatabase.FindAssets("t:CatalogAsset")){
    var a=AssetDatabase.LoadAssetAtPath<CatalogAsset>(AssetDatabase.GUIDToAssetPath(guid));if(!a||!a.prefab)continue;
    var rs=a.prefab.GetComponentsInChildren<Renderer>(true).Where(x=>!(x is ParticleSystemRenderer)).ToArray();var meshes=rs.Select(GetMesh).Where(m=>m).ToArray();
    d.catalog.Add(new Entry{id=a.id,name=a.displayName,kind=a.kind.ToString(),prefab=AssetDatabase.GetAssetPath(a.prefab),vertices=meshes.Sum(m=>(long)m.vertexCount),triangles=meshes.Sum(m=>Tris(m)),renderers=rs.Length,slots=rs.Sum(r=>r.sharedMaterials.Length),materials=rs.SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().Count(),colliders=a.prefab.GetComponentsInChildren<Collider>(true).Length,placements=placements.Count(p=>p.assetId==a.id)});
   }
   foreach(var t in All<Terrain>(scene)){int n=t.terrainData.heightmapResolution;d.terrains.Add(new TerrainRow{name=t.name,resolution=n,fullGridVertices=(long)n*n,fullGridTriangles=2L*(n-1)*(n-1),pixelError=t.heightmapPixelError,basemapDistance=t.basemapDistance,enabled=t.enabled,instanced=t.drawInstanced});}
   foreach(var c in All<DistanceVisibility>(scene))d.culling.Add(new Cull{owner=PathOf(c.transform),distance=c.distance});
   foreach(var p in All<ParticleSystem>(scene)){var r=p.GetComponent<ParticleSystemRenderer>();var m=r&&r.renderMode==ParticleSystemRenderMode.Mesh?r.mesh:null;d.particles.Add(new Particle{path=PathOf(p.transform),maxParticles=p.main.maxParticles,meshVertices=m?m.vertexCount:0,meshTriangles=m?(int)Tris(m):0,active=p.gameObject.activeInHierarchy});}
   var cs=All<Collider>(scene);d.colliders=cs.Length;d.enabledColliders=cs.Count(c=>c.enabled&&c.gameObject.activeInHierarchy);var ls=All<Light>(scene);d.lights=ls.Length;d.enabledLights=ls.Count(l=>l.enabled&&l.gameObject.activeInHierarchy);
   File.WriteAllText(Folder+"/NativeInventory.json",JsonUtility.ToJson(d,true));
  }finally{if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);}
  if(before!=Hash128.Compute(File.ReadAllBytes(scenePath)).ToString()||SceneManager.GetActiveScene()!=active||active.isDirty!=dirty)throw new InvalidOperationException("Audit changed saved scene or active scene state");
  if(File.Exists(Folder+"/Failure.txt"))File.Delete(Folder+"/Failure.txt");
  File.WriteAllText(Folder+"/Validation.txt","PASS: read-only native Unity mesh inventory; saved scene hash and active scene/dirty state preserved. No asset saves, runtime entry, geometry changes or player-save writes.");Debug.Log("Geometry audit written to "+Folder);
 }
 }
}
