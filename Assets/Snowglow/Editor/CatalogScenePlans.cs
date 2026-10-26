using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Snowglow.Editor {
 [Serializable]public sealed class CatalogScenePlan {public int version=1;public string name="Catalog layout";public bool terrain=true;public PlanPlacement[] placements=new PlanPlacement[0];}
 [Serializable]public sealed class PlanPlacement {public string id,assetId;public Vector3 position,rotation;public float scale=1;public string connectTo,ownAnchor,targetAnchor;}
 [Serializable]public sealed class CatalogManifest {public int version=1;public string coordinates="metres; Y up; Z forward; ground-centred roots";public ManifestAsset[] assets;}
 [Serializable]public sealed class ManifestAsset {public string id,name,kind;public string[] tags;public Vector3 dimensions;public float minimumScale,maximumScale;public bool blocksSnow;public ManifestAnchor[] anchors;public ManifestSlot[] sockets;}
 [Serializable]public sealed class ManifestAnchor {public string id,role,interfaceId;public Vector3 position,rotation;}
 [Serializable]public sealed class ManifestSlot {public string id,accepts,interfaceId;public float maximumMass,maximumWidth;public bool hydraulics;}
 public sealed class ResolvedPlacement {public PlanPlacement plan;public CatalogAsset asset;public Vector3 position;public Quaternion rotation;public Bounds bounds;}
 public static class CatalogScenePlans {
  public static string Manifest(AssetCatalog catalog){
   if(!catalog)throw new ArgumentException("Select a catalog.");ValidateIds(catalog);
   var entries=catalog.entries.Where(a=>a&&a.status==PreparationStatus.Ready).ToArray();
   if(entries.Length==0)throw new InvalidOperationException("Publish at least one reviewed catalog entry before exporting a Ready manifest.");
   foreach(var a in entries){var report=CatalogValidation.Check(a,true);if(!report.Ok)throw new InvalidOperationException(a.displayName+"\n"+report);}
   var manifest=new CatalogManifest{assets=entries.Select(a=>new ManifestAsset{
    id=a.id,name=a.displayName,kind=a.kind.ToString(),tags=a.tags,dimensions=a.localBounds.size,minimumScale=a.minimumScale,maximumScale=a.maximumScale,blocksSnow=a.blocksSnow,
    anchors=a.prefab.GetComponentsInChildren<AssetAnchor>(true).Select(t=>new ManifestAnchor{id=t.id,role=t.role.ToString(),interfaceId=t.interfaceId,position=a.prefab.transform.InverseTransformPoint(t.transform.position),rotation=(Quaternion.Inverse(a.prefab.transform.rotation)*t.transform.rotation).eulerAngles}).ToArray(),
    sockets=a.prefab.GetComponentsInChildren<AssetSocket>(true).Select(s=>new ManifestSlot{id=s.id,accepts=s.accepts.ToString(),interfaceId=s.interfaceId,maximumMass=s.maximumMass,maximumWidth=s.maximumWidth,hydraulics=s.hydraulics}).ToArray()
   }).ToArray()};return JsonUtility.ToJson(manifest,true);
  }
  public static void ValidateIds(AssetCatalog c){if(c.entries.Any(a=>!a))throw new InvalidOperationException("Catalog contains missing entries.");if(c.entries.GroupBy(a=>a.id).Any(g=>g.Count()>1))throw new InvalidOperationException("Duplicate catalog IDs.");}
  static AssetAnchor FindAnchor(CatalogAsset a,string id){var anchor=a.prefab.GetComponentsInChildren<AssetAnchor>(true).FirstOrDefault(t=>t.id==id);if(!anchor)throw new InvalidOperationException("Unknown anchor "+id+" on "+a.id);return anchor;}
  public static List<ResolvedPlacement> Resolve(AssetCatalog catalog,string json){
   if(!catalog)throw new ArgumentException("Select a catalog.");ValidateIds(catalog);
   if(string.IsNullOrWhiteSpace(json)||json.Length>2000000)throw new InvalidOperationException("Scene plan is empty or larger than 2 MB.");
   CatalogScenePlan plan;try{plan=JsonUtility.FromJson<CatalogScenePlan>(json);}catch(Exception e){throw new InvalidOperationException("Invalid scene-plan JSON.",e);}
   if(plan==null||plan.version!=1||plan.placements==null||plan.placements.Length==0||plan.placements.Length>1000)throw new InvalidOperationException("Use version 1 and between 1 and 1000 placements.");
   var pending=new Dictionary<string,PlanPlacement>();
   foreach(var p in plan.placements){if(p==null||string.IsNullOrWhiteSpace(p.id)||pending.ContainsKey(p.id))throw new InvalidOperationException("Missing or duplicate placement ID.");if(!CatalogValidation.Finite(p.position)||!CatalogValidation.Finite(p.rotation)||!CatalogValidation.Finite(p.scale)||p.scale<=0)throw new InvalidOperationException("Invalid placement transform.");pending.Add(p.id,p);}
   var done=new Dictionary<string,ResolvedPlacement>();
   while(pending.Count>0){bool progress=false;
    foreach(var pair in pending.ToArray()){
     var p=pair.Value;if(!string.IsNullOrEmpty(p.connectTo)&&!done.ContainsKey(p.connectTo))continue;
     var a=catalog.Find(p.assetId);var report=CatalogValidation.Check(a,true);if(!report.Ok)throw new InvalidOperationException(p.id+": "+report);
     if(p.scale<a.minimumScale||p.scale>a.maximumScale)throw new InvalidOperationException("Scale outside approved range for "+p.id);
     if(a.kind==CatalogKind.Vehicle&&!a.prefab.GetComponent<MachineController>())throw new InvalidOperationException("Use an assembled vehicle, not an unfinished body: "+p.id);
     var item=new ResolvedPlacement{asset=a,plan=p,position=p.position,rotation=Quaternion.Euler(p.rotation)};
     if(!string.IsNullOrEmpty(p.connectTo)){
      var target=done[p.connectTo];var own=FindAnchor(a,p.ownAnchor);var other=FindAnchor(target.asset,p.targetAnchor);
      if(own.role!=AnchorRole.RoadConnection||other.role!=AnchorRole.RoadConnection||string.IsNullOrEmpty(own.interfaceId)||own.interfaceId!=other.interfaceId)throw new InvalidOperationException("Road connection interfaces differ.");
      Quaternion ownRot=Quaternion.Inverse(a.prefab.transform.rotation)*own.transform.rotation,otherRot=Quaternion.Inverse(target.asset.prefab.transform.rotation)*other.transform.rotation;
      item.rotation=target.rotation*otherRot*Quaternion.Euler(0,180,0)*Quaternion.Inverse(ownRot);
      item.position=target.position+target.rotation*(target.asset.prefab.transform.InverseTransformPoint(other.transform.position)*target.plan.scale)-item.rotation*(a.prefab.transform.InverseTransformPoint(own.transform.position)*p.scale);
     }
     var b=a.localBounds;bool first=true;
     for(int n=0;n<8;n++){var v=item.position+item.rotation*((b.center+Vector3.Scale(b.extents,new Vector3((n&1)==0?-1:1,(n&2)==0?-1:1,(n&4)==0?-1:1)))*p.scale);if(first){item.bounds=new Bounds(v,Vector3.zero);first=false;}else item.bounds.Encapsulate(v);}
     if(!CatalogValidation.Finite(item.bounds.min)||!CatalogValidation.Finite(item.bounds.max)||item.bounds.min.x< -500||item.bounds.max.x>500||item.bounds.min.z< -500||item.bounds.max.z>500||Mathf.Abs(item.position.y)>200)throw new InvalidOperationException("Placement lies outside the 1 km district or has invalid bounds: "+p.id);
     foreach(var previous in done.Values){if(!Solid(a)||!Solid(previous.asset))continue;var overlap=Vector3.Min(item.bounds.max,previous.bounds.max)-Vector3.Max(item.bounds.min,previous.bounds.min);if(overlap.x>.05f&&overlap.y>.05f&&overlap.z>.05f)throw new InvalidOperationException("Occupied footprints overlap: "+p.id+" and "+previous.plan.id);}
     done.Add(p.id,item);pending.Remove(pair.Key);progress=true;
    }
    if(!progress)throw new InvalidOperationException("Missing connection target or cyclic connection graph.");
   }return done.Values.ToList();
  }
  static bool Solid(CatalogAsset a)=>a.blocksSnow&&a.kind!=CatalogKind.RoadAssembly&&a.kind!=CatalogKind.GameplayAssembly;
  public static UnityEngine.SceneManagement.Scene Build(AssetCatalog catalog,string json,string output){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Scene assembly is Editor-only.");
   // Resolve everything before creating or changing a scene.
   var resolved=Resolve(catalog,json);var plan=JsonUtility.FromJson<CatalogScenePlan>(json);
   if(!output.StartsWith("Assets/Scenes/",StringComparison.Ordinal)||output.Contains("..")||!output.EndsWith(".unity",StringComparison.Ordinal)||output==SceneValidation.ScenePath)throw new InvalidOperationException("Choose a new .unity file under Assets/Scenes; Pinefall is protected.");
   if(File.Exists(output))throw new InvalidOperationException("Output scene exists; choose a new name.");
   if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())throw new OperationCanceledException();
   var scene=EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects,NewSceneMode.Single);
   var root=new GameObject(string.IsNullOrEmpty(plan.name)?"Catalog layout":plan.name);
   foreach(var item in resolved){var go=(GameObject)PrefabUtility.InstantiatePrefab(item.asset.prefab,scene);go.name=item.plan.id+" | "+item.asset.displayName;go.transform.SetParent(root.transform,false);go.transform.SetPositionAndRotation(item.position,item.rotation);go.transform.localScale=Vector3.one*item.plan.scale;var tag=go.GetComponent<CatalogPlacement>()??go.AddComponent<CatalogPlacement>();tag.assetId=item.asset.id;tag.placementId=item.plan.id;}
   if(plan.terrain){var terrainData=new TerrainData{heightmapResolution=33,size=new Vector3(1000,20,1000)};string terrainPath=AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(output,null)+"-terrain.asset");AssetDatabase.CreateAsset(terrainData,terrainPath);var terrain=Terrain.CreateTerrainGameObject(terrainData);terrain.name="Authored 1 km terrain";terrain.transform.position=new Vector3(-500,0,-500);}
   if(!EditorSceneManager.SaveScene(scene,output))throw new IOException("Scene could not be saved.");AssetDatabase.SaveAssets();return scene;
  }
 }
}
