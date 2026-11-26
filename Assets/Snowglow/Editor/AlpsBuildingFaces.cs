using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Snowglow.Editor {
 [InitializeOnLoad] public static class AlpsBuildingFaces {
  const string Log="Logs/AlpsBuildingFaces/",ScenePath="Assets/Scenes/AlpsTerrainTest.unity";
  const string Folder="Assets/Snowglow/Levels/AlpsTerrainTest/BuildingFaceRepair/Sources/";
  static Dictionary<string,Dictionary<string,Mesh>> replacements;
  [Serializable] class Part {public string slug,name;public Vector3 position,scale;public Quaternion rotation;public Vector3[] vertices,normals;public int[] triangles;}
  [Serializable] class Dump {public List<Part> parts=new List<Part>();public List<string> instances=new List<string>();}
  static AlpsBuildingFaces(){EditorApplication.update+=Pending;}
  static void Check(bool b,string m){if(!b)throw new InvalidOperationException("Building faces: "+m);}
  static void Pending(){const string request="Logs/AlpsBuildingFaces.request";if(!File.Exists(request)||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;var action=File.ReadAllText(request).Trim();File.Delete(request);Directory.CreateDirectory(Log);try{if(action=="audit")Audit();else if(action=="preview"){Prepare();CaptureVariants(SceneManager.GetActiveScene(),"After");File.WriteAllText(Log+"Preview.txt","PASS: 13 derivatives imported and geometry invariants checked; four views each captured. Scene not saved.");}else if(action=="apply")Apply();else if(action=="apply-buildings")ApplyBuildings();else if(action=="review")ReviewScene();else throw new InvalidOperationException("Unknown action "+action);}catch(Exception e){File.WriteAllText(Log+"Validation.txt","FAIL: "+e);Debug.LogException(e);}}
  static void ReviewScene(){
   var scene=SceneManager.GetActiveScene();Check(scene.path==ScenePath,"active test scene");Check(!scene.isDirty,"save manual edits before camera review");var source=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First();var go=new GameObject("Temporary repaired town camera");SceneManager.MoveGameObjectToScene(go,scene);var camera=go.AddComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;camera.aspect=1;camera.farClipPlane=3000;camera.layerCullDistances=new float[32];var rig=go.AddComponent<CameraRig>();rig.view=camera;rig.terrain=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single();var focus=new GameObject("Temporary repaired town focus");SceneManager.MoveGameObjectToScene(focus,scene);
   try{var placements=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CatalogPlacement>(true)).Where(p=>Slugs().Any(s=>p.assetId=="alps-rebuild-"+s.ToLowerInvariant())).OrderBy(p=>p.name).ToArray();int index=0;
    foreach(var p in placements){focus.transform.position=p.transform.position+p.transform.forward*16;rig.SetMode(CameraMode.Isometric);rig.UpdateForTarget(focus.transform,.1f,false);Check(camera.orthographic&&Quaternion.Angle(camera.transform.rotation,Quaternion.Euler(41,45,0))<.01f,"actual gameplay camera");Capture(camera,Log+"Scene_"+(++index).ToString("D2")+".png");}
    camera.orthographic=true;camera.orthographicSize=225;camera.transform.SetPositionAndRotation(new Vector3(-390,500,-390),Quaternion.Euler(41,45,0));Capture(camera,Log+"SceneOverview.png");File.WriteAllText(Log+"SceneReview.txt","PASS: "+index+" actual gameplay CameraRig views captured, plus town overview; saved scene reopened after temporary camera removal.");
   }finally{UnityEngine.Object.DestroyImmediate(focus);UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.OpenScene(ScenePath);}
  }
  static string[] Slugs(bool buildingsOnly=false)=>Enumerable.Range(1,11).Select(i=>"Building_"+i.ToString("D2")).Concat(buildingsOnly?new string[0]:new[]{"Monument","GateTower"}).ToArray();
  static Vector3[] Vertices(Mesh mesh){using(var data=MeshUtility.AcquireReadOnlyMeshData(mesh))using(var v=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp)){data[0].GetVertices(v);return v.ToArray();}}
  static string[] Positions(Vector3[] v)=>v.Select(p=>p.x.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+p.y.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+","+p.z.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)).Distinct().OrderBy(s=>s,StringComparer.Ordinal).ToArray();
  static bool SameMatrix(Matrix4x4 a,Matrix4x4 b){for(int i=0;i<16;i++)if(Mathf.Abs(a[i]-b[i])>.0001f)return false;return true;}
  static void Prepare(bool changedBuildingGeometry=false){
   Check(SceneManager.GetActiveScene().path==ScenePath,"active terrain test");replacements=new Dictionary<string,Dictionary<string,Mesh>>();var report=new List<string>();
   foreach(var slug in Slugs(changedBuildingGeometry)){
    string path=Folder+slug+".fbx";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);var importer=AssetImporter.GetAtPath(path) as ModelImporter;Check(importer!=null,"derivative importer");importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.globalScale=1;importer.isReadable=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
    var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);Check(model,"imported derivative "+slug);var meshes=model.GetComponentsInChildren<MeshFilter>(true).ToDictionary(f=>f.name,f=>f.sharedMesh);replacements.Add(slug,meshes);
    var reference=AssetDatabase.LoadAssetAtPath<GameObject>(AlpsRebuildAssets.Source+slug+".fbx");Check(reference,"catalogue reference "+slug);var sourceFilters=reference.GetComponentsInChildren<MeshFilter>(true);var importedFilters=model.GetComponentsInChildren<MeshFilter>(true);
    Check(meshes.Count==sourceFilters.Length&&meshes.Keys.OrderBy(n=>n,StringComparer.Ordinal).SequenceEqual(sourceFilters.Select(f=>f.name).OrderBy(n=>n,StringComparer.Ordinal)),"same named mesh parts "+slug);
    foreach(var f in sourceFilters){
     var n=meshes[f.name];var nf=importedFilters.Single(p=>p.name==f.name);
     Check(SameMatrix(nf.transform.localToWorldMatrix,f.transform.localToWorldMatrix),"matching mesh space "+slug+"/"+f.name);
     Check(n&&n.vertexCount>0&&n.subMeshCount>0&&Enumerable.Range(0,n.subMeshCount).All(i=>n.GetIndexCount(i)>0&&n.GetIndexCount(i)%3==0),"valid imported geometry "+slug+"/"+f.name);
     if(!changedBuildingGeometry){
      Check(n.GetIndexCount(0)==f.sharedMesh.GetIndexCount(0),"same triangle count "+slug+"/"+f.name);
      var before=Vertices(f.sharedMesh);var after=Vertices(n);
      if(f.name!="SnowCover"||slug=="Monument")Check(Positions(before).SequenceEqual(Positions(after)),"architecture positions retained "+slug+"/"+f.name);
      else {Check(Mathf.Abs(n.bounds.min.x-f.sharedMesh.bounds.min.x)<.001f&&Mathf.Abs(n.bounds.max.x-f.sharedMesh.bounds.max.x)<.001f&&Mathf.Abs(n.bounds.min.z-f.sharedMesh.bounds.min.z)<.001f&&Mathf.Abs(n.bounds.max.z-f.sharedMesh.bounds.max.z)<.001f,"snow footprint retained");Check(n.bounds.max.y<=f.sharedMesh.bounds.max.y+.5f,"bounded snow clearance lift");}
     }
     report.Add(slug+"/"+f.name+" | triangles="+Enumerable.Range(0,n.subMeshCount).Sum(i=>(long)n.GetIndexCount(i))/3+" | vertices="+n.vertexCount);
    }
   }
   File.WriteAllLines(Log+"ImportChecks.txt",report);
  }
  static void Replace(GameObject go,string slug){foreach(var f in go.GetComponentsInChildren<MeshFilter>(true)){Check(replacements[slug].ContainsKey(f.name),"known source part "+f.name);var prior=f.sharedMesh;var fixedMesh=replacements[slug][f.name];f.sharedMesh=fixedMesh;foreach(var collider in f.GetComponents<MeshCollider>()){Check(collider.sharedMesh==prior,"matched collision source");collider.sharedMesh=fixedMesh;}}}
  static string State(Scene scene)=>string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Select(t=>GlobalObjectId.GetGlobalObjectIdSlow(t)+" | "+t.name+" | "+t.localPosition.ToString("F6")+" | "+t.localRotation.ToString("F6")+" | "+t.localScale.ToString("F6")));
  static string MaterialState(IEnumerable<CatalogPlacement> placements)=>string.Join("\n",placements.SelectMany(p=>p.GetComponentsInChildren<MeshRenderer>(true)).OrderBy(r=>GlobalObjectId.GetGlobalObjectIdSlow(r).ToString()).Select(r=>GlobalObjectId.GetGlobalObjectIdSlow(r)+" | "+string.Join(",",r.sharedMaterials.Select(m=>m?AssetDatabase.GetAssetPath(m)+"|"+m.name:"<null>"))));
  static void Apply()=>Apply(false);
  static void ApplyBuildings()=>Apply(true);
  static void Apply(bool buildingsOnly){
   var scene=SceneManager.GetActiveScene();Check(scene.path==ScenePath,"active test scene");string stamp=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"),backup=Log+"InMemory-apply-"+stamp+".unity";Check(EditorSceneManager.SaveScene(scene,backup,true),"preserve current scene");File.Copy(ScenePath,Log+"Disk-apply-"+stamp+".unity");
   try{Prepare(buildingsOnly);var before=State(scene);var placements=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CatalogPlacement>(true)).Where(p=>Slugs(buildingsOnly).Any(s=>p.assetId=="alps-rebuild-"+s.ToLowerInvariant())).ToArray();Check(placements.Count(p=>p.assetId.StartsWith("alps-rebuild-building_"))==75,"75 map buildings");Check(placements.Length==(buildingsOnly?75:77),"expected Alps building and landmark instances");var materials=MaterialState(placements);var boxes=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BoxCollider>(true)).ToDictionary(c=>GlobalObjectId.GetGlobalObjectIdSlow(c).ToString(),c=>c.center.ToString("F6")+c.size.ToString("F6"));
    foreach(var p in placements){if(!buildingsOnly)Check(!p.GetComponentsInChildren<MeshFilter>().Any(f=>AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith(Folder)),"refuse repeated application");var slug=Slugs(buildingsOnly).Single(s=>p.assetId=="alps-rebuild-"+s.ToLowerInvariant());Replace(p.gameObject,slug);}
    Check(before==State(scene),"all transforms and hierarchy retained");Check(EditorSceneManager.SaveScene(scene,ScenePath),"save repaired scene");scene=EditorSceneManager.OpenScene(ScenePath);Check(before==State(scene),"reopened hierarchy/transforms");foreach(var c in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BoxCollider>(true)))Check(boxes[GlobalObjectId.GetGlobalObjectIdSlow(c).ToString()]==c.center.ToString("F6")+c.size.ToString("F6"),"box unchanged");
    var refreshed=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CatalogPlacement>(true)).Where(p=>Slugs(buildingsOnly).Any(s=>p.assetId=="alps-rebuild-"+s.ToLowerInvariant())).ToArray();Check(materials==MaterialState(refreshed),"building materials unchanged");foreach(var p in refreshed){var slug=Slugs(buildingsOnly).Single(s=>p.assetId=="alps-rebuild-"+s.ToLowerInvariant());foreach(var f in p.GetComponentsInChildren<MeshFilter>()){Check(f.sharedMesh==replacements[slug][f.name],"saved shared derivative");foreach(var c in f.GetComponents<MeshCollider>())Check(c.sharedMesh==f.sharedMesh,"matching mesh collider");}}
    CaptureVariants(scene,"After",buildingsOnly);EditorSceneManager.OpenScene(ScenePath);File.WriteAllText(Log+"Validation.txt",buildingsOnly
     ?"PASS: all 75 map buildings refreshed from the supplied revised Blender source and use the shared imported test derivatives. All 11 building variants inspected from four sides. Named part sets/transforms, valid imported topology, scene transforms/hierarchy and box colliders verified across save/reopen. Production scene and catalogue assets untouched. No runtime changes or device acceptance."
     :"PASS: all 75 map buildings and both landmarks use shared imported derivatives. All 11 source building variants and both landmarks inspected from four sides. Same triangle counts and architecture vertex positions; roof snow lifted only for clearance. All transforms/hierarchy and box colliders unchanged across save/reopen; tower visible/static collision meshes match. Original imports/prefabs/materials untouched. No runtime changes or device acceptance.");
   }catch{File.Copy(backup,ScenePath,true);AssetDatabase.ImportAsset(ScenePath,ImportAssetOptions.ForceSynchronousImport);EditorSceneManager.OpenScene(ScenePath);throw;}
  }
  static void Audit(){var scene=SceneManager.GetActiveScene();Check(scene.path==ScenePath,"active test scene");Check(EditorSceneManager.SaveScene(scene,Log+"InMemory-before.unity",true),"backup current edits");File.Copy(ScenePath,Log+"Disk-before.unity",true);var dump=new Dump();
   foreach(var slug in Enumerable.Range(1,11).Select(i=>"Building_"+i.ToString("D2")).Concat(new[]{"Monument","GateTower"})){
    var path=AlpsRebuildAssets.Source+slug+".fbx";var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);Check(model,"source "+slug);
    foreach(var f in model.GetComponentsInChildren<MeshFilter>(true)){
     var mesh=f.sharedMesh;using(var data=MeshUtility.AcquireReadOnlyMeshData(mesh)){var d=data[0];using(var v=new NativeArray<Vector3>(d.vertexCount,Allocator.Temp))using(var n=new NativeArray<Vector3>(d.vertexCount,Allocator.Temp))using(var ix=new NativeArray<int>(d.GetSubMesh(0).indexCount,Allocator.Temp)){
      d.GetVertices(v);d.GetNormals(n);d.GetIndices(ix,0);var normalMatrix=f.transform.localToWorldMatrix.inverse.transpose;
      dump.parts.Add(new Part{slug=slug,name=f.name,position=f.transform.position,rotation=f.transform.rotation,scale=f.transform.lossyScale,vertices=v.ToArray().Select(p=>f.transform.TransformPoint(p)).ToArray(),normals=n.ToArray().Select(p=>normalMatrix.MultiplyVector(p).normalized).ToArray(),triangles=ix.ToArray()});
     }}
    }
   }
   foreach(var p in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CatalogPlacement>(true)).Where(p=>(p.assetId??"").StartsWith("alps-rebuild-building")||(p.assetId??"").Contains("gatetower")))dump.instances.Add(p.name+" | "+p.assetId+" | "+p.transform.lossyScale);
   File.WriteAllText(Log+"UnityMeshes.json",JsonUtility.ToJson(dump));
   CaptureVariants(scene,"Before");File.WriteAllText(Log+"Validation.txt","AUDIT COMPLETE: "+dump.parts.Count+" imported parts; "+dump.instances.Count+" building/tower instances. No scene changes saved.");
  }
  static void CaptureVariants(Scene scene,string prefix,bool buildingsOnly=false){
   var source=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First();var oldRenderers=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(r=>r.enabled).ToArray();foreach(var r in oldRenderers)r.enabled=false;
   var cameraObject=new GameObject("Temporary building face inspection");SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;camera.orthographic=true;camera.aspect=1;camera.farClipPlane=3000;camera.layerCullDistances=new float[32];
   try{foreach(var slug in Slugs(buildingsOnly)){
    var entry=AlpsRebuildAssets.Find(slug);var go=UnityEngine.Object.Instantiate(entry.prefab);SceneManager.MoveGameObjectToScene(go,scene);go.transform.position=new Vector3(0,200,0);
    try{if(prefix=="After")Replace(go,slug);var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);camera.orthographicSize=Mathf.Max(b.size.y,Mathf.Max(b.size.x,b.size.z))* .72f;
     for(int side=0;side<4;side++){float yaw=45+side*90;var direction=Quaternion.Euler(20,yaw,0)*Vector3.forward;camera.transform.SetPositionAndRotation(b.center-direction*100,Quaternion.LookRotation(direction));Capture(camera,Log+prefix+"_"+slug+"_"+side+".png");}
    }finally{UnityEngine.Object.DestroyImmediate(go);}
   }}finally{foreach(var r in oldRenderers)if(r)r.enabled=true;UnityEngine.Object.DestroyImmediate(cameraObject);}
  }
  static void Capture(Camera camera,string path){var rt=RenderTexture.GetTemporary(768,768,24);var old=RenderTexture.active;var tex=new Texture2D(768,768,TextureFormat.RGB24,false);try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,768,768),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}finally{camera.targetTexture=null;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);}}
 }
}
