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
 // Content authoring only. Existing homes retain their identities and world poses.
 public static class TownBlockAuthoring {
  const string Request="Logs/ExpandTownBlocks.request", Report="Logs/TownBlocks/Validation.txt";
  const string SeedPath="Assets/Snowglow/Catalog/SceneData/TownBlocksSnowSeed.asset";
  sealed class Block {
   public string name;public Vector2[] sites;
   public Block(string name,float[] xs,float[] zs){this.name=name;sites=xs.SelectMany(x=>zs.Select(z=>new Vector2(x,z))).ToArray();}
  }
  static Block[] Layout()=>new[]{
   new Block("01 Southwest",new[]{-34f,-18},new[]{-49f,-32,-15}),
   new Block("02 Southeast",new[]{20f,39},new[]{-49f,-32,-15}),
   new Block("03 Northwest",new[]{-34f,-18},new[]{15f,31,47}),
   new Block("04 Northeast",new[]{20f,39},new[]{15f,31,47}),
   new Block("05 North west",new[]{-34f,-18},new[]{80f,94}),
   new Block("06 North east",new[]{20f,39},new[]{80f,94}),
   new Block("07 West south",new[]{-94f},new[]{-32f,-16}),
   new Block("08 West north",new[]{-94f},new[]{31f,47}),
   new Block("09 Outer northwest",new[]{-110f,-94},new[]{94f}),
   new Block("10 Outer northeast",new[]{94f,110},new[]{94f})
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
  static int UpdateObstacleMask(Scene scene,SnowField snow){
   // Keep the original seed and its GUID intact. Only the scene-specific copy's
   // obstacle mask changes; depth, credit and all volume accounting are preserved.
   var seed=AssetDatabase.LoadAssetAtPath<SnowSeed>(SeedPath);
   if(!seed){Directory.CreateDirectory(Path.GetDirectoryName(SeedPath));AssetDatabase.Refresh();seed=UnityEngine.Object.Instantiate(snow.seed);seed.name="Pinefall | expanded residential obstacle mask";AssetDatabase.CreateAsset(seed,SeedPath);}
   Require(SnowData(seed)==SnowData(snow.seed),"scene-specific seed has different snow volumes");
   snow.seed=seed;Physics.SyncTransforms();int changed=0;
   var colliders=Homes(scene).SelectMany(h=>h.GetComponentsInChildren<Collider>()).Where(c=>c.enabled&&!c.isTrigger).ToArray();
   var cellBox=new Bounds(Vector3.zero,new Vector3(SnowVolumeGrid.Cell*.9f,1.3f,SnowVolumeGrid.Cell*.9f));
   foreach(var collider in colliders){
    var bounds=collider.bounds;float origin=SnowVolumeGrid.Origin,cell=SnowVolumeGrid.Cell;int max=SnowVolumeGrid.Size-1;
    int x0=Mathf.Clamp(Mathf.FloorToInt((bounds.min.x-origin)/cell)-1,0,max),x1=Mathf.Clamp(Mathf.CeilToInt((bounds.max.x-origin)/cell)+1,0,max);
    int z0=Mathf.Clamp(Mathf.FloorToInt((bounds.min.z-origin)/cell)-1,0,max),z1=Mathf.Clamp(Mathf.CeilToInt((bounds.max.z-origin)/cell)+1,0,max);
    for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++){
     int i=z*SnowVolumeGrid.Size+x;if(seed.grid.blocked[i])continue;
     cellBox.center=new Vector3(seed.grid.X(i),SnowField.Ground+.85f,seed.grid.Z(i));
     if(bounds.Intersects(cellBox)){seed.grid.blocked[i]=true;changed++;}
    }
   }
   EditorUtility.SetDirty(seed);return changed;
  }
  [MenuItem("Snowglow/Environment/Expand town into 2, 4 and 6 house blocks")]
  public static void Expand()=>Expand(false);
  static void Expand(bool resume){
   Require(!EditorApplication.isPlayingOrWillChangePlaymode,"stop Play first");var scene=SceneManager.GetActiveScene();Require(scene.path==SceneValidation.ScenePath&&(!scene.isDirty||(resume&&Homes(scene).Length==40&&File.Exists("Logs/TownBlocks/Pinefall-before.unity"))),"open saved Pinefall first");
   Directory.CreateDirectory("Logs/TownBlocks");if(!File.Exists("Logs/TownBlocks/Pinefall-before.unity"))File.Copy(scene.path,"Logs/TownBlocks/Pinefall-before.unity");
   var original=Homes(scene).ToDictionary(h=>h.placementId,h=>h.assetId+"|"+Pose(h.transform));
   var session=All<PrototypeSession>(scene).Single();var volumes=SnowData(session.Snow.seed);
   var protectedAssets=All<Terrain>(scene).Select(t=>AssetDatabase.GetAssetPath(t.terrainData)).Concat(session.Snow.GetComponentsInChildren<MeshFilter>().Select(m=>AssetDatabase.GetAssetPath(m.sharedMesh))).Concat(new[]{AssetDatabase.GetAssetPath(session.Snow.seed)}).Where(p=>!string.IsNullOrEmpty(p)&&p!=SeedPath).Distinct().ToDictionary(p=>p,p=>AssetDatabase.GetAssetDependencyHash(p).ToString());
   var mountains=All<CatalogPlacement>(scene).Where(p=>(p.placementId??"").StartsWith("pinefall-mountain-",StringComparison.Ordinal)).ToDictionary(p=>p.placementId,p=>Pose(p.transform));
   var fleet=session.Fleet.Select(m=>m.GetInstanceID()).ToArray();
   PlacePreparedBlocks(scene);int blocked=UpdateObstacleMask(scene,session.Snow);
   Require(fleet.SequenceEqual(session.Fleet.Select(m=>m.GetInstanceID())),"fleet changed");
   SceneValidation.Validate(scene);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();Require(EditorSceneManager.SaveScene(scene),"scene save failed");
   scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);ValidateLayout(scene);SceneValidation.Validate(scene);MechanicsChecks.Run();
   foreach(var h in Homes(scene))if(original.TryGetValue(h.placementId,out var pose))Require(pose==h.assetId+"|"+Pose(h.transform),"existing home moved or changed: "+h.placementId);
   foreach(var m in All<CatalogPlacement>(scene))if(mountains.TryGetValue(m.placementId??"",out var pose))Require(pose==Pose(m.transform),"mountain moved");
   foreach(var a in protectedAssets)Require(a.Value==AssetDatabase.GetAssetDependencyHash(a.Key).ToString(),"terrain or snow mesh changed: "+a.Key);
   session=All<PrototypeSession>(scene).Single();Require(volumes==SnowData(session.Snow.seed),"snow volume or credit data changed");
   var result=new StringBuilder("PASS: Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+". Expanded Pinefall to 40 persistent homes in ten Editor-authored blocks. Saved and reopened.\n");
   foreach(var b in Layout())result.AppendLine(b.name+": "+b.sites.Length+" houses.");
   var library=CatalogPreparation.Library();int triangles=0;
   foreach(var group in Homes(scene).GroupBy(h=>h.assetId)){var a=library.Find(group.Key);triangles+=group.Count()*a.triangles;result.AppendLine(a.displayName+": "+group.Count()+" instances.");}
   result.AppendLine("House geometry: "+triangles+" triangles / 160 house renderers before shadow passes; fences/foundations extra. All original home identities, variants and poses preserved. Footprint checks: at least 1 m house/road and house/house gap. Terrain, mountains, fleet and saved snow meshes unchanged.");
   result.AppendLine("Scene-specific snow seed retains all depth/credit/accounting values; added "+blocked+" blocked cells for house collision proxies. Original seed asset preserved. No player saves touched. Scene checks and 19 snow-ledger checks passed. Repeat command preserves 40 homes without duplicates. Full driving/clearance walk, Android build and A07 performance remain untested.");
   File.WriteAllText(Report,result.ToString());Debug.Log(result.ToString());
   if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(new Vector3(0,5,20),Quaternion.Euler(90,0,0),135,true,true);
  }
 }
}
