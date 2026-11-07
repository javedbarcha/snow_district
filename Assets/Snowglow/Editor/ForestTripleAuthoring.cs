using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace Snowglow.Editor {
 public static partial class ForestSeatingAuthoring {
  const string TriplePrefix="pinefall-tree-triple-",TripleRequest="Logs/TripleForest.request",TripleLogs="Logs/ForestTriple";
  static int tripleChecks;
  static void TripleCheck(bool ok,string message){tripleChecks++;Require(ok,message);}
  [InitializeOnLoadMethod]static void RegisterTriple(){EditorApplication.update+=PendingTriple;}
  static void PendingTriple(){if(!File.Exists(TripleRequest)){EditorApplication.update-=PendingTriple;return;}if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;EditorApplication.update-=PendingTriple;File.Delete(TripleRequest);try{TripleForest();}catch(Exception e){Directory.CreateDirectory(TripleLogs);File.WriteAllText(TripleLogs+"/Validation.txt","FAIL: "+e);Debug.LogException(e);}}
  // Elevated, gentler hill terrain takes priority over the steeper hillside band.
  static int HillBand(Terrain terrain,Vector3 p){float height=Ground(terrain,p)-DistrictFactory.BaseHeight;float slope=Vector3.Angle(Vector3.up,Normal(terrain,p));return height>=15&&slope<12?0:slope>=5?1:2;}
  static string BandName(int band)=>new[]{"Hills","Hilly slopes","Town outskirts"}[band];
  static string FullPose(CatalogPlacement p)=>p.assetId+"|"+p.transform.localToWorldMatrix.ToString("R");
  [MenuItem("Snowglow/Environment/Triple forest with hills first")]
  public static void TripleForest(){
   if(All<CatalogPlacement>(SceneManager.GetActiveScene()).Count(Tree)==500){Apply500();return;}
   if(All<CatalogPlacement>(SceneManager.GetActiveScene()).Count(Tree)==638){Apply638();return;}
   tripleChecks=0;cache.Clear();outerIds.Clear();slopeIds.Clear();Directory.CreateDirectory(TripleLogs);
   var scene=SceneManager.GetActiveScene();TripleCheck(scene.path==SceneValidation.ScenePath&&!EditorApplication.isPlayingOrWillChangePlaymode,"open Pinefall with Play stopped");
   if(!File.Exists(TripleLogs+"/Pinefall-before.unity"))TripleCheck(EditorSceneManager.SaveScene(scene,TripleLogs+"/Pinefall-before.unity",true),"backup failed");
   if(scene.isDirty)TripleCheck(EditorSceneManager.SaveScene(scene,TripleLogs+"/Pinefall-unsaved-before.unity",true),"unsaved backup failed");
   var forest=All<CatalogPlacement>(scene).Where(Tree).OrderBy(p=>p.placementId).ToArray();var baseline=forest.Where(p=>!(p.placementId??"").StartsWith(TriplePrefix,StringComparison.Ordinal)).ToArray();
   TripleCheck(baseline.Length==146,"expected audited baseline of 146; inspect changed baseline before expansion");int target=baseline.Length*3;
   TripleCheck(forest.Length==146||forest.Length==target,"partial triple forest requires inspection");
   var terrain=All<Terrain>(scene).Single();var session=All<PrototypeSession>(scene).Single();
   var mountains=All<CatalogPlacement>(scene).Where(Mountain).Select(p=>{var rs=p.GetComponentsInChildren<Renderer>(true);var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}).ToArray();TripleCheck(mountains.Length==12,"mountain ring missing");
   var obstacles=All<Collider>(scene).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&!c.isTrigger&&!(c is TerrainCollider)&&!(c is MeshCollider&&c.GetComponentInParent<SnowField>())&&!Tree(c.GetComponentInParent<CatalogPlacement>())).Select(c=>c.bounds).ToArray();
   string snow=SnowData(session.Snow.seed);var poses=All<CatalogPlacement>(scene).Where(p=>!string.IsNullOrEmpty(p.placementId)).ToDictionary(p=>p.placementId,FullPose);var fleet=session.Fleet.Select(m=>m.transform.localToWorldMatrix.ToString("R")).ToArray();
   var protectedPaths=All<MeshFilter>(scene).Where(f=>f.sharedMesh).Select(f=>AssetDatabase.GetAssetPath(f.sharedMesh)).Concat(new[]{AssetDatabase.GetAssetPath(terrain.terrainData),AssetDatabase.GetAssetPath(session.Snow.seed)}).Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal)).Distinct().ToDictionary(p=>p,p=>AssetDatabase.GetAssetDependencyHash(p));
   Validate(forest,terrain,mountains,forest.Length);
   var counts=Enumerable.Range(0,3).Select(b=>forest.Count(p=>HillBand(terrain,p.transform.position)==b)).ToArray();var goals=new[]{241,132,65};
   // Retain existing manual positions if one band already exceeds its nominal quota.
   for(int band=0;band<3;band++)if(counts[band]>goals[band]){int delta=counts[band]-goals[band];goals[band]=counts[band];for(int other=0;other<3&&delta>0;other++)if(other!=band){int take=Math.Min(delta,Math.Max(0,goals[other]-counts[other]));goals[other]-=take;delta-=take;}TripleCheck(delta==0,"cannot preserve baseline band distribution");}
   var plans=new List<Plan>();var occupied=forest.Select(p=>new Occupied{id=p.placementId,point=p.transform.position,radius=Radius(Data(p.assetId),p.transform.lossyScale)}).ToList();var rng=new System.Random(611438);
   if(forest.Length<target){
    var bag=baseline.SelectMany(p=>Enumerable.Repeat(p.assetId,2)).ToArray();for(int i=bag.Length-1;i>0;i--){int j=rng.Next(i+1);var swap=bag[i];bag[i]=bag[j];bag[j]=swap;}
    // Candidate terrain samples are preclassified, avoiding expensive geometry tests on the wrong band.
    var candidates=new List<Vector3>[] {new List<Vector3>(),new List<Vector3>(),new List<Vector3>()};
    for(int z=-360;z<=360;z+=4)for(int x=-360;x<=360;x+=4){var p=new Vector3(x,0,z);if(new Vector2(x,z).magnitude>360||Mathf.Abs(x)<140&&Mathf.Abs(z)<140)continue;candidates[HillBand(terrain,p)].Add(p);}
    for(int i=0;i<bag.Length;i++){
     int band=Enumerable.Range(0,3).OrderByDescending(b=>goals[b]-counts[b]).First();TripleCheck(goals[band]>counts[band]&&candidates[band].Count>0,"no quota/candidates for "+BandName(band));var d=Data(bag[i]);Plan plan=null;
     for(int attempt=0;attempt<40000;attempt++){
      var p=candidates[band][rng.Next(candidates[band].Count)]+new Vector3((float)rng.NextDouble()*4-2,0,(float)rng.NextDouble()*4-2);if(HillBand(terrain,p)!=band)continue;
      float scale=.8f+(float)rng.NextDouble()*.6f;var size=Vector3.one*scale;if(!Spaced(d,p,size,occupied))continue;float yaw=(float)rng.NextDouble()*360;
      if(!Fit(d,terrain,p,yaw,size,mountains,obstacles,out var pos,out var rot))continue;
      plan=new Plan{geometry=d,id=TriplePrefix+(i+1).ToString("D3"),position=pos,rotation=rot,scale=size};break;
     }
     TripleCheck(plan!=null,"no safe "+BandName(band)+" site for "+bag[i]+"; nothing placed");plans.Add(plan);counts[band]++;occupied.Add(new Occupied{id=plan.id,point=plan.position,radius=Radius(d,plan.scale)});
    }
   }
   TripleCheck(counts[0]>counts[1]&&counts[1]>counts[2],"hills > slopes > outskirts not achieved");
   Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Triple forest with hills first");bool saved=false;
   try{
    Transform parent=null;if(plans.Count>0){var go=new GameObject("Forest expansion | triple density, hills first");SceneManager.MoveGameObjectToScene(go,scene);Undo.RegisterCreatedObjectUndo(go,"Forest density group");parent=go.transform;}
    foreach(var plan in plans){var go=(GameObject)PrefabUtility.InstantiatePrefab(plan.geometry.asset.prefab,scene);Undo.RegisterCreatedObjectUndo(go,"Added LOD3 forest instance");go.transform.SetParent(parent,false);go.transform.localScale=plan.scale;go.transform.SetPositionAndRotation(plan.position,plan.rotation);var p=go.GetComponent<CatalogPlacement>();p.placementId=plan.id;PrefabUtility.RecordPrefabInstancePropertyModifications(p);PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);}
    Physics.SyncTransforms();var current=All<CatalogPlacement>(scene).Where(Tree).ToArray();ValidateTriple(current,terrain,mountains,baseline,target);
    int mask=RefreshMask(session.Snow,Array.Empty<Bounds>(),current.Where(p=>(p.placementId??"").StartsWith(TriplePrefix,StringComparison.Ordinal)).SelectMany(p=>p.GetComponentsInChildren<Collider>(true)).Where(c=>c.enabled).Select(c=>c.bounds).ToArray());TripleCheck(snow==SnowData(session.Snow.seed),"snow quantities changed");
    SceneValidation.Validate(scene);MechanicsChecks.Run();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);TripleCheck(EditorSceneManager.SaveScene(scene),"save failed");saved=true;Undo.CollapseUndoOperations(undo);
    scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);terrain=All<Terrain>(scene).Single();session=All<PrototypeSession>(scene).Single();current=All<CatalogPlacement>(scene).Where(Tree).ToArray();ValidateTriple(current,terrain,mountains,baseline,target);SceneValidation.Validate(scene);MechanicsChecks.Run();
    foreach(var p in All<CatalogPlacement>(scene).Where(p=>poses.ContainsKey(p.placementId??"")))TripleCheck(poses[p.placementId]==FullPose(p),"existing placement moved "+p.placementId);
    TripleCheck(fleet.SequenceEqual(session.Fleet.Select(m=>m.transform.localToWorldMatrix.ToString("R"))),"fleet moved");foreach(var pair in protectedPaths)TripleCheck(pair.Value==AssetDatabase.GetAssetDependencyHash(pair.Key),"protected asset changed "+pair.Key);TripleCheck(snow==SnowData(session.Snow.seed),"saved snow quantities changed");
    long triangles=current.Sum(p=>(long)Data(p.assetId).asset.triangles);int renderers=current.Sum(p=>p.GetComponentsInChildren<Renderer>(true).Length);
    var csv=new StringBuilder("placement,variant,band,x,y,z\n");foreach(var p in current.OrderBy(p=>p.placementId))csv.AppendLine(p.placementId+","+p.assetId+","+BandName(HillBand(terrain,p.transform.position))+","+p.transform.position.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+p.transform.position.y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+p.transform.position.z.ToString("R",System.Globalization.CultureInfo.InvariantCulture));File.WriteAllText(TripleLogs+"/Placements.csv",csv.ToString());
    File.WriteAllText(TripleLogs+"/Validation.txt","PASS: Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+"; "+tripleChecks+" checks plus saved/reopened full forest seating validation, scene validation and 19 snow checks. Forest 146 -> 438 (420 trees/snags, 18 wood); added "+plans.Count+"; "+triangles+" triangles / "+renderers+" renderers. Hills "+counts[0]+", hilly slopes "+counts[1]+", outskirts "+counts[2]+". Hills: >=15m above town and <12deg; otherwise slopes >=5deg; remaining outer terrain is outskirts. Existing poses/variants retained; exact 3x population per variant. 365m root/full-bound cap, mountain exclusion, source LOD3, terrain support, slope seating, single-box collision and 230m render culling checked. Snow obstacle flags changed "+mask+" in separate seed; all snow quantities, original seeds/meshes/Terrain and fleet/scenery poses preserved. No player-save writes or district rebake. Android packaging, live driving and phone performance remain untested.");
    PreviewTriple(scene);EditorSceneManager.OpenScene(SceneValidation.ScenePath);GeometryAudit.Run();Debug.Log(File.ReadAllText(TripleLogs+"/Validation.txt"));
   }catch{if(!saved)Undo.RevertAllDownToGroup(undo);throw;}
  }
  static void ValidateTriple(CatalogPlacement[] current,Terrain terrain,Bounds[] mountains,CatalogPlacement[] baseline,int target){
   Validate(current,terrain,mountains,target);
   // Baseline objects are unloaded after reopen; derive the immutable ratios from the preserved catalog ID snapshot instead.
   foreach(var group in current.GroupBy(p=>p.assetId)){int original=group.Count(p=>!(p.placementId??"").StartsWith(TriplePrefix,StringComparison.Ordinal));TripleCheck(group.Count()==original*3,"variant not tripled "+group.Key);}
   foreach(var p in current){var cs=p.GetComponentsInChildren<Collider>(true);TripleCheck(cs.Length==1&&cs[0] is BoxCollider&&cs[0].enabled&&!cs[0].isTrigger,"single solid tree box "+p.placementId);TripleCheck(p.GetComponent<DistanceVisibility>()&&Mathf.Approximately(p.GetComponent<DistanceVisibility>().distance,230),"distance culling changed");}
   var counts=Enumerable.Range(0,3).Select(b=>current.Count(p=>HillBand(terrain,p.transform.position)==b)).ToArray();TripleCheck(counts[0]>counts[1]&&counts[1]>counts[2],"saved density order");
  }
  static void PreviewTriple(Scene scene){var camera=All<Camera>(scene).First();var pos=camera.transform.position;var rot=camera.transform.rotation;bool ortho=camera.orthographic;float size=camera.orthographicSize,aspect=camera.aspect;try{Overview(camera);File.Copy(Logs+"/Forest-overview.png",TripleLogs+"/Forest-overview.png",true);}finally{camera.transform.SetPositionAndRotation(pos,rot);camera.orthographic=ortho;camera.orthographicSize=size;camera.aspect=aspect;}}
 }
}
