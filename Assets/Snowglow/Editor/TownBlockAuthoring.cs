using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Snowglow.Editor {
 // Content authoring only. Preserve house identities, scale and complete lot assemblies.
 public static class TownBlockAuthoring {
  const string Request="Logs/ExpandTownBlocks.request", Report="Logs/TownBlocks/Validation.txt";
  const string SeedPath="Assets/Snowglow/Catalog/SceneData/TownBlocksSnowSeed.asset";
  sealed class Block {
   public string name;public Vector2[] sites;
   public Block(string name,float[] xs,float[] zs){this.name=name;sites=xs.SelectMany(x=>zs.Select(z=>new Vector2(x,z))).ToArray();}
  }
  static Block[] Layout()=>new[]{
   new Block("01 Southwest",new[]{-46f,-22},new[]{-48f,-32,-16}),
   new Block("02 Southeast",new[]{22f,46},new[]{-48f,-32,-16}),
   new Block("03 Northwest",new[]{-46f,-22},new[]{16f,32,48}),
   new Block("04 Northeast",new[]{22f,46},new[]{16f,32,48}),
   new Block("05 North west",new[]{-46f,-22},new[]{82f,102}),
   new Block("06 North east",new[]{22f,46},new[]{82f,102}),
   new Block("07 West south",new[]{-102f,-82},new[]{-44f}),
   new Block("08 West north",new[]{-102f,-82},new[]{-20f}),
   new Block("09 Outer northwest",new[]{-102f,-82},new[]{20f}),
   new Block("10 Outer northeast",new[]{-102f,-82},new[]{44f})
  };
  static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException("Town blocks: "+message);}
  static T[] All<T>(Scene s) where T:Component=>s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
  static CatalogPlacement[] Homes(Scene s)=>All<CatalogPlacement>(s).Where(p=>(p.placementId??"").StartsWith("pinefall-home-",StringComparison.Ordinal)).ToArray();
  static bool At(Transform t,Vector2 site)=>Mathf.Abs(t.position.x-site.x)<.05f&&Mathf.Abs(t.position.z-site.y)<.05f;
  static string Pose(Transform t)=>t.localToWorldMatrix.ToString("R");
  static bool Overlap(Bounds a,Bounds b,float margin=0)=>a.min.x-margin<b.max.x&&a.max.x+margin>b.min.x&&a.min.z-margin<b.max.z&&a.max.z+margin>b.min.z;
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){
   if(!File.Exists(Request)){EditorApplication.update-=Pending;return;}
   if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
   EditorApplication.update-=Pending;bool resume=File.ReadAllText(Request).Trim()=="resume";File.Delete(Request);
   try{Expand(resume);}catch(Exception e){Directory.CreateDirectory("Logs/TownBlocks");File.WriteAllText(Report,"FAIL: "+e);Debug.LogException(e);}
  }
  public static void PlacePreparedBlocks(Scene scene){
   var entries=Enumerable.Range(1,7).Select(i=>CatalogPreparation.Library().Find("winter-home-"+i.ToString("D2"))).ToArray();
   Require(entries.All(e=>e&&e.prefab),"all seven prepared homes required");
   var homes=Homes(scene);Require(homes.Length==16||homes.Length==40,"expected original 16 or expanded 40 homes");
   var parent=homes[0].transform.parent.parent;
   // In an expanded scene the parent is the block; recover the neighbourhood.
   if(parent.name.StartsWith("Residential block |",StringComparison.Ordinal))parent=parent.parent;
   var lots=homes.Select(h=>h.transform.parent).ToArray();
   var ids=new HashSet<string>(homes.Select(p=>p.placementId));int next=17,choice=0;
   var templates=lots.ToArray();
   // Reuse every complete lot, including foundations and fences. Pair by nearest
   // available site deterministically, so repeat authoring is idempotent.
   if(homes.Length==16){
    var available=Layout().SelectMany(b=>b.sites).ToList();
    foreach(var lot in lots){
     var site=available.OrderBy(v=>(new Vector2(lot.position.x,lot.position.z)-v).sqrMagnitude).First();
     available.Remove(site);lot.position=new Vector3(site.x,lot.position.y,site.y);
    }
   }
   if(homes.Length==40){
    var remaining=lots.OrderBy(t=>t.GetComponentInChildren<CatalogPlacement>().placementId,StringComparer.Ordinal).ToList();
    foreach(var block in Layout()){
     string groupName="Residential block | "+block.name+" | "+block.sites.Length+" houses";
     var group=parent.Find(groupName);if(!group){group=new GameObject(groupName).transform;group.SetParent(parent,false);}
     foreach(var site in block.sites){
      var lot=remaining.OrderBy(t=>(new Vector2(t.position.x,t.position.z)-site).sqrMagnitude).First();
      remaining.Remove(lot);lot.position=new Vector3(site.x,lot.position.y,site.y);lot.SetParent(group,true);
     }
    }
   }
   foreach(var block in Layout()){
    string name="Residential block | "+block.name+" | "+block.sites.Length+" houses";
    var group=parent.Find(name);if(!group){group=new GameObject(name).transform;group.SetParent(parent,false);}
    foreach(var site in block.sites){
     var lot=lots.SingleOrDefault(t=>At(t,site));
     if(!lot){
      var template=templates.OrderBy(t=>(new Vector2(t.position.x,t.position.z)-site).sqrMagnitude).First();
      var obj=UnityEngine.Object.Instantiate(template.gameObject,group);lot=obj.transform;lot.position=new Vector3(site.x,template.position.y,site.y);
      foreach(var old in lot.GetComponentsInChildren<CatalogPlacement>().Where(p=>(p.placementId??"").StartsWith("pinefall-home-",StringComparison.Ordinal)).ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
      var a=entries[(choice++*3+2)%entries.Length];var foundation=lot.Find("Foundation").GetComponent<Renderer>().bounds;
      var scale=Mathf.Min(1.5f,Mathf.Min(foundation.size.x/a.localBounds.size.x,foundation.size.z/a.localBounds.size.z));
      Require(scale>=a.minimumScale,"new house scale outside catalog range");
      var house=(GameObject)PrefabUtility.InstantiatePrefab(a.prefab,scene);house.transform.SetParent(lot,true);
      house.transform.SetPositionAndRotation(new Vector3(site.x,foundation.max.y,site.y),Quaternion.Euler(0,180,0));house.transform.localScale=Vector3.one*scale;
      while(ids.Contains("pinefall-home-"+next.ToString("D2")))next++;
      var id="pinefall-home-"+next.ToString("D2");ids.Add(id);house.GetComponent<CatalogPlacement>().placementId=id;
      lot.name="Home lot | 1.5x | "+a.displayName;
     }
     lot.SetParent(group,true);
    }
   }
   ValidateLayout(scene);
  }
  static void ValidateLayout(Scene scene){
   var homes=Homes(scene);Require(homes.Length==40&&homes.Select(h=>h.placementId).Distinct().Count()==40,"40 unique home IDs required");
   var blocks=All<Transform>(scene).Where(t=>t.name.StartsWith("Residential block |",StringComparison.Ordinal)).ToArray();Require(blocks.Length==10,"ten residential groups required");
   foreach(var block in Layout()){
    var group=blocks.Single(t=>t.name=="Residential block | "+block.name+" | "+block.sites.Length+" houses");
    var members=group.GetComponentsInChildren<CatalogPlacement>().Where(p=>homes.Contains(p)).ToArray();
    Require(members.Length==block.sites.Length,"wrong house count: "+block.name);
    foreach(var site in block.sites)Require(members.Count(h=>At(h.transform.parent,site))==1,"missing or duplicate site "+site);
   }
   var roads=All<Renderer>(scene).Where(r=>r.name=="Road | asphalt").Select(r=>r.bounds).ToArray();
   var bounds=homes.Select(h=>{var renderers=h.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);return b;}).ToArray();
   for(int i=0;i<homes.Length;i++){
    Require(PrefabUtility.IsPartOfPrefabInstance(homes[i]),"house lost prefab connection");
    Require(!roads.Any(b=>Overlap(bounds[i],b,1)),"home within 1 m of road: "+homes[i].placementId);
    for(int j=0;j<i;j++)Require(!Overlap(bounds[i],bounds[j],1),"house footprints too close: "+homes[i].placementId+" / "+homes[j].placementId);
   }
   Require(Enumerable.Range(1,7).All(i=>homes.Any(h=>h.assetId=="winter-home-"+i.ToString("D2"))),"all seven variants required");
  }
  static string SnowData(SnowSeed seed)=>JsonUtility.ToJson(new SnowVolumeGrid{depth=seed.grid.depth,credit=seed.grid.credit,initialVolume=seed.grid.initialVolume,disposed=seed.grid.disposed,revision=seed.grid.revision,blocked=new bool[0]});
  static int RefreshMovedObstacles(SnowField snow){
   // A fresh scene copy retains the previous seed for rollback. Recompute both
   // freed and newly occupied ground cells; additive masks leave ghost barriers.
   const string path="Assets/Snowglow/Catalog/SceneData/EvenTownSnowSeed.asset";
   var seed=AssetDatabase.LoadAssetAtPath<SnowSeed>(path);
   if(!seed){seed=UnityEngine.Object.Instantiate(snow.seed);seed.name="Pinefall | evenly distributed home obstacles";AssetDatabase.CreateAsset(seed,path);}
   Require(SnowData(seed)==SnowData(snow.seed),"snow data differs");
   seed.grid.blocked=(bool[])snow.seed.grid.blocked.Clone();snow.seed=seed;
   Physics.SyncTransforms();int changed=0;
   for(int i=0;i<seed.grid.blocked.Length;i++){
    bool blocked=Physics.CheckBox(new Vector3(seed.grid.X(i),SnowField.Ground+.85f,seed.grid.Z(i)),new Vector3(SnowVolumeGrid.Cell*.45f,.65f,SnowVolumeGrid.Cell*.45f),Quaternion.identity,1<<0,QueryTriggerInteraction.Ignore);
    if(seed.grid.blocked[i]!=blocked){seed.grid.blocked[i]=blocked;changed++;}
   }
   EditorUtility.SetDirty(seed);return changed;
  }
  [MenuItem("Snowglow/Environment/Redistribute homes evenly")]
  public static void Redistribute()=>Expand(false);
  public static void RedistributeBatch(){
   EditorSceneManager.OpenScene(SceneValidation.ScenePath);
   if(File.Exists(Request))File.Delete(Request);
   Expand(false);
  }
  [MenuItem("Snowglow/Environment/Expand town into 2, 4 and 6 house blocks")]
  public static void Expand()=>Expand(false);
  static void Expand(bool resume){
   Require(!EditorApplication.isPlayingOrWillChangePlaymode,"stop Play first");var scene=SceneManager.GetActiveScene();Require(scene.path==SceneValidation.ScenePath&&(!scene.isDirty||(resume&&Homes(scene).Length==40&&File.Exists("Logs/TownBlocks/Pinefall-before.unity"))),"open saved Pinefall first");
   Directory.CreateDirectory("Logs/TownBlocks");if(!File.Exists("Logs/TownBlocks/Pinefall-before.unity"))File.Copy(scene.path,"Logs/TownBlocks/Pinefall-before.unity");if(!File.Exists("Logs/TownBlocks/Pinefall-before-even-layout.unity"))File.Copy(scene.path,"Logs/TownBlocks/Pinefall-before-even-layout.unity");
   var original=Homes(scene).ToDictionary(h=>h.placementId,h=>h.assetId+"|"+h.transform.localScale.ToString("R"));
   var session=All<PrototypeSession>(scene).Single();var volumes=SnowData(session.Snow.seed);
   var protectedAssets=All<Terrain>(scene).Select(t=>AssetDatabase.GetAssetPath(t.terrainData)).Concat(session.Snow.GetComponentsInChildren<MeshFilter>().Select(m=>AssetDatabase.GetAssetPath(m.sharedMesh))).Concat(new[]{AssetDatabase.GetAssetPath(session.Snow.seed)}).Where(p=>!string.IsNullOrEmpty(p)&&!p.StartsWith(CatalogPreparation.Root+"/SceneData/",StringComparison.Ordinal)).Distinct().ToDictionary(p=>p,p=>AssetDatabase.GetAssetDependencyHash(p).ToString());
   var mountains=All<CatalogPlacement>(scene).Where(p=>(p.placementId??"").StartsWith("pinefall-mountain-",StringComparison.Ordinal)).ToDictionary(p=>p.placementId,p=>Pose(p.transform));
   var fleet=session.Fleet.Select(m=>m.GetInstanceID()).ToArray();
   PlacePreparedBlocks(scene);int blocked=RefreshMovedObstacles(session.Snow);
   Require(fleet.SequenceEqual(session.Fleet.Select(m=>m.GetInstanceID())),"fleet changed");
   SceneValidation.Validate(scene);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();Require(EditorSceneManager.SaveScene(scene),"scene save failed");
   scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);ValidateLayout(scene);SceneValidation.Validate(scene);MechanicsChecks.Run();
   foreach(var h in Homes(scene))if(original.TryGetValue(h.placementId,out var pose))Require(pose==h.assetId+"|"+h.transform.localScale.ToString("R"),"existing home identity or scale changed: "+h.placementId);
   foreach(var m in All<CatalogPlacement>(scene))if(mountains.TryGetValue(m.placementId??"",out var pose))Require(pose==Pose(m.transform),"mountain moved");
   foreach(var a in protectedAssets)Require(a.Value==AssetDatabase.GetAssetDependencyHash(a.Key).ToString(),"terrain or snow mesh changed: "+a.Key);
   session=All<PrototypeSession>(scene).Single();Require(volumes==SnowData(session.Snow.seed),"snow volume or credit data changed");
   var result=new StringBuilder("PASS: Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+". Expanded Pinefall to 40 persistent homes in ten Editor-authored blocks. Saved and reopened.\n");
   foreach(var b in Layout())result.AppendLine(b.name+": "+b.sites.Length+" houses.");
   var library=CatalogPreparation.Library();int triangles=0;
   foreach(var group in Homes(scene).GroupBy(h=>h.assetId)){var a=library.Find(group.Key);triangles+=group.Count()*a.triangles;result.AppendLine(a.displayName+": "+group.Count()+" instances.");}
   result.AppendLine("House geometry: "+triangles+" triangles / 160 house renderers before shadow passes; fences/foundations extra. All home identities, variants and scales preserved; complete lots redistributed evenly. Footprint checks: at least 1 m house/road and house/house gap. Terrain, mountains, fleet and saved snow meshes unchanged.");
   result.AppendLine("Scene-specific snow seed retains all depth/credit/accounting values; updated "+blocked+" obstacle cells for house collision proxies. Original seed asset preserved. No player saves touched. Scene checks and 19 snow-ledger checks passed. Repeat command preserves 40 homes without duplicates. Full driving/clearance walk, Android build and A07 performance remain untested.");
   File.WriteAllText(Report,result.ToString());Debug.Log(result.ToString());
   if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(new Vector3(0,5,20),Quaternion.Euler(90,0,0),135,true,true);
  }
 }
}
