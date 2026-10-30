using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Snowglow.Editor {
 public static class StreetLightsIntake {
  const string Source=CatalogPreparation.Root+"/Sources/AlpineStreetLights/";
  const string Request="Logs/ImportStreetLights.request",Report="Logs/AlpineStreetLights/UnityIntake.txt";
  const string SeedPath=CatalogPreparation.Root+"/SceneData/StreetLightsSnowSeed.asset";
  const string Hash="50F565BAE9ECF8CEA58338E271A939C914EAB6C6FD2FBCD6567F6356286EC88D";
  [Serializable]public class Export {public Fixture[] fixtures;}
  [Serializable]public class Fixture {public string index,name,model;public int triangles;public float[] size,proxyCenter,proxySize;public Lamp[] lights;}
  [Serializable]public class Lamp {public string name;public float[] position,color;public float sourceWatts;}
  static Vector3 V(float[] a)=>new Vector3(a[0],a[1],a[2]);
  static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException("Street lights: "+message);}
  static T[] All<T>(Scene s) where T:Component=>s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
  static CatalogPlacement[] Placed(Scene s)=>All<CatalogPlacement>(s).Where(p=>(p.placementId??"").StartsWith("pinefall-light-",StringComparison.Ordinal)).ToArray();
  static string Pose(Transform t)=>t.localToWorldMatrix.ToString("R");
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){
   if(!File.Exists(Request)){EditorApplication.update-=Pending;return;}
   if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
   EditorApplication.update-=Pending;bool resume=File.ReadAllText(Request).Trim()=="resume";File.Delete(Request);
   try{Install(resume);}catch(Exception e){Directory.CreateDirectory("Logs/AlpineStreetLights");File.WriteAllText(Report,"FAIL: "+e);Debug.LogException(e);}
  }
  static Material Material(bool glow){
   string path=Source+(glow?"Luminous.mat":"Frame.mat");var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!m){m=new Material(Shader.Find(glow?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Simple Lit"));AssetDatabase.CreateAsset(m,path);}
   m.name=glow?"Street lights | opaque luminous palette":"Street lights | opaque frame palette";m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"Palette.png"));m.SetColor("_BaseColor",Color.white);m.SetFloat("_Surface",0);m.SetFloat("_ZWrite",1);m.SetFloat("_Cull",2);m.renderQueue=2000;m.enableInstancing=true;
   if(!glow){m.SetColor("_SpecColor",Color.black);m.SetFloat("_Smoothness",0);}EditorUtility.SetDirty(m);return m;
  }
  static CatalogAsset Prepare(Fixture f,Material snow){
   string id="alpine-street-light-"+f.index;var a=CatalogPreparation.Library().Find(id);
   if(a&&a.tags.Contains("alpine-street-light")){CatalogPreparation.RecordMetrics(a);ValidateEntry(a,f);return a;}
   string path=Source+f.model+".fbx";var importer=AssetImporter.GetAtPath(path) as ModelImporter;Require(importer!=null,"FBX not imported: "+path);
   importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;importer.globalScale=1;importer.isReadable=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
   importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"StreetLightPalette"),Material(false));importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"StreetLightGlow"),Material(true));importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"StreetLightSnow"),snow);importer.SaveAndReimport();
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);Require(model,"missing imported model");Require((CatalogValidation.BoundsOf(model).size-V(f.size)).magnitude<.02f,"metres/axes mismatch: "+id);
   if(!a){var draft=CatalogPreparation.Draft(f.name,CatalogKind.Landscape,AssetSource.BlenderScript,null,"User supplied ChatGPT-generated Alpine_Street_Lights_12_Variants.blend, collection "+f.index+" "+f.name+"; SHA256 "+Hash+". Embedded text was not executed.");draft.id=id;draft.creationSourcePath="SourceAssets/AlpineStreetLights/Alpine_Street_Lights_12_Variants.blend";a=CatalogPreparation.Prepare(model,CatalogKind.Landscape,f.name,1,Vector3.zero,AssetSource.BlenderScript,draft.provenance,draft);}
   var root=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(a.prefab));
   try{
    var visual=root.transform.Find("VisualRoot");visual.localPosition=Vector3.zero;var raw=visual.GetChild(0);
    if(PrefabUtility.IsPartOfPrefabInstance(raw))PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(raw.gameObject),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
    var frames=raw.GetComponentsInChildren<Transform>();Vector3 Basis(string name)=>root.transform.InverseTransformPoint(frames.Single(t=>t.name==name).position);
    var bx=Basis("BasisX");var by=Basis("BasisY");var bz=Basis("BasisZ");Require(Mathf.Abs(bx.x)>.99f&&Mathf.Abs(by.z)>.99f&&Mathf.Abs(bz.y)>.99f,"basis mismatch");
    Vector3 Position(float[] xyz)=>bx*xyz[0]+by*xyz[1]+bz*xyz[2];
    foreach(var collider in root.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider.gameObject);
    foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true)){r.sharedMaterial=r.name=="SnowCap"?snow:r.name=="Luminous"?Material(true):Material(false);if(r.name=="Luminous"){r.receiveShadows=false;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}}
    var cap=root.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.name=="SnowCap");cap.name="SnowCap | removable decorative snow";
    var proxy=new GameObject("Collision | "+(f.index=="06"?"wall fixture":"pole envelope"));proxy.transform.SetParent(root.transform,false);var box=proxy.AddComponent<BoxCollider>();box.center=Position(f.proxyCenter);box.size=new Vector3(f.proxySize[0],f.proxySize[2],f.proxySize[1]);
    var connection=CatalogPreparation.Anchor(root.transform,"mount",AnchorRole.Connection,f.index=="06"?by*.145f+bz*2.3f:Vector3.zero);connection.interfaceId=f.index=="06"?"wall-fixture":"ground-fixture";connection.transform.localRotation=Quaternion.LookRotation(-by,bz);
    var optional=new GameObject("Optional lights | enable in Editor (mobile default off)");optional.transform.SetParent(root.transform,false);
    foreach(var source in f.lights){var go=new GameObject(source.name);go.transform.SetParent(optional.transform,false);go.transform.localPosition=Position(source.position);var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(source.color[0],source.color[1],source.color[2]);light.intensity=f.index=="01"?.35f:.8f;light.range=f.index=="01"?2.5f:Mathf.Clamp(f.size[1]*1.2f,3,10);light.shadows=LightShadows.None;light.renderMode=LightRenderMode.Auto;}
    optional.SetActive(false);foreach(var frame in frames.Where(t=>t.name.StartsWith("Basis",StringComparison.Ordinal)))UnityEngine.Object.DestroyImmediate(frame.gameObject);
    PrefabUtility.SaveAsPrefabAsset(root,AssetDatabase.GetAssetPath(a.prefab));
   }finally{PrefabUtility.UnloadPrefabContents(root);}
   a.tags=new[]{"alpine-street-light","removable-snow","optional-lights-off",f.index=="06"?"wall-mounted":"ground-mounted",f.model.ToLowerInvariant()};a.minimumScale=.75f;a.maximumScale=1.25f;a.status=PreparationStatus.NeedsReview;a.visualReviewed=false;
   a.assumptions="Metres, Y-up. Original mounting origin retained. Three separate mesh/material groups: Frame, opaque unlit Luminous, removable SnowCap. Source rigid fixture already combines metal parts; original Blender modules are preserved. Narrow pole/base proxy avoids an invisible barrier under outreach arms; wall variant uses a compact fixture box. Decorative snow is outside the snow ledger. Optional point lights are saved under an inactive Editor-toggle group, shadowless with proposed URP intensity/range; Blender watts are provenance, not directly equivalent Unity intensity.";
   a.reviewNotes="Authorized prototype placement, not Ready publication. Review wall seating, pole collisions, night lighting and A07 timings.";
   CatalogPreparation.RecordMetrics(a);ValidateEntry(a,f);EditorUtility.SetDirty(a);return a;
  }
  static void ValidateEntry(CatalogAsset a,Fixture f){
   var checks=CatalogValidation.Check(a);Require(checks.Ok,"catalog check: "+a.id+"\n"+checks);Require(a.triangles==f.triangles,"triangle mismatch: "+a.id+" imported "+a.triangles+" expected "+f.triangles);Require(a.renderers==3&&a.materialSlots==3,"three separate groups required: "+a.id);
   Require(a.prefab.GetComponentsInChildren<Light>(true).Length==f.lights.Length,"optional light count differs");Require(a.prefab.GetComponentsInChildren<Light>().Length==0,"light active by default");Require(a.prefab.GetComponentsInChildren<Camera>(true).Length==0,"camera leaked");Require(a.prefab.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="SnowCap | removable decorative snow"),"removable cap missing");
  }
  static void Place(Scene scene,CatalogAsset[] entries){
   var existing=Placed(scene);if(existing.Length>0){ValidatePlacements(scene,entries);return;}
   var originals=All<Transform>(scene).Where(t=>t.name=="Street light | cylinders and cubes").OrderBy(t=>t.position.x).ThenBy(t=>t.position.z).ToArray();Require(originals.Length==9,"nine prototype lamp positions required");
   var parent=originals[0].parent;var terrain=All<Terrain>(scene).Single();var group=new GameObject("Street lights | prepared alpine fixtures").transform;group.SetParent(parent,false);int number=1;
   GameObject Put(int index,Vector3 position,Quaternion rotation){var a=entries[index-1];var go=(GameObject)PrefabUtility.InstantiatePrefab(a.prefab,scene);go.transform.SetParent(group,true);go.transform.SetPositionAndRotation(position,rotation);go.GetComponent<CatalogPlacement>().placementId="pinefall-light-"+(number++).ToString("D2");return go;}
   int[] order={4,5,7,3,8,9,10,11,12};
   for(int i=0;i<originals.Length;i++){
    var position=originals[i].position;int index=order[i];float lane=new[]{-65f,0,65f}.OrderBy(x=>Mathf.Abs(x-position.x)).First();float toward=lane-position.x;float reach=entries[index-1].localBounds.center.x;
    Put(index,new Vector3(position.x,terrain.SampleHeight(position)+terrain.transform.position.y-.025f,position.z),Quaternion.Euler(0,reach*toward<0?180:0,0));UnityEngine.Object.DestroyImmediate(originals[i].gameObject);
   }
   foreach(float z in new[]{-100f,-80}){var p=new Vector3(-72.5f,0,z);p.y=terrain.SampleHeight(p)+terrain.transform.position.y-.025f;Put(1,p,Quaternion.identity);}
   foreach(float z in new[]{15f,47}){var p=new Vector3(-77,0,z);p.y=terrain.SampleHeight(p)+terrain.transform.position.y-.025f;Put(2,p,Quaternion.identity);}
   var mount=entries[5].prefab.GetComponentsInChildren<AssetAnchor>().Single(a=>a.id=="mount").transform;
   var rotationWall=Quaternion.LookRotation(Vector3.forward)*Quaternion.Inverse(mount.localRotation);var target=new Vector3(-94,8,-102.97f);
   Put(6,target-rotationWall*mount.localPosition,rotationWall);ValidatePlacements(scene,entries);
  }
  static void ValidatePlacements(Scene scene,CatalogAsset[] entries){
   Physics.SyncTransforms();
   var placed=Placed(scene);Require(placed.Length==14&&placed.Select(p=>p.placementId).Distinct().Count()==14,"14 unique fixture placements required");Require(entries.All(a=>placed.Any(p=>p.assetId==a.id)),"every variant must be used");Require(!All<Transform>(scene).Any(t=>t.name=="Street light | cylinders and cubes"),"prototype lamps remain");
   foreach(var p in placed){Require(PrefabUtility.IsPartOfPrefabInstance(p),"disconnected fixture prefab");Require(p.GetComponentsInChildren<Light>().Length==0,"optional light enabled in scene");Require(p.GetComponentsInChildren<Collider>().Length==1,"one pole/wall proxy required");}
   var roads=All<Renderer>(scene).Where(r=>r.name=="Road | asphalt").Select(r=>r.bounds).ToArray();
   var houses=All<CatalogPlacement>(scene).Where(p=>(p.placementId??"").StartsWith("pinefall-home-",StringComparison.Ordinal)).SelectMany(p=>p.GetComponentsInChildren<Collider>()).ToArray();
   foreach(var p in placed.Where(p=>p.assetId!="alpine-street-light-06")){
    var b=p.GetComponentInChildren<Collider>().bounds;
    Require(!roads.Any(r=>b.min.x<r.max.x&&b.max.x>r.min.x&&b.min.z<r.max.z&&b.max.z>r.min.z),"pole on road: "+p.placementId+" bounds "+b+" position "+p.transform.position);
    Require(!houses.Any(c=>c.bounds.Intersects(b)),"pole intersects house: "+p.placementId);
   }
   var wall=placed.Single(p=>p.assetId=="alpine-street-light-06");var anchor=wall.GetComponentsInChildren<AssetAnchor>().Single(a=>a.id=="mount");Require((anchor.transform.position-new Vector3(-94,8,-102.97f)).magnitude<.005f,"wall mount not seated on workshop facade");
  }
  public static void PlacePreparedLights(Scene scene){
   if(!File.Exists(Source+"ExportMetrics.json"))return;var data=JsonUtility.FromJson<Export>(File.ReadAllText(Source+"ExportMetrics.json"));var entries=data.fixtures.Select(f=>CatalogPreparation.Library().Find("alpine-street-light-"+f.index)).ToArray();if(entries.Length==12&&entries.All(a=>a&&a.prefab))Place(scene,entries);
  }
  static string SnowData(SnowSeed seed)=>JsonUtility.ToJson(new SnowVolumeGrid{depth=seed.grid.depth,credit=seed.grid.credit,initialVolume=seed.grid.initialVolume,disposed=seed.grid.disposed,revision=seed.grid.revision,blocked=new bool[0]});
  static int RefreshObstacles(SnowField snow){
   var seed=AssetDatabase.LoadAssetAtPath<SnowSeed>(SeedPath);if(!seed){Directory.CreateDirectory(Path.GetDirectoryName(SeedPath));AssetDatabase.Refresh();seed=UnityEngine.Object.Instantiate(snow.seed);seed.name="Pinefall | prepared street light obstacle mask";AssetDatabase.CreateAsset(seed,SeedPath);}
   Require(SnowData(seed)==SnowData(snow.seed),"different saved snow volumes in scene copy");snow.seed=seed;Physics.SyncTransforms();int changed=0;
   for(int i=0;i<seed.grid.blocked.Length;i++){bool blocked=Physics.CheckBox(new Vector3(seed.grid.X(i),SnowField.Ground+.85f,seed.grid.Z(i)),new Vector3(SnowVolumeGrid.Cell*.45f,.65f,SnowVolumeGrid.Cell*.45f),Quaternion.identity,1<<0,QueryTriggerInteraction.Ignore);if(seed.grid.blocked[i]!=blocked){seed.grid.blocked[i]=blocked;changed++;}}
   EditorUtility.SetDirty(seed);return changed;
  }
  static void Preview(CatalogAsset a,string index){
   var p=new PreviewRenderUtility();try{var obj=UnityEngine.Object.Instantiate(a.prefab);p.AddSingleGO(obj);var b=CatalogValidation.BoundsOf(obj);p.camera.orthographic=true;p.camera.orthographicSize=Mathf.Max(b.extents.y,b.extents.x)*1.25f;p.camera.nearClipPlane=.05f;p.camera.farClipPlane=100;p.camera.transform.position=b.center+new Vector3(1,.3f,-1)*b.size.magnitude*2;p.camera.transform.LookAt(b.center);p.camera.clearFlags=CameraClearFlags.SolidColor;p.camera.backgroundColor=new Color(.25f,.38f,.47f);p.ambientColor=new Color(.4f,.4f,.4f);p.lights[0].intensity=1.1f;p.lights[0].transform.rotation=p.camera.transform.rotation;p.lights[1].intensity=.4f;p.lights[1].transform.rotation=Quaternion.Euler(20,150,0);p.BeginStaticPreview(new Rect(0,0,480,600));p.Render(true);var image=p.EndStaticPreview();File.WriteAllBytes("Logs/AlpineStreetLights/"+index+"-unity.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}finally{p.Cleanup();}
  }
  [MenuItem("Snowglow/Asset intake/Import twelve alpine street lights")]
  public static void Install()=>Install(false);
  static void Install(bool resume){
   Require(!EditorApplication.isPlayingOrWillChangePlaymode,"stop Play first");var scene=SceneManager.GetActiveScene();Require(scene.path==SceneValidation.ScenePath&&(!scene.isDirty||(resume&&Placed(scene).Length==14&&File.Exists("Logs/AlpineStreetLights/Pinefall-before.unity"))),"open saved Pinefall first");Directory.CreateDirectory("Logs/AlpineStreetLights");if(!File.Exists("Logs/AlpineStreetLights/Pinefall-before.unity"))File.Copy(scene.path,"Logs/AlpineStreetLights/Pinefall-before.unity");
   var session=All<PrototypeSession>(scene).Single();var dataBefore=SnowData(session.Snow.seed);var fleet=session.Fleet.Select(m=>m.GetInstanceID()).ToArray();var snapshots=All<CatalogPlacement>(scene).Where(p=>!string.IsNullOrEmpty(p.placementId)).ToDictionary(p=>p.placementId,p=>p.assetId+"|"+Pose(p.transform));
   var paths=All<Terrain>(scene).Select(t=>AssetDatabase.GetAssetPath(t.terrainData)).Concat(session.Snow.GetComponentsInChildren<MeshFilter>().Select(m=>AssetDatabase.GetAssetPath(m.sharedMesh))).Concat(new[]{AssetDatabase.GetAssetPath(session.Snow.seed)}).Where(p=>!string.IsNullOrEmpty(p)&&p!=SeedPath).Distinct().ToDictionary(p=>p,p=>AssetDatabase.GetAssetDependencyHash(p).ToString());
   var texture=AssetImporter.GetAtPath(Source+"Palette.png") as TextureImporter;Require(texture!=null,"palette not imported");texture.filterMode=FilterMode.Point;texture.mipmapEnabled=false;texture.wrapMode=TextureWrapMode.Clamp;texture.textureCompression=TextureImporterCompression.Uncompressed;texture.sRGBTexture=true;texture.SaveAndReimport();
   var data=JsonUtility.FromJson<Export>(File.ReadAllText(Source+"ExportMetrics.json"));Require(data.fixtures.Length==12,"12 collections required");var entries=data.fixtures.Select(f=>Prepare(f,session.Snow.palette.snow)).ToArray();AssetDatabase.SaveAssets();
   Place(scene,entries);int changed=RefreshObstacles(session.Snow);Require(fleet.SequenceEqual(session.Fleet.Select(m=>m.GetInstanceID())),"fleet changed");SceneValidation.Validate(scene);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();Require(EditorSceneManager.SaveScene(scene),"scene save failed");
   scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);
   // OpenScene unloads catalog ScriptableObjects not referenced by gameplay.
   // Reacquire native asset handles before checking prefabs or rendering them.
   entries=data.fixtures.Select(f=>CatalogPreparation.Library().Find("alpine-street-light-"+f.index)).ToArray();
   ValidatePlacements(scene,entries);SceneValidation.Validate(scene);MechanicsChecks.Run();session=All<PrototypeSession>(scene).Single();Require(dataBefore==SnowData(session.Snow.seed),"snow volumes changed");
   var current=All<CatalogPlacement>(scene).Where(p=>!string.IsNullOrEmpty(p.placementId)).ToDictionary(p=>p.placementId,p=>p.assetId+"|"+Pose(p.transform));foreach(var item in snapshots)Require(current.TryGetValue(item.Key,out var state)&&state==item.Value,"existing catalog placement changed: "+item.Key);foreach(var item in paths)Require(item.Value==AssetDatabase.GetAssetDependencyHash(item.Key).ToString(),"protected terrain/snow asset changed");
   var report=new StringBuilder("PASS: Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+". Twelve prepared street-light variants, 14 scene placements, all nine prototype lamps replaced. Saved and reopened.\n");
   foreach(var f in data.fixtures){var a=entries.Single(e=>e.id=="alpine-street-light-"+f.index);ValidateEntry(a,f);report.AppendLine(f.index+" "+a.displayName+": "+a.triangles+" triangles / "+a.prefab.GetComponentsInChildren<MeshFilter>(true).Sum(m=>m.sharedMesh.vertexCount)+" imported vertices / 3 renderers / 3 slots; "+f.lights.Length+" optional shadowless point lights (inactive); "+Placed(scene).Count(p=>p.assetId==a.id)+" placements; Needs Review.");Preview(a,f.index);}
   report.AppendLine("Total placed mesh triangles: "+Placed(scene).Sum(p=>entries.Single(a=>a.id==p.assetId).triangles)+"; 42 renderer/material slots, three shared materials across the set. Source geometry has not been decimated. All caps removable; luminous surfaces opaque unlit, no transparent glass or enabled local lights. Point-light intensity/range are URP proposals, not conversions of Blender watts.");
   report.AppendLine("Pole proxies clear roads/houses; wall mount aligned to workshop facade. Existing homes/mountains/fleet and original terrain/snow assets preserved. Scene-specific seed refresh changed "+changed+" obstacle cells, preserving all depth/credit/volume values. Scene structural and 19 snow-ledger checks passed. No runtime assembly or player-save writes. Twelve native previews rendered. Full driving, night light appearance, Android packaging and Galaxy A07 timings remain untested.");
   File.WriteAllText(Report,report.ToString());Debug.Log(report.ToString());if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(Placed(scene)[0].transform.position+Vector3.up*3,Quaternion.Euler(20,135,0),8,true,true);
  }
 }
}
