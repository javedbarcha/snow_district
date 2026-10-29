using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Collections;

namespace Snowglow.Editor {
 public static class AlpineMountainsIntake {
  const string Source=CatalogPreparation.Root+"/Sources/FiveAlpineMountains/";
  const string Request="Logs/ImportAlpineMountains.request",Report="Logs/AlpineMountains/UnityIntake.txt";
  const string Hash="382B0050FB4E9ACB40D17B4904C4B35950DCF2400A42D0F5863F47574CFCBF73";
  [Serializable]public class Export {public Mountain[] mountains;}
  [Serializable]public class Mountain {public string index,name,model;public float[] size;public int triangles;}
  static void Require(bool value,string message){if(!value)throw new InvalidOperationException("Alpine mountains: "+message);}
  static Transform[] Transforms(Scene scene)=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>()).ToArray();
  static CatalogPlacement[] Placements(Scene scene)=>Transforms(scene).Select(t=>t.GetComponent<CatalogPlacement>()).Where(p=>p&&(p.placementId??"").StartsWith("pinefall-mountain-",StringComparison.Ordinal)).ToArray();
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){
   if(!File.Exists(Request)){EditorApplication.update-=Pending;return;}
   if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
   EditorApplication.update-=Pending;File.Delete(Request);
   try{Install();}catch(Exception e){Directory.CreateDirectory("Logs/AlpineMountains");File.WriteAllText(Report,"FAIL: "+e);Debug.LogException(e);}
  }
  static Material Rock(){
   var material=AssetDatabase.LoadAssetAtPath<Material>(Source+"Rock.mat");
   if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));AssetDatabase.CreateAsset(material,Source+"Rock.mat");}
   material.name="Alpine mountains | opaque rock palette";material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"Palette.png"));material.SetColor("_BaseColor",Color.white);material.SetColor("_SpecColor",Color.black);material.SetFloat("_Smoothness",0);material.SetFloat("_Surface",0);material.SetFloat("_ZWrite",1);material.SetFloat("_Cull",2);material.renderQueue=2000;material.enableInstancing=true;EditorUtility.SetDirty(material);return material;
  }
  static CatalogAsset Prepare(Mountain mountain,Material snow){
   string id="alpine-mountain-"+mountain.index;var entry=CatalogPreparation.Library().Find(id);
   if(entry&&entry.tags.Contains("alpine-mountain")){CatalogPreparation.RecordMetrics(entry);Require(entry.triangles==mountain.triangles,"changed prepared geometry: "+id);Require(CatalogValidation.Check(entry).Ok,"catalog validation: "+id);return entry;}
   string path=Source+mountain.model+".fbx";var importer=AssetImporter.GetAtPath(path) as ModelImporter;Require(importer!=null,"FBX not imported: "+path);
   importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;importer.globalScale=1;importer.isReadable=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
   importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"AlpineRockPalette"),Rock());importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"AlpineSnow"),snow);importer.SaveAndReimport();
   var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);var bounds=CatalogValidation.BoundsOf(model);Require((bounds.size-new Vector3(mountain.size[0],mountain.size[1],mountain.size[2])).magnitude<.1f,"metre scale/axes mismatch: "+id);
   if(!entry){
    var draft=CatalogPreparation.Draft(mountain.name,CatalogKind.Landscape,AssetSource.ImportedModel,null,"User supplied Five_Alpine_Mountains.blend; collection "+mountain.index+" "+mountain.name+"; SHA256 "+Hash+". Embedded text was not executed.");
    draft.id=id;draft.creationSourcePath="SourceAssets/FiveAlpineMountains/Five_Alpine_Mountains.blend";
    entry=CatalogPreparation.Prepare(model,CatalogKind.Landscape,mountain.name,1,Vector3.zero,AssetSource.ImportedModel,draft.provenance,draft);
   }
   var root=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(entry.prefab));
   try{
    foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))renderer.sharedMaterial=renderer.name=="SnowCap"?snow:Rock();
    PrefabUtility.SaveAsPrefabAsset(root,AssetDatabase.GetAssetPath(entry.prefab));
   }finally{PrefabUtility.UnloadPrefabContents(root);}
   entry.tags=new[]{"alpine-mountain","background-landscape","removable-snow",mountain.model.ToLowerInvariant()};entry.minimumScale=.25f;entry.maximumScale=12;entry.status=PreparationStatus.NeedsReview;entry.visualReviewed=false;
   entry.assumptions="Metres, Y-up, centred ground pivot. Two opaque renderers with shared rock palette and existing toon snow. Separate SnowCap remains removable. Source material node graphs are approximated for URP; original preserved. Box collision proxy is conservative background scenery, not a climbable mountain surface. Snow cap is decorative and outside the plowing ledger.";
   entry.reviewNotes="Authorized background replacement. Review terrain seating, sightlines and device rendering before Ready publication.";
   CatalogPreparation.RecordMetrics(entry);Require(entry.triangles==mountain.triangles,"export/import triangle count: "+id);Require(entry.renderers==2&&entry.materialSlots==2,"two mesh groups required");Require(CatalogValidation.Check(entry).Ok,"catalog validation: "+id);EditorUtility.SetDirty(entry);return entry;
  }
  static void Place(Scene scene,CatalogAsset[] entries){
   var originals=Transforms(scene).Where(t=>t.name=="Rounded mountain | sphere").OrderBy(t=>Mathf.Atan2(t.position.x,t.position.z)).ToArray();var installed=Placements(scene);
   if(originals.Length==0){Require(installed.Length>=5&&entries.All(a=>installed.Any(p=>p.assetId==a.id)),"incomplete installed mountain set");return;}
   Require(installed.Length==0&&originals.Length>=5,"partial mountain replacement requires review");
   var terrain=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Terrain>()).Single();
   var bag=Enumerable.Range(0,originals.Length).Select(i=>i%entries.Length).ToArray();var rng=new System.Random(291029);
   for(int i=bag.Length-1;i>0;i--){int j=rng.Next(i+1);int swap=bag[i];bag[i]=bag[j];bag[j]=swap;}
   var positions=new Vector3[originals.Length];var scales=new float[originals.Length];var angles=new float[originals.Length];
   for(int i=0;i<originals.Length;i++){
    var old=originals[i].GetComponent<Renderer>().bounds;var size=entries[bag[i]].localBounds.size;angles[i]=(float)rng.NextDouble()*360;
    float c=Mathf.Abs(Mathf.Cos(angles[i]*Mathf.Deg2Rad)),s=Mathf.Abs(Mathf.Sin(angles[i]*Mathf.Deg2Rad));
    scales[i]=Mathf.Min(old.size.x/(size.x*c+size.z*s),old.size.z/(size.x*s+size.z*c));Require(scales[i]>=entries[bag[i]].minimumScale&&scales[i]<=entries[bag[i]].maximumScale,"placement scale outside range");
    // Seat the whole flat base below the lowest sampled terrain height in the
    // existing footprint. Keep source proportions instead of stretching peaks.
    float ground=float.PositiveInfinity;
    for(int x=0;x<=8;x++)for(int z=0;z<=8;z++){
     var sample=new Vector3(Mathf.Lerp(old.min.x,old.max.x,x/8f),0,Mathf.Lerp(old.min.z,old.max.z,z/8f));ground=Mathf.Min(ground,terrain.SampleHeight(sample)+terrain.transform.position.y);
    }
    positions[i]=new Vector3(old.center.x,ground-2,old.center.z);Require(Mathf.Max(Mathf.Abs(old.center.x),Mathf.Abs(old.center.z))-Mathf.Max(old.extents.x,old.extents.z)>128,"mountain intrudes into interactive snow field");
   }
   for(int i=0;i<originals.Length;i++){
    var slot=originals[i];var a=entries[bag[i]];
    foreach(var child in slot.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
    foreach(var component in slot.GetComponents<Component>())if(!(component is Transform))UnityEngine.Object.DestroyImmediate(component);
    slot.name="Mountain lot | "+a.displayName;slot.SetPositionAndRotation(positions[i],Quaternion.identity);slot.localScale=Vector3.one;
    var placed=(GameObject)PrefabUtility.InstantiatePrefab(a.prefab,scene);placed.transform.SetParent(slot,false);placed.transform.localRotation=Quaternion.Euler(0,angles[i],0);placed.transform.localScale=Vector3.one*scales[i];placed.GetComponent<CatalogPlacement>().placementId="pinefall-mountain-"+(i+1).ToString("D2");
   }
  }
  public static void PlacePreparedMountains(Scene scene){
   if(!File.Exists(Source+"ExportMetrics.json"))return;var data=JsonUtility.FromJson<Export>(File.ReadAllText(Source+"ExportMetrics.json"));var entries=data.mountains.Select(m=>CatalogPreparation.Library().Find("alpine-mountain-"+m.index)).ToArray();
   if(entries.Length==5&&entries.All(a=>a&&a.prefab)){Place(scene,entries);AlpineTerrainFitting.Fit(scene,entries);}
  }
  static void Preview(CatalogAsset entry,string index){
   var preview=new PreviewRenderUtility();try{
    var obj=UnityEngine.Object.Instantiate(entry.prefab);preview.AddSingleGO(obj);var b=CatalogValidation.BoundsOf(obj);
    preview.camera.orthographic=true;preview.camera.orthographicSize=Mathf.Max(b.extents.y,b.extents.x)*1.35f;preview.camera.nearClipPlane=.1f;preview.camera.farClipPlane=200;
    preview.camera.transform.position=b.center+new Vector3(-1,.65f,1)*b.size.magnitude;preview.camera.transform.LookAt(b.center);preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.35f,.52f,.64f);
    preview.ambientColor=new Color(.35f,.35f,.35f);preview.lights[0].intensity=1.1f;preview.lights[0].transform.rotation=preview.camera.transform.rotation;preview.lights[1].intensity=.4f;
    preview.BeginStaticPreview(new Rect(0,0,720,600));preview.Render(true);var image=preview.EndStaticPreview();File.WriteAllBytes("Logs/AlpineMountains/"+index+"-unity.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
   }finally{preview.Cleanup();}
  }
  static string Homes(Scene scene)=>string.Join("|",Transforms(scene).Select(t=>t.GetComponent<CatalogPlacement>()).Where(p=>p&&(p.placementId??"").StartsWith("pinefall-home-",StringComparison.Ordinal)).OrderBy(p=>p.placementId).Select(p=>p.placementId+":"+p.assetId+":"+p.transform.localToWorldMatrix.ToString("R")));
  static float SnowRelief(CatalogPlacement placement,Terrain terrain){
   var filter=placement.GetComponentsInChildren<MeshFilter>(true).Single(m=>m.name=="SnowCap");float relief=float.NegativeInfinity;
   // Editor readback works with CPU readability disabled on the player asset.
   using(var data=MeshUtility.AcquireReadOnlyMeshData(filter.sharedMesh))
   using(var vertices=new NativeArray<Vector3>(data[0].vertexCount,Allocator.Temp)){
    data[0].GetVertices(vertices);
    foreach(var vertex in vertices){var world=filter.transform.TransformPoint(vertex);relief=Mathf.Max(relief,world.y-terrain.SampleHeight(world)-terrain.transform.position.y);}
   }
   return relief;
  }
  [MenuItem("Snowglow/Asset intake/Import five alpine mountains")]
  public static void Install(){
   Require(!EditorApplication.isPlayingOrWillChangePlaymode,"stop Play first");var scene=SceneManager.GetActiveScene();Require(scene.path==SceneValidation.ScenePath&&!scene.isDirty,"open and save Pinefall first");Directory.CreateDirectory("Logs/AlpineMountains");
   if(!File.Exists("Logs/AlpineMountains/Pinefall-before.unity"))File.Copy(scene.path,"Logs/AlpineMountains/Pinefall-before.unity");if(!File.Exists("Logs/AlpineMountains/Pinefall-before-terrain-fit.unity"))File.Copy(scene.path,"Logs/AlpineMountains/Pinefall-before-terrain-fit.unity");
   var session=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PrototypeSession>()).Single();string seedPath=AssetDatabase.GetAssetPath(session.Snow.seed),seedHash=AssetDatabase.GetAssetDependencyHash(seedPath).ToString(),homes=Homes(scene);
   var fleet=session.Fleet.Select(m=>m.GetInstanceID()).ToArray();var texture=AssetImporter.GetAtPath(Source+"Palette.png") as TextureImporter;Require(texture!=null,"palette not imported");texture.sRGBTexture=true;texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Clamp;texture.mipmapEnabled=false;texture.textureCompression=TextureImporterCompression.Uncompressed;texture.SaveAndReimport();
   var data=JsonUtility.FromJson<Export>(File.ReadAllText(Source+"ExportMetrics.json"));Require(data.mountains.Length==5,"five source collections required");var entries=data.mountains.Select(m=>Prepare(m,session.Snow.palette.snow)).ToArray();AssetDatabase.SaveAssets();Place(scene,entries);AlpineTerrainFitting.Fit(scene,entries);
   Require(Homes(scene)==homes&&fleet.SequenceEqual(session.Fleet.Select(m=>m.GetInstanceID())),"homes or fleet altered");Require(seedHash==AssetDatabase.GetAssetDependencyHash(seedPath).ToString(),"snow seed changed");SceneValidation.Validate(scene);EditorSceneManager.MarkSceneDirty(scene);Require(EditorSceneManager.SaveScene(scene),"scene save failed");AssetDatabase.SaveAssets();
   scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);SceneValidation.Validate(scene);AlpineTerrainFitting.Validate(scene,entries);MechanicsChecks.Run();var placed=Placements(scene);Require(placed.Length>=5&&placed.Select(p=>p.placementId).Distinct().Count()==placed.Length&&entries.All(a=>placed.Any(p=>p.assetId==a.id)),"saved placements/variant coverage invalid");Require(!Transforms(scene).Any(t=>t.name=="Rounded mountain | sphere"||t.name=="Mountain snow cap"),"primitive mountain remains");Require(Homes(scene)==homes&&seedHash==AssetDatabase.GetAssetDependencyHash(seedPath).ToString(),"saved homes/snow changed");
   var report=new StringBuilder("PASS: Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+". All "+placed.Length+" primitive mountain locations replaced with five shuffled imported variants, saved and reopened.\n");
   for(int i=0;i<entries.Length;i++){
    var a=entries[i];Require(a.prefab.GetComponentsInChildren<Camera>(true).Length==0&&a.prefab.GetComponentsInChildren<Light>(true).Length==0,"presentation objects leaked");int vertices=a.prefab.GetComponentsInChildren<MeshFilter>(true).Sum(m=>m.sharedMesh.vertexCount);
    report.AppendLine(a.displayName+": "+a.triangles+" triangles / "+vertices+" imported vertices / "+a.renderers+" renderers / "+a.materialSlots+" slots / "+placed.Count(p=>p.assetId==a.id)+" placements; Needs Review.");Preview(a,data.mountains[i].index);
   }
   var terrain=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Terrain>()).Single();
   foreach(var p in placed){float relief=SnowRelief(p,terrain);Require(relief>2,"mountain snow cap buried in terrain: "+p.placementId);report.AppendLine(p.placementId+" "+p.assetId+" at "+p.transform.position+" scale "+p.transform.localScale.x.ToString("F3")+" yaw "+p.transform.eulerAngles.y.ToString("F1")+"; highest cap vertex "+relief.ToString("F2")+"m above terrain");}
   report.AppendLine("Original X/Z locations and footprint envelopes preserved; random yaw and uniform scaling retain source proportions. Persisted scene-specific mesh copies follow local Terrain heights, retaining source-relative height. Bases are buried at least 2m, with sampled base-edge clearance; under 15% of source height is buried. Shared catalog/source meshes remain unchanged. Separate snow caps use the shared white/cyan toon snow. Homes, fleet, snow seed/geometry and terrain assets preserved; no player save loaded/written, no district rebake. Structural scene validation and 19 snow-ledger checks passed. Live sightlines, terrain seam walkthrough, Android packaging and Galaxy A07 timings remain untested.");File.WriteAllText(Report,report.ToString());Debug.Log(report.ToString());
   if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(placed[0].transform.position+Vector3.up*40,Quaternion.Euler(25,135,0),110,true,true);
  }
 }
}
