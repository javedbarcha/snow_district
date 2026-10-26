using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Snowglow.Editor {
 public static class CatalogChecks {
  const string Request="Logs/CatalogChecks.request";static int count;
  static void Check(bool ok,string label){if(!ok)throw new InvalidOperationException("Catalog check failed: "+label);count++;}
  static void Refused(Action action,string label){bool refused=false;try{action();}catch(InvalidOperationException){refused=true;}Check(refused,label);}
  [InitializeOnLoadMethod]static void OnReload(){EditorApplication.update+=Pending;}
  static void Pending(){if(Application.isBatchMode||!File.Exists(Request)){EditorApplication.update-=Pending;return;}if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;EditorApplication.update-=Pending;Run();File.Delete(Request);}
  [MenuItem("Snowglow/Run asset workshop checks")]
  public static void Run(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
   Directory.CreateDirectory("Logs");File.WriteAllText("Logs/AssetWorkshopChecks.txt","RUNNING");count=0;CatalogPreparation.EnsureFolders();var library=CatalogPreparation.Library();
   // Recover only fixtures from an earlier interrupted check run.
   foreach(var stale in library.entries.Where(a=>a&&((a.provenance=="Automated fixture"&&a.displayName.StartsWith("Check ",StringComparison.Ordinal))||(a.id.StartsWith("check-assembly-",StringComparison.Ordinal)&&string.IsNullOrEmpty(a.creationSourcePath)&&!a.prefab))).ToArray()){
    string prefabPath=AssetDatabase.GetAssetPath(stale.prefab),entryPath=AssetDatabase.GetAssetPath(stale);library.entries.Remove(stale);if(!string.IsNullOrEmpty(prefabPath))AssetDatabase.DeleteAsset(prefabPath);AssetDatabase.DeleteAsset(entryPath);
   }EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
   foreach(string old in Directory.GetDirectories(CatalogPreparation.Root,"Check_*")){if(Guid.TryParseExact(Path.GetFileName(old).Substring(6),"N",out var ignored))AssetDatabase.DeleteAsset(old.Replace('\\','/'));}
   string fixture=CatalogPreparation.Root+"/Check_"+Guid.NewGuid().ToString("N");AssetDatabase.CreateFolder(CatalogPreparation.Root,Path.GetFileName(fixture));
   var created=new List<string>();var garbage=new List<UnityEngine.Object>();var extraPaths=new List<string>();
   try{
    CatalogBootstrap.ImportPrototype();library=CatalogPreparation.Library();
    var source=GameObject.CreatePrimitive(PrimitiveType.Cube);source.transform.localScale=new Vector3(2,1,3);
    var material=new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));AssetDatabase.CreateAsset(material,fixture+"/Material.asset");source.GetComponent<Renderer>().sharedMaterial=material;
    var prefab=PrefabUtility.SaveAsPrefabAsset(source,fixture+"/Source.prefab");UnityEngine.Object.DestroyImmediate(source);string original=AssetDatabase.GetAssetDependencyHash(fixture+"/Source.prefab").ToString();
    var entry=CatalogPreparation.Prepare(prefab,CatalogKind.BuildingAssembly,"Check building",1,Vector3.zero,AssetSource.ImportedModel,"Automated fixture");created.Add(entry.id);
    Check(original==AssetDatabase.GetAssetDependencyHash(fixture+"/Source.prefab").ToString(),"source model preserved");
    Check(entry.status==PreparationStatus.NeedsReview&&entry.prefab.transform.localScale==Vector3.one,"prepared root and review state");
    Check(entry.prefab.GetComponentsInChildren<Collider>(true).Length==1&&CatalogValidation.Check(entry).Ok,"generated static collision proxy");
    Refused(()=>CatalogPreparation.Publish(entry),"unreviewed assets cannot publish");entry.visualReviewed=true;CatalogPreparation.Publish(entry);Check(CatalogValidation.Check(entry,true).Ok,"reviewed asset publishes");
    var draft=CatalogPreparation.Draft("Check draft",CatalogKind.BuildingAssembly,AssetSource.BlenderScript,prefab,"Automated fixture");created.Add(draft.id);string draftId=draft.id;
    Check(draft.status==PreparationStatus.Draft&&!draft.prefab,"source-only draft retained without claiming readiness");
    CatalogPreparation.Prepare(prefab,CatalogKind.BuildingAssembly,"Check completed draft",1,Vector3.zero,AssetSource.BlenderScript,"Automated fixture",draft);
    Check(draft.id==draftId&&draft.creationSourcePath==fixture+"/Source.prefab"&&draft.status==PreparationStatus.NeedsReview,"draft completion preserves identity and provenance");
    var standardShader=Shader.Find("Standard");if(standardShader){
     var standard=new Material(standardShader);AssetDatabase.CreateAsset(standard,fixture+"/Standard.asset");var visual=UnityEngine.Object.Instantiate(prefab);visual.GetComponent<Renderer>().sharedMaterial=standard;var standardSource=PrefabUtility.SaveAsPrefabAsset(visual,fixture+"/StandardSource.prefab");UnityEngine.Object.DestroyImmediate(visual);
     var converted=CatalogPreparation.Prepare(standardSource,CatalogKind.BuildingAssembly,"Check converted material",1,Vector3.zero,AssetSource.ImportedModel,"Automated fixture");created.Add(converted.id);
     Check(converted.prefab.GetComponentInChildren<Renderer>().sharedMaterial.shader.name=="Universal Render Pipeline/Simple Lit"&&standard.shader==standardShader,"URP conversion copies rather than edits source material");
    }
    var testCatalog=ScriptableObject.CreateInstance<AssetCatalog>();garbage.Add(testCatalog);testCatalog.entries.Add(entry);
    var plan=new CatalogScenePlan{terrain=false,placements=new[]{new PlanPlacement{id="one",assetId=entry.id,position=Vector3.zero,scale=1},new PlanPlacement{id="two",assetId=entry.id,position=new Vector3(10,0,0),scale=1}}};
    Check(CatalogScenePlans.Resolve(testCatalog,JsonUtility.ToJson(plan)).Count==2,"valid scene plan resolves");
    plan.placements[1].position=Vector3.zero;Refused(()=>CatalogScenePlans.Resolve(testCatalog,JsonUtility.ToJson(plan)),"solid footprint overlap rejected");
    plan.placements[1].position=new Vector3(600,0,0);Refused(()=>CatalogScenePlans.Resolve(testCatalog,JsonUtility.ToJson(plan)),"district bounds enforced");
    plan.placements[1].position=new Vector3(10,0,0);plan.placements[1].assetId="missing";Refused(()=>CatalogScenePlans.Resolve(testCatalog,JsonUtility.ToJson(plan)),"unknown asset rejected");
    plan.placements[1].assetId=entry.id;plan.placements[1].scale=-1;Refused(()=>CatalogScenePlans.Resolve(testCatalog,JsonUtility.ToJson(plan)),"negative scale rejected");
    plan.placements[1].scale=1;entry.status=PreparationStatus.NeedsReview;Refused(()=>CatalogScenePlans.Resolve(testCatalog,JsonUtility.ToJson(plan)),"AI cannot place unapproved entries");entry.status=PreparationStatus.Ready;
    testCatalog.entries.Add(entry);Refused(()=>CatalogScenePlans.Manifest(testCatalog),"duplicate catalog IDs rejected");testCatalog.entries.RemoveAt(1);
    Check(CatalogScenePlans.Manifest(testCatalog).Contains(entry.id),"Ready manifest contains stable IDs");
    entry.maximumScale=2;Check(!CatalogValidation.Check(entry,true).Ok,"changed constraints invalidate approval");entry.maximumScale=1;
    Check(!CatalogValidation.Finite(new Vector3(float.NaN,0,0)),"non-finite transforms rejected");
    Bounds approvedBounds=entry.localBounds;entry.localBounds=new Bounds(new Vector3(float.NaN,0,0),Vector3.one);Check(!CatalogValidation.Check(entry,true).Ok,"non-finite stored footprints rejected");entry.localBounds=approvedBounds;
    var road=CatalogPreparation.Prepare(prefab,CatalogKind.RoadAssembly,"Check road",1,Vector3.zero,AssetSource.ImportedModel,"Automated fixture");created.Add(road.id);road.visualReviewed=true;CatalogPreparation.Publish(road);testCatalog.entries.Add(road);
    var roads=new CatalogScenePlan{terrain=false,placements=new[]{new PlanPlacement{id="road1",assetId=road.id,scale=1},new PlanPlacement{id="road2",assetId=road.id,scale=1,connectTo="road1",ownAnchor="road-a",targetAnchor="road-b"}}};
    var resolved=CatalogScenePlans.Resolve(testCatalog,JsonUtility.ToJson(roads));Check(Mathf.Abs(resolved[1].position.z-3)<.001f,"road anchors align automatically");
    var roadContents=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(road.prefab));try{roadContents.GetComponentsInChildren<AssetAnchor>()[0].transform.localPosition+=Vector3.right*.1f;PrefabUtility.SaveAsPrefabAsset(roadContents,AssetDatabase.GetAssetPath(road.prefab));}finally{PrefabUtility.UnloadPrefabContents(roadContents);}
    Check(!CatalogValidation.Check(road,true).Ok,"edited prefab invalidates approval");CatalogPreparation.Publish(road);
    roads.placements[0].connectTo="road2";roads.placements[0].ownAnchor="road-a";roads.placements[0].targetAnchor="road-b";Refused(()=>CatalogScenePlans.Resolve(testCatalog,JsonUtility.ToJson(roads)),"connection cycles rejected");
    var vehicle=UnityEngine.Object.Instantiate(library.Find("prototype-plow-body"));var part=UnityEngine.Object.Instantiate(library.Find("prototype-plow-attachment"));garbage.Add(vehicle);garbage.Add(part);
    vehicle.visualReviewed=part.visualReviewed=true;vehicle.status=part.status=PreparationStatus.Ready;vehicle.reviewedPrefabHash=CatalogValidation.Fingerprint(vehicle);part.reviewedPrefabHash=CatalogValidation.Fingerprint(part);
    var fitting=ScriptableObject.CreateInstance<FittingProfile>();garbage.Add(fitting);fitting.vehicle=vehicle;fitting.part=part;fitting.socketId="front";fitting.fitReviewed=true;fitting.reviewedVehicleHash=vehicle.reviewedPrefabHash;fitting.reviewedPartHash=part.reviewedPrefabHash;
    CatalogPreparation.ApproveFitting(fitting);Check(CatalogValidation.CheckFit(fitting).Ok,"supported plow fitting validates");
    fitting.positionOffset=Vector3.right*.01f;Check(!CatalogValidation.CheckFit(fitting).Ok,"edited fitting transforms invalidate approval");fitting.positionOffset=Vector3.zero;CatalogPreparation.ApproveFitting(fitting);
    var badEquipment=UnityEngine.Object.Instantiate(part.attachment);garbage.Add(badEquipment);part.attachment=badEquipment;badEquipment.mountId="wrong";Check(!CatalogValidation.CheckFit(fitting,false).Ok,"incompatible mount rejected");badEquipment.mountId=vehicle.vehicle.mountId;badEquipment.mass=9999;Check(!CatalogValidation.CheckFit(fitting,false).Ok,"overweight equipment rejected");Check(!CatalogValidation.Check(part,true).Ok,"changed physics invalidate approval");part.attachment=library.Find("prototype-plow-attachment").attachment;
    var recipe=ScriptableObject.CreateInstance<AssemblyRecipe>();garbage.Add(recipe);recipe.id="check-assembly-"+Guid.NewGuid().ToString("N");recipe.displayName="Check assembly";recipe.vehicle=vehicle;recipe.fittings.Add(fitting);
    var preview=CatalogAssembly.Preview(recipe,true);var slot=preview.GetComponentsInChildren<AssetSocket>().Single(s=>s.id=="front");var connection=slot.GetComponentsInChildren<AssetAnchor>().Single(a=>a.role==AnchorRole.Connection);Check(Vector3.Distance(slot.transform.position,connection.transform.position)<.0001f,"part connection snaps to socket");UnityEngine.Object.DestroyImmediate(preview);
    var assembled=CatalogAssembly.Bake(recipe);extraPaths.Add(AssetDatabase.GetAssetPath(assembled));var machine=assembled.GetComponent<MachineController>();extraPaths.Add(AssetDatabase.GetAssetPath(machine.definition));
    Check(machine&&machine.Tool&&machine.Tool.useAuthoredRig&&machine.Tool.mouth&&machine.Tool.spray&&machine.equipment.Validate(machine.definition)==null,"drivable assembly saves complete bindings");
    var registered=CatalogBootstrap.RegisterAssembled(recipe);created.Add(registered.id);Check(registered.status==PreparationStatus.NeedsReview&&registered.prefab==assembled,"baked assembly registers for explicit review");
    var wheel=UnityEngine.Object.Instantiate(library.Find("prototype-wheel-set"));garbage.Add(wheel);wheel.visualReviewed=true;wheel.status=PreparationStatus.Ready;wheel.wheelMass+=10;wheel.gripMultiplier=1.15f;wheel.reviewedPrefabHash=CatalogValidation.Fingerprint(wheel);
    var wheelFit=ScriptableObject.CreateInstance<FittingProfile>();garbage.Add(wheelFit);wheelFit.vehicle=vehicle;wheelFit.part=wheel;wheelFit.socketId="wheels";wheelFit.fitReviewed=true;wheelFit.reviewedVehicleHash=vehicle.reviewedPrefabHash;wheelFit.reviewedPartHash=wheel.reviewedPrefabHash;
    CatalogPreparation.ApproveFitting(wheelFit);Check(CatalogValidation.CheckFit(wheelFit).Ok,"wheel set fits its rated socket");float radius=wheel.wheelRadius;wheel.wheelRadius=99;Check(!CatalogValidation.CheckFit(wheelFit,false).Ok,"wheel clearance enforced");wheel.wheelRadius=radius;
    recipe.fittings.Add(wheelFit);var upgraded=CatalogAssembly.Bake(recipe);extraPaths.Add(AssetDatabase.GetAssetPath(upgraded));var upgradedMachine=upgraded.GetComponent<MachineController>();extraPaths.Add(AssetDatabase.GetAssetPath(upgradedMachine.definition));
    Check(upgradedMachine.definition.mass>vehicle.vehicle.mass&&Mathf.Approximately(upgradedMachine.definition.baseGrip,1.15f)&&Mathf.Approximately(upgradedMachine.wheels[0].radius,wheel.wheelRadius),"wheel set updates physics and visual rig together");
    Check(CatalogBootstrap.RegisterAssembled(recipe).prefab==upgraded&&registered.id==recipe.id,"rebaking updates catalog output without changing identity");
    var loader=UnityEngine.Object.Instantiate(library.Find("prototype-loader-body"));var bucket=UnityEngine.Object.Instantiate(library.Find("prototype-loader-attachment"));garbage.Add(loader);garbage.Add(bucket);loader.visualReviewed=bucket.visualReviewed=true;loader.status=bucket.status=PreparationStatus.Ready;loader.reviewedPrefabHash=CatalogValidation.Fingerprint(loader);bucket.reviewedPrefabHash=CatalogValidation.Fingerprint(bucket);
    var loaderFit=ScriptableObject.CreateInstance<FittingProfile>();garbage.Add(loaderFit);loaderFit.vehicle=loader;loaderFit.part=bucket;loaderFit.socketId="front";CatalogPreparation.ApproveFitting(loaderFit);Check(CatalogValidation.CheckFit(loaderFit).Ok,"loader full-load fitting validates");
    var loaderRecipe=ScriptableObject.CreateInstance<AssemblyRecipe>();garbage.Add(loaderRecipe);loaderRecipe.id="check-loader-"+Guid.NewGuid().ToString("N");loaderRecipe.displayName="Check loader";loaderRecipe.vehicle=loader;loaderRecipe.fittings.Add(loaderFit);
    var loaderPrefab=CatalogAssembly.Bake(loaderRecipe);extraPaths.Add(AssetDatabase.GetAssetPath(loaderPrefab));var loaderMachine=loaderPrefab.GetComponent<MachineController>();extraPaths.Add(AssetDatabase.GetAssetPath(loaderMachine.definition));
    Check(loaderMachine.definition.loader&&loaderMachine.Tool.boom&&loaderMachine.Tool.bucket&&loaderMachine.Tool.loadVisual&&!loaderMachine.Tool.loadVisual.gameObject.activeSelf,"loader assembly retains articulation and preauthored load visual");
    Check(Mathf.Approximately(library.Find("prototype-plow-body").vehicle.baseGrip,1)&&library.Find("prototype-plow-body").vehicle.dryCenterOfMass==new Vector3(0,-.24f,0),"legacy vehicle physics defaults preserved");
    // Build and reopen a disposable authored layout, then restore Pinefall.
    string output="Assets/Scenes/WorkshopCheck_"+Guid.NewGuid().ToString("N")+".unity";extraPaths.Add(output);
    var scene=CatalogScenePlans.Build(testCatalog,JsonUtility.ToJson(plan),output);Check(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CatalogPlacement>()).Count()==2,"scene instances authored before Play");
    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(output);Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CatalogPlacement>()).Count()==2,"layout reopens without runtime generation");
    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SceneValidation.ScenePath);SceneValidation.Validate(UnityEngine.SceneManagement.SceneManager.GetActiveScene());MechanicsChecks.Run();
   }catch(Exception e){File.WriteAllText("Logs/AssetWorkshopChecks.txt","FAIL: "+e);throw;}
   finally{
    if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=SceneValidation.ScenePath)UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SceneValidation.ScenePath);
    library=CatalogPreparation.Library();
    foreach(string id in created){library.entries.RemoveAll(a=>!a||a.id==id);AssetDatabase.DeleteAsset(CatalogPreparation.Root+"/Prefabs/"+id+".prefab");AssetDatabase.DeleteAsset(CatalogPreparation.Root+"/Entries/"+id+".asset");foreach(string path in Directory.GetFiles(CatalogPreparation.Root+"/Entries",id+"-material*.asset"))AssetDatabase.DeleteAsset(path.Replace('\\','/'));}
    foreach(string path in extraPaths)if(!string.IsNullOrEmpty(path))AssetDatabase.DeleteAsset(path);
    foreach(var o in garbage)if(o)UnityEngine.Object.DestroyImmediate(o);
    EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();AssetDatabase.DeleteAsset(fixture);
   }
   File.WriteAllText("Logs/AssetWorkshopChecks.txt","PASS: Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+"; "+count+" asset preparation, fitting, approval and scene-plan checks including fixture cleanup. Pinefall validation and 19 snow checks also passed. Imported commercial models, visual fit, live handling and phone profiling remain untested.");
   Debug.Log("Snowglow: "+count+" Asset Workshop checks passed, fixtures cleaned.");
  }
 }
}
// Editor-only acceptance runner; fixtures are cleaned before PASS is written.
// Acceptance includes identity-preserving assembly registration.
// Particle effects are excluded from solid footprint measurement.
