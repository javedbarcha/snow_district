using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Snowglow.Editor {
 public static class CatalogBootstrap {
  const string Request="Logs/InitializeCatalog.request";
  public static CatalogAsset RegisterAssembled(AssemblyRecipe recipe){
   if(!recipe||!recipe.bakedPrefab||!recipe.bakedPrefab.GetComponent<MachineController>())throw new InvalidOperationException("Bake a complete assembly first.");
   var library=CatalogPreparation.Library();var existing=library.Find(recipe.id);string recipePath=AssetDatabase.GetAssetPath(recipe);
   if(existing&&existing.creationSourcePath!=recipePath)throw new InvalidOperationException("Recipe ID is already used by another catalog entry.");
   var a=ScriptableObject.CreateInstance<CatalogAsset>();a.id=recipe.id;
   try{
    a.kind=CatalogKind.Vehicle;a.displayName=recipe.displayName;a.prefab=recipe.bakedPrefab;a.vehicle=recipe.bakedPrefab.GetComponent<MachineController>().definition;a.sourceType=AssetSource.ExistingPrototype;a.sourcePath=AssetDatabase.GetAssetPath(recipe.bakedPrefab);a.creationSourcePath=recipePath;a.provenance="Baked from an authored assembly recipe.";a.status=PreparationStatus.NeedsReview;a.visualReviewed=false;a.blocksSnow=false;CatalogPreparation.RecordMetrics(a);
    if(existing){EditorUtility.CopySerialized(a,existing);UnityEngine.Object.DestroyImmediate(a);a=existing;EditorUtility.SetDirty(a);}else{AssetDatabase.CreateAsset(a,CatalogPreparation.Root+"/Entries/"+a.id+".asset");library.entries.Add(a);}
    EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();return a;
   }catch{if(a&&!AssetDatabase.Contains(a))UnityEngine.Object.DestroyImmediate(a);throw;}
  }
  [InitializeOnLoadMethod]static void OnReload(){EditorApplication.update+=Pending;}
  static void Pending(){if(Application.isBatchMode||!File.Exists(Request)){EditorApplication.update-=Pending;return;}if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)return;EditorApplication.update-=Pending;if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty){Debug.Log("Snowglow: save the scene, then use Asset Workshop > Import prototype building blocks.");return;}ImportPrototype();File.Delete(Request);}
  public static CatalogAsset Register(GameObject source,CatalogKind kind,string title,string id,bool bodyOnly=false,AttachmentDefinition equipment=null){
   var library=CatalogPreparation.Library();var existing=library.Find(id);if(existing)return existing;
   var a=ScriptableObject.CreateInstance<CatalogAsset>();a.id=id;a.displayName=title;a.kind=kind;a.status=PreparationStatus.NeedsReview;a.sourceType=AssetSource.ExistingPrototype;a.sourcePath=AssetDatabase.GetAssetPath(source);a.sourceGuid=AssetDatabase.AssetPathToGUID(a.sourcePath);a.provenance="Extracted from the existing authored primitive prototype. Source preserved.";a.assumptions="Review pivots, footprint, clearance and gameplay use before publishing.";a.blocksSnow=kind>=CatalogKind.BuildingAssembly;a.attachment=equipment;
   var root=UnityEngine.Object.Instantiate(source);root.name=title;root.transform.position=Vector3.zero;
   if(kind!=CatalogKind.Vehicle){var wrapper=new GameObject(title);root.transform.SetParent(wrapper.transform,true);root=wrapper;}
   try{
    var placement=root.GetComponent<CatalogPlacement>()??root.AddComponent<CatalogPlacement>();placement.assetId=id;placement.placementId="";
    var m=root.GetComponent<MachineController>();if(m){
     a.vehicle=m.definition;var rig=root.GetComponent<PreparedVehicleRig>()??root.AddComponent<PreparedVehicleRig>();rig.wheels=m.wheels;rig.wheelVisuals=m.wheelVisuals;rig.visualRoot=root.transform.Find("VisualRoot | replace body meshes here");rig.maximumWheelRadius=m.definition.wheelRadius*1.15f;rig.maximumWheelWidth=.6f;
     if(m.Tool){
      var oldTool=m.Tool;rig.boom=oldTool.boom;
      if(bodyOnly){
       Vector3 pos=root.transform.InverseTransformPoint(oldTool.mount.position);Transform slotParent=root.transform;
       if(m.definition.loader){pos=oldTool.bucket.localPosition;slotParent=oldTool.boom;UnityEngine.Object.DestroyImmediate(oldTool.bucket.gameObject);}
       else UnityEngine.Object.DestroyImmediate(oldTool.mount.gameObject);
       var slot=new GameObject("Socket | front").AddComponent<AssetSocket>();slot.transform.SetParent(slotParent,false);slot.transform.localPosition=pos;slot.id="front";slot.interfaceId=m.definition.mountId;slot.maximumMass=m.definition.maxAttachmentMass;slot.maximumWidth=m.definition.maxWorkingWidth;
       if(oldTool.spray)UnityEngine.Object.DestroyImmediate(oldTool.spray.gameObject);UnityEngine.Object.DestroyImmediate(oldTool);UnityEngine.Object.DestroyImmediate(m);
      }
     }
     if(!root.GetComponentsInChildren<AssetSocket>(true).Any(s=>s.id=="wheels")){var s=new GameObject("Socket | wheels").AddComponent<AssetSocket>();s.transform.SetParent(root.transform,false);s.id="wheels";s.interfaceId="wheel-standard";s.accepts=CatalogKind.WheelSet;s.maximumWidth=2;}
    }
    a.prefab=PrefabUtility.SaveAsPrefabAsset(root,AssetDatabase.GenerateUniqueAssetPath(CatalogPreparation.Root+"/Prefabs/"+id+".prefab"));CatalogPreparation.RecordMetrics(a);AssetDatabase.CreateAsset(a,CatalogPreparation.Root+"/Entries/"+id+".asset");library.entries.Add(a);EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();return a;
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  [MenuItem("Snowglow/Import prototype into catalog")]
  public static void ImportPrototype(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
   if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=SceneValidation.ScenePath)scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath,OpenSceneMode.Single);
   var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
   var session=all.Select(t=>t.GetComponent<PrototypeSession>()).FirstOrDefault(s=>s);if(!session)throw new InvalidOperationException("Open the baked Pinefall scene first.");
   foreach(var m in session.Fleet){
    string type=m.definition.loader?"loader":"plow";Register(m.gameObject,CatalogKind.Vehicle,m.definition.displayName+" body","prototype-"+type+"-body",true);
    if(!m.definition.loader&&!CatalogPreparation.Library().Find("prototype-wheel-set")){var wheels=Register(m.wheelVisuals[0].gameObject,CatalogKind.WheelSet,"Municipal wheel set","prototype-wheel-set");wheels.interfaceId="wheel-standard";wheels.wheelRadius=m.definition.wheelRadius;wheels.wheelWidth=wheels.localBounds.size.x;wheels.wheelMass=m.wheels[0].mass;EditorUtility.SetDirty(wheels);}
    Transform original=m.definition.loader?m.Tool.bucket:m.Tool.mount;var part=UnityEngine.Object.Instantiate(original.gameObject);part.transform.position=Vector3.zero;part.transform.rotation=Quaternion.identity;
    try{
     string mouthPath=AnimationUtility.CalculateTransformPath(m.Tool.mouth,original);var mouth=part.transform.Find(mouthPath);var edge=mouth.gameObject.AddComponent<AssetAnchor>();edge.id="working-edge";edge.role=AnchorRole.WorkingEdge;
     CatalogPreparation.Anchor(part.transform,"connection",AnchorRole.Connection,Vector3.zero);
     if(m.definition.loader){var load=part.transform.Find(AnimationUtility.CalculateTransformPath(m.Tool.loadVisual,original));var marker=load.gameObject.AddComponent<AssetAnchor>();marker.id="load";marker.role=AnchorRole.LoadVisual;}
     Register(part,CatalogKind.Attachment,m.equipment.displayName,"prototype-"+type+"-attachment",false,m.equipment);
    }finally{UnityEngine.Object.DestroyImmediate(part);}
   }
   var house=all.FirstOrDefault(t=>t.name.StartsWith("Cottage |",StringComparison.Ordinal));if(house)Register(house.gameObject,CatalogKind.BuildingAssembly,"Cottage and fence lot","prototype-cottage-lot");
   var tree=all.FirstOrDefault(t=>t.name.StartsWith("Snow tree |",StringComparison.Ordinal));if(tree)Register(tree.gameObject,CatalogKind.Landscape,"Snow tree assembly","prototype-snow-tree");
   var depot=all.FirstOrDefault(t=>t.name=="Fleet workshop");if(depot)Register(depot.gameObject,CatalogKind.BuildingAssembly,"Service depot assembly","prototype-depot");
   var road=new GameObject("Reusable 16 m road");try{
    PrimitiveKit.Cube("Road",road.transform,new Vector3(0,.09f,0),new Vector3(10,.18f,16),session.Snow.palette.asphalt,true);
    var start=CatalogPreparation.Anchor(road.transform,"road-a",AnchorRole.RoadConnection,new Vector3(0,.18f,-8));start.interfaceId="road-10";
    var end=CatalogPreparation.Anchor(road.transform,"road-b",AnchorRole.RoadConnection,new Vector3(0,.18f,8));end.interfaceId="road-10";end.transform.localRotation=Quaternion.Euler(0,180,0);
    Register(road,CatalogKind.RoadAssembly,"16 m two-lane road","prototype-road-16m");
   }finally{UnityEngine.Object.DestroyImmediate(road);}
   var bucket=CatalogPreparation.Library().Find("prototype-loader-attachment");if(bucket){bucket.interfaceId=bucket.attachment.mountId;EditorUtility.SetDirty(bucket);}
   var blade=CatalogPreparation.Library().Find("prototype-plow-attachment");if(blade){blade.interfaceId=blade.attachment.mountId;EditorUtility.SetDirty(blade);}
   AssetDatabase.SaveAssets();Debug.Log("Snowglow catalog imported. Entries remain Needs Review until explicit approval. Pinefall objects were not replaced.");
  }
 }
}
