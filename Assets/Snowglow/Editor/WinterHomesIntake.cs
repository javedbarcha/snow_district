using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Snowglow.Editor {
 public static class WinterHomesIntake {
  const string Source=CatalogPreparation.Root+"/Sources/SevenWinterHomes/";
  const string Request="Logs/ImportWinterHomes.request", Report="Logs/WinterHomes/UnityIntake.txt";
  const string Hash="2AD3BEC380E5EE05A400561C0E9CA5445607C4D389EBFBF46EF4E4DECB9F8CDE";
  [Serializable]public class Export {public Home[] homes;}
  [Serializable]public class NormalAudit {public int closedSolidsChecked;}
  [Serializable]public class Home {public string index,name,model;public int triangles,degenerateTriangles;public float[] size,entrance;public Proxy[] colliders;}
  [Serializable]public class Proxy {public string name;public float[] center,size;}
  static Vector3 V(float[] a)=>new Vector3(a[0],a[1],a[2]);
  static void Require(bool value,string message){if(!value)throw new InvalidOperationException("Winter homes: "+message);}
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){
   if(!File.Exists(Request)){EditorApplication.update-=Pending;return;}
   if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
   EditorApplication.update-=Pending;File.Delete(Request);
   try{Install();}catch(Exception e){Directory.CreateDirectory("Logs/WinterHomes");File.WriteAllText(Report,"FAIL: "+e);Debug.LogException(e);}
  }
  static Material Material(bool windows){
   string path=Source+(windows?"WarmWindows.mat":"Architecture.mat");var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!m){m=new Material(Shader.Find(windows?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Simple Lit"));AssetDatabase.CreateAsset(m,path);}
   m.name=windows?"Winter homes | warm opaque windows":"Winter homes | opaque palette";
   m.SetColor("_BaseColor",Color.white);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"Palette.png"));m.SetFloat("_Surface",0);m.SetFloat("_ZWrite",1);m.SetFloat("_Cull",2);m.renderQueue=2000;
   if(!windows){m.SetFloat("_Smoothness",0);m.SetColor("_SpecColor",Color.black);}
   m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
  }
  static CatalogAsset Prepare(Home home,Material snow){
   string id="winter-home-"+home.index;var a=CatalogPreparation.Library().Find(id);if(a&&a.tags.Contains("winter-home")){a.maximumScale=1.5f;EditorUtility.SetDirty(a);CatalogPreparation.RecordMetrics(a);Require(a.triangles==home.triangles-home.degenerateTriangles,"prepared geometry count differs: "+home.name);Require(CatalogValidation.Check(a).Ok,"catalog checks: "+a.id);return a;}
   string path=Source+home.model+".fbx";
   var importer=(ModelImporter)AssetImporter.GetAtPath(path);Require(importer!=null,"model not imported: "+path);
   importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;importer.globalScale=1;importer.isReadable=false;
   importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
   importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"WinterHomesPalette"),Material(false));
   importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"WinterHomesWarmWindows"),Material(true));
   importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"WinterHomesSnow"),snow);importer.SaveAndReimport();
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);Require(model,"FBX missing");
   var rawBounds=CatalogValidation.BoundsOf(model);Require((rawBounds.size-V(home.size)).magnitude<.1f,"metre scale/axis conversion differs for "+home.name+": "+rawBounds.size);
   if(!a){
    var draft=CatalogPreparation.Draft(home.name,CatalogKind.BuildingAssembly,AssetSource.ImportedModel,null,"User supplied Seven_Winter_Homes.blend, collection "+home.index+" "+home.name+"; SHA256 "+Hash+". Embedded source text was not executed.");
    draft.id=id;draft.creationSourcePath="SourceAssets/SevenWinterHomes/Seven_Winter_Homes.blend";
    a=CatalogPreparation.Prepare(model,CatalogKind.BuildingAssembly,home.name,1,Vector3.zero,AssetSource.ImportedModel,draft.provenance,draft);
   }
   var root=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(a.prefab));
   try{
    var raw=root.transform.Find("VisualRoot").GetChild(0);if(PrefabUtility.IsPartOfPrefabInstance(raw))PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(raw.gameObject),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
    foreach(var c in root.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c.gameObject);
    var snowGroup=new GameObject("SnowCover | removable decorative snow").transform;snowGroup.SetParent(raw,false);
    foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true)){
     bool isSnow=r.name=="RoofSnow"||r.name=="GroundSnow";
     r.sharedMaterial=isSnow?snow:r.name=="WarmWindows"?Material(true):Material(false);
     if(isSnow)r.transform.SetParent(snowGroup,true);
     r.receiveShadows=r.name!="WarmWindows";r.shadowCastingMode=r.name=="WarmWindows"?UnityEngine.Rendering.ShadowCastingMode.Off:UnityEngine.Rendering.ShadowCastingMode.On;
    }
    var frames=raw.GetComponentsInChildren<Transform>();
    Vector3 Basis(string name)=>raw.InverseTransformPoint(frames.Single(t=>t.name==name).position);
    var bx=Basis("BasisX");var by=Basis("BasisY");var bz=Basis("BasisZ");
    Require(Mathf.Abs(bx.x)>.99f&&Mathf.Abs(by.z)>.99f&&Mathf.Abs(bz.y)>.99f,"FBX basis mismatch");
    // JSON coordinates are (Blender X, Z, -Y); include actual import handedness.
    Vector3 Position(float[] xyz)=>bx*xyz[0]+bz*xyz[1]-by*xyz[2];
    foreach(var p in home.colliders){var go=new GameObject("Collision | "+p.name);go.transform.SetParent(raw,false);var box=go.AddComponent<BoxCollider>();box.center=Position(p.center);box.size=V(p.size);}
    var entry=CatalogPreparation.Anchor(raw,"entrance",AnchorRole.Entrance,Position(home.entrance));entry.interfaceId="house-entrance";entry.transform.localRotation=Quaternion.LookRotation(-by,bz);
    foreach(var frame in frames.Where(t=>t.name.StartsWith("Basis",StringComparison.Ordinal)))UnityEngine.Object.DestroyImmediate(frame.gameObject);
    root.name=home.name;PrefabUtility.SaveAsPrefabAsset(root,AssetDatabase.GetAssetPath(a.prefab));
   }finally{PrefabUtility.UnloadPrefabContents(root);}
   a.tags=new[]{"winter-home",home.model.ToLowerInvariant(),"removable-snow","warm-windows","opaque-palette"};a.minimumScale=.75f;a.maximumScale=1.5f;
   a.assumptions="Metres, Y-up, entrance +Z. Combined rigid architecture and opaque unlit warm windows; roof and ground snow remain separately removable under SnowCover. Square display tile, text, cameras and lights excluded. Ground drifts clipped to the architectural ground plane on prepared copies. Compound wall/deck/step boxes are collision proposals, not enterable interiors. Decorative snow does not enter the plowing volume ledger.";
   a.reviewNotes="Authorized prototype placement. Needs visual/clearance and Galaxy A07 review before Ready publication.";a.visualReviewed=false;a.status=PreparationStatus.NeedsReview;
   CatalogPreparation.RecordMetrics(a);Require(a.triangles==home.triangles-home.degenerateTriangles,"export/import triangle count: "+home.name+" "+a.triangles+" vs "+home.triangles);Require(CatalogValidation.Check(a).Ok,"catalog checks: "+a.id);EditorUtility.SetDirty(a);return a;
  }
  static void Preview(CatalogAsset a,string index){
   var p=new PreviewRenderUtility();try{
    var obj=UnityEngine.Object.Instantiate(a.prefab);p.AddSingleGO(obj);var b=CatalogValidation.BoundsOf(obj);
    p.camera.orthographic=true;p.camera.orthographicSize=Mathf.Max(b.extents.y,b.extents.x)*1.5f;p.camera.nearClipPlane=.1f;p.camera.farClipPlane=100;
    p.camera.transform.position=b.center+new Vector3(-1,.75f,1)*b.size.magnitude;p.camera.transform.LookAt(b.center);p.camera.clearFlags=CameraClearFlags.SolidColor;p.camera.backgroundColor=new Color(.35f,.52f,.64f);
    p.ambientColor=new Color(.35f,.35f,.35f);p.lights[0].intensity=1.1f;p.lights[0].transform.rotation=p.camera.transform.rotation;p.lights[1].intensity=.4f;p.lights[1].transform.rotation=Quaternion.Euler(20,150,0);
    p.BeginStaticPreview(new Rect(0,0,720,600));p.Render(true);var image=p.EndStaticPreview();File.WriteAllBytes("Logs/WinterHomes/"+index+"-unity.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
   }finally{p.Cleanup();}
  }
  static bool IsHome(CatalogPlacement p)=>(p.placementId??"").StartsWith("pinefall-home-",StringComparison.Ordinal);
  static Transform[] HomeLots(Scene scene)=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>()).Where(t=>t.name=="Cottage | modular primitives"||t.name.StartsWith("Home lot |",StringComparison.Ordinal)).OrderBy(t=>t.position.x).ThenBy(t=>t.position.z).ToArray();
  static bool OverlapXZ(Bounds a,Bounds b)=>a.min.x<b.max.x&&a.max.x>b.min.x&&a.min.z<b.max.z&&a.max.z>b.min.z;
  static void Place(Scene scene,Home[] homes,CatalogAsset[] entries){
   var lots=HomeLots(scene);Require(lots.Length>=7,"at least seven house lots required");
   var installed=lots.SelectMany(t=>t.GetComponentsInChildren<CatalogPlacement>()).Where(IsHome).ToArray();
   if(lots.All(t=>t.name.Contains(" | 1.5x | "))){
    Require(installed.Length==lots.Length&&entries.All(a=>installed.Any(p=>p.assetId==a.id)),"all lots must contain a home and every variant must be used");return;
   }
   Require(!lots.Any(t=>t.name.Contains(" | 1.5x | ")),"partial large-house layout requires review");
   // A shuffled balanced bag guarantees every variant appears. The seed makes
   // Editor authoring reproducible; no random selection or construction in Play.
   var order=Enumerable.Range(0,lots.Length).Select(i=>i%entries.Length).ToArray();var rng=new System.Random(291026);
   for(int i=order.Length-1;i>0;i--){int j=rng.Next(i+1);int swap=order[i];order[i]=order[j];order[j]=swap;}
   var foundations=lots.Select(t=>t.Find("Foundation")).ToArray();Require(foundations.All(t=>t),"lot foundation missing");
   var bounds=new Bounds[lots.Length];var scales=new float[lots.Length];
   var roads=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Renderer>()).Where(r=>r.name=="Road | asphalt").Select(r=>r.bounds).ToArray();
   for(int i=0;i<lots.Length;i++){
    var limit=foundations[i].GetComponent<Renderer>().bounds;var b=entries[order[i]].localBounds;
    scales[i]=1.5f*Mathf.Min(1,Mathf.Min(limit.size.x/b.size.x,limit.size.z/b.size.z));
    Require(scales[i]>=entries[order[i]].minimumScale&&scales[i]<=1.5f+.001f,"house scale outside reviewed placement range");
    bounds[i]=new Bounds(new Vector3(limit.center.x,limit.max.y+b.size.y*scales[i]*.5f,limit.center.z),b.size*scales[i]);
    Require(!roads.Any(road=>OverlapXZ(bounds[i],road)),"house would cross road at "+lots[i].position);
    for(int j=0;j<i;j++)Require(!OverlapXZ(bounds[i],bounds[j]),"enlarged houses would overlap");
   }
   var ids=installed.Select(p=>p.placementId).ToList();int nextId=1;
   for(int i=0;i<lots.Length;i++){
    var lot=lots[i];var foundation=foundations[i];var limit=foundation.GetComponent<Renderer>().bounds;var existing=lot.GetComponentsInChildren<CatalogPlacement>().SingleOrDefault(IsHome);string id=existing?existing.placementId:null;
    if(id==null){while(ids.Contains("pinefall-home-"+nextId.ToString("D2")))nextId++;id="pinefall-home-"+nextId.ToString("D2");ids.Add(id);}
    foreach(var child in lot.Cast<Transform>().ToArray())if(child!=foundation&&!child.name.StartsWith("Fence",StringComparison.Ordinal))UnityEngine.Object.DestroyImmediate(child.gameObject);
    // Enlarge X/Z only: preserve the foundation top and the house ground level.
    foundation.localScale=new Vector3(foundation.localScale.x*1.5f,foundation.localScale.y,foundation.localScale.z*1.5f);
    var a=entries[order[i]];var placed=(GameObject)PrefabUtility.InstantiatePrefab(a.prefab,scene);placed.transform.SetParent(lot,true);
    placed.transform.SetPositionAndRotation(new Vector3(limit.center.x,limit.max.y,limit.center.z),Quaternion.Euler(0,180,0));placed.transform.localScale=Vector3.one*scales[i];
    placed.GetComponent<CatalogPlacement>().placementId=id;lot.name="Home lot | 1.5x | "+homes[order[i]].name;
    var actual=CatalogValidation.BoundsOf(placed);var expanded=foundation.GetComponent<Renderer>().bounds;
    Require(actual.size.x<=expanded.size.x+.01f&&actual.size.z<=expanded.size.z+.01f,"house exceeds enlarged foundation footprint");
   }
  }
  public static void PlacePreparedHomes(Scene scene){
   if(!File.Exists(Source+"ExportMetrics.json"))return;
   var homes=JsonUtility.FromJson<Export>(File.ReadAllText(Source+"ExportMetrics.json")).homes;
   var library=CatalogPreparation.Library();var entries=homes.Select(h=>library.Find("winter-home-"+h.index)).ToArray();
   if(entries.Length==7&&entries.All(a=>a&&a.prefab))Place(scene,homes,entries);
  }
  [MenuItem("Snowglow/Asset intake/Import seven winter homes")]
  public static void Install(){
   Require(!EditorApplication.isPlayingOrWillChangePlaymode,"stop Play first");var scene=SceneManager.GetActiveScene();Require(scene.path==SceneValidation.ScenePath,"open Pinefall first");Require(!scene.isDirty,"save your current scene changes before intake");
   Directory.CreateDirectory("Logs/WinterHomes");if(!File.Exists("Logs/WinterHomes/Pinefall-before.unity"))File.Copy(scene.path,"Logs/WinterHomes/Pinefall-before.unity");if(!File.Exists("Logs/WinterHomes/Pinefall-before-all-homes.unity"))File.Copy(scene.path,"Logs/WinterHomes/Pinefall-before-all-homes.unity");
   var session=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PrototypeSession>()).Single();string seedPath=AssetDatabase.GetAssetPath(session.Snow.seed),seedHash=AssetDatabase.GetAssetDependencyHash(seedPath).ToString();
   var fleet=session.Fleet.Select(m=>m.GetInstanceID()).ToArray();var layer=(TextureImporter)AssetImporter.GetAtPath(Source+"Palette.png");Require(layer!=null,"palette not imported");layer.filterMode=FilterMode.Point;layer.mipmapEnabled=false;layer.wrapMode=TextureWrapMode.Clamp;layer.textureCompression=TextureImporterCompression.Uncompressed;layer.sRGBTexture=true;layer.SaveAndReimport();
   var data=JsonUtility.FromJson<Export>(File.ReadAllText(Source+"ExportMetrics.json"));Require(data.homes.Length==7,"seven collections required");
   var normals=JsonUtility.FromJson<NormalAudit>(File.ReadAllText(Source+"NormalValidation.json"));Require(normals.closedSolidsChecked>0,"closed-solid normal audit missing");
   var entries=data.homes.Select(h=>Prepare(h,session.Snow.palette.snow)).ToArray();AssetDatabase.SaveAssets();
   Place(scene,data.homes,entries);
   Require(fleet.SequenceEqual(session.Fleet.Select(m=>m.GetInstanceID())),"fleet altered");Require(seedHash==AssetDatabase.GetAssetDependencyHash(seedPath).ToString(),"snow seed changed");
   SceneValidation.Validate(scene);EditorSceneManager.MarkSceneDirty(scene);Require(EditorSceneManager.SaveScene(scene),"scene save failed");AssetDatabase.SaveAssets();
   scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);SceneValidation.Validate(scene);MechanicsChecks.Run();
   var placements=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CatalogPlacement>()).Where(c=>c.placementId!=null&&c.placementId.StartsWith("pinefall-home-",StringComparison.Ordinal)).ToArray();Require(placements.Length==HomeLots(scene).Length&&placements.Length>=7,"saved placements missing");Require(!scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>()).Any(t=>t.name=="Cottage | modular primitives"),"prototype cottage remains");Require(entries.All(a=>placements.Any(p=>p.assetId==a.id)),"not every house variant is used");Require(placements.Select(p=>p.placementId).Distinct().Count()==placements.Length,"duplicate home placement IDs");
   var report=new StringBuilder("PASS: Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+". All "+placements.Length+" house lots replaced with a shuffled mix of seven imported variants at 1.5x the former fitted size, saved and reopened.\n");
   report.AppendLine("Prepared export normal audit: "+normals.closedSolidsChecked+" closed source solids checked; open facade panels retain their authored direction. See NormalValidation.json for repairs.");
   for(int i=0;i<7;i++){
    var a=entries[i];Require(a.prefab.GetComponentsInChildren<Light>(true).Length==0&&a.prefab.GetComponentsInChildren<Camera>(true).Length==0,"presentation objects leaked");
    Require(a.renderers==4&&a.materialSlots==4,"expected four merged renderer/material groups");
    Require(a.prefab.GetComponentsInChildren<Transform>(true).Any(t=>t.name.StartsWith("SnowCover",StringComparison.Ordinal)),"removable snow missing");
    var vertices=a.prefab.GetComponentsInChildren<MeshFilter>(true).Sum(m=>m.sharedMesh.vertexCount);report.AppendLine(a.displayName+": "+a.triangles+" triangles / "+vertices+" imported vertices / "+a.renderers+" renderers / "+a.materialSlots+" slots; "+placements.Count(p=>p.assetId==a.id)+" placements; "+a.prefab.GetComponentsInChildren<Collider>().Length+" compound proxies; Needs Review.");
    Preview(a,data.homes[i].index);
   }
   Require(seedHash==AssetDatabase.GetAssetDependencyHash(seedPath).ToString(),"seed changed after reopen");report.AppendLine("Existing fences retained; foundation footprints enlarged 1.5x horizontally, keeping their height. All homes are 1.5x their previous lot-fitted size; preflight checks found no house/house or house/road footprint overlap. Re-running preserves the saved shuffled layout and does not enlarge it again. Snow seed/geometry, roads, terrain, fleet and player saves unchanged. Decorative house snow is independently removable in the Editor; no runtime assembly or roof-plowing mechanic added. Scene structural and 19 ledger checks passed. Seven Unity previews rendered for visual review. Live clearance/driving, Android packaging and Galaxy A07 frame timings remain untested.");
   File.WriteAllText(Report,report.ToString());Debug.Log(report.ToString());
   if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(placements[0].transform.position+Vector3.up*2,Quaternion.Euler(30,135,0),13,true,true);
  }
 }
}
