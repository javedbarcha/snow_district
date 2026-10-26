using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Snowglow.Editor {
 public static class CatalogAssembly {
  public static void Align(GameObject part,Transform socket,FittingProfile profile){
   var connection=part.GetComponentsInChildren<AssetAnchor>(true).FirstOrDefault(a=>a.role==AnchorRole.Connection);
   Vector3 local=connection?part.transform.InverseTransformPoint(connection.transform.position):Vector3.zero;
   Quaternion rotation=connection?Quaternion.Inverse(part.transform.rotation)*connection.transform.rotation:Quaternion.identity;
   part.transform.SetParent(socket,false);part.transform.localScale=Vector3.one;
   part.transform.localRotation=Quaternion.Euler(profile.rotationOffset)*Quaternion.Inverse(rotation);
   part.transform.localPosition=profile.positionOffset-part.transform.localRotation*local;
  }
  public static GameObject Preview(AssemblyRecipe recipe,bool validate){
   if(!recipe||!recipe.vehicle||!recipe.vehicle.prefab)throw new InvalidOperationException("Select a prepared vehicle.");
   if(recipe.vehicle.prefab.GetComponent<MachineController>())throw new InvalidOperationException("Select a vehicle body entry, not an already equipped assembly.");
   if(validate){var report=CatalogValidation.Check(recipe.vehicle,true);if(!report.Ok)throw new InvalidOperationException(report.ToString());}
   var root=UnityEngine.Object.Instantiate(recipe.vehicle.prefab);root.name=recipe.displayName;
   try{
    var used=new System.Collections.Generic.HashSet<string>();
    foreach(var fitting in recipe.fittings){
     if(!fitting||fitting.vehicle!=recipe.vehicle)throw new InvalidOperationException("Every fitting must target this vehicle.");
     var report=CatalogValidation.CheckFit(fitting,validate);if(!report.Ok)throw new InvalidOperationException(report.ToString());
     if(!used.Add(fitting.socketId))throw new InvalidOperationException("Two parts occupy socket "+fitting.socketId);
     var slot=root.GetComponentsInChildren<AssetSocket>(true).Single(s=>s.id==fitting.socketId);
     var part=UnityEngine.Object.Instantiate(fitting.part.prefab);part.name=fitting.part.displayName;Align(part,slot.transform,fitting);
    }return root;
   }catch{UnityEngine.Object.DestroyImmediate(root);throw;}
  }
  public static GameObject Bake(AssemblyRecipe recipe){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before assembly authoring.");
   if(!recipe)throw new InvalidOperationException("Select an assembly recipe.");if(string.IsNullOrWhiteSpace(recipe.id))recipe.id="assembly-"+Guid.NewGuid().ToString("N");if(!System.Text.RegularExpressions.Regex.IsMatch(recipe.id,"^[a-z0-9][a-z0-9._-]{2,80}$"))throw new InvalidOperationException("Recipe ID has invalid characters.");if(string.IsNullOrWhiteSpace(recipe.displayName))recipe.displayName=recipe.name;
   var root=Preview(recipe,true);
   try{
    var body=root.GetComponent<PreparedVehicleRig>();if(!body)throw new InvalidOperationException("PreparedVehicleRig missing.");
    var attachmentFits=recipe.fittings.Where(f=>f.part.kind==CatalogKind.Attachment).ToArray();
    if(attachmentFits.Length!=1)throw new InvalidOperationException("The supplied mechanics require exactly one blade or bucket.");
    var fit=attachmentFits[0];var slot=root.GetComponentsInChildren<AssetSocket>(true).Single(s=>s.id==fit.socketId);
    var equipment=slot.GetComponentsInChildren<AssetAnchor>(true).First(a=>a.role==AnchorRole.WorkingEdge);
    var tool=root.GetComponent<AttachmentController>()??root.AddComponent<AttachmentController>();tool.mount=slot.transform;tool.mouth=equipment.transform;tool.useAuthoredRig=true;
    var load=slot.GetComponentsInChildren<AssetAnchor>(true).FirstOrDefault(a=>a.role==AnchorRole.LoadVisual);
    tool.loadVisual=load?load.transform:null;if(load)load.gameObject.SetActive(false);
    tool.boom=body.boom;tool.bucket=slot.transform.childCount>0?slot.transform.GetChild(0):null;
    if(fit.part.attachment.kind==AttachmentKind.Bucket&&(!tool.boom||!tool.bucket||!tool.loadVisual))throw new InvalidOperationException("Bucket requires mapped boom, bucket pivot and load visual.");
    var effects=new GameObject("Authored snow spray");effects.transform.SetParent(tool.mount,false);var ps=effects.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.playOnAwake=false;main.startLifetime=.7f;main.startSize=.12f;main.startSpeed=2;main.maxParticles=150;var emission=ps.emission;emission.enabled=false;tool.spray=ps;
    var renderer=slot.GetComponentInChildren<Renderer>(true);if(renderer)ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=renderer.sharedMaterial;
    var definition=UnityEngine.Object.Instantiate(recipe.vehicle.vehicle);definition.id=string.IsNullOrWhiteSpace(recipe.id)?"assembly-"+Guid.NewGuid().ToString("N"):recipe.id;definition.displayName=recipe.displayName;
    var identity=root.GetComponent<CatalogPlacement>()??root.AddComponent<CatalogPlacement>();identity.assetId=definition.id;identity.placementId="";
    float baseMass=definition.mass;Vector3 moment=definition.dryCenterOfMass*baseMass;
    foreach(var f in recipe.fittings){
     if(f.part.kind==CatalogKind.Attachment)continue;
     var part=f.part;var socket=root.GetComponentsInChildren<AssetSocket>(true).Single(s=>s.id==f.socketId);
     if(part.kind==CatalogKind.WheelSet){
      for(int n=0;n<4;n++){
       float delta=part.wheelMass-body.wheels[n].mass;definition.mass+=delta;moment+=root.transform.InverseTransformPoint(body.wheels[n].transform.position)*delta;
       body.wheels[n].radius=part.wheelRadius;body.wheels[n].mass=part.wheelMass;
       while(body.wheelVisuals[n].childCount>0)UnityEngine.Object.DestroyImmediate(body.wheelVisuals[n].GetChild(0).gameObject);
       var visual=UnityEngine.Object.Instantiate(part.prefab,body.wheelVisuals[n]);visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=part.mirrorRightWheels&&n%2==1?Quaternion.Euler(0,180,0):Quaternion.identity;
      }definition.wheelRadius=part.wheelRadius;
      // The preview's single wheel is just a slot representation, not a fifth wheel.
      if(socket.transform.childCount>0)UnityEngine.Object.DestroyImmediate(socket.transform.GetChild(0).gameObject);
     }else{definition.mass+=part.addedMass;moment+=root.transform.InverseTransformPoint(socket.transform.position)*part.addedMass;}
     definition.torque*=part.torqueMultiplier;definition.baseGrip*=part.gripMultiplier;
    }
    if(definition.mass<=0)throw new InvalidOperationException("Invalid assembled mass.");definition.dryCenterOfMass=moment/definition.mass;
    string defPath=AssetDatabase.GenerateUniqueAssetPath(CatalogPreparation.Root+"/Recipes/"+definition.id+"-physics.asset");AssetDatabase.CreateAsset(definition,defPath);
    var machine=root.GetComponent<MachineController>()??root.AddComponent<MachineController>();machine.definition=definition;machine.equipment=fit.part.attachment;machine.Tool=tool;machine.wheels=body.wheels;machine.wheelVisuals=body.wheelVisuals;machine.Fuel=definition.fuelCapacity;root.GetComponent<Rigidbody>().mass=definition.mass+machine.equipment.mass;
    string prefabPath=AssetDatabase.GenerateUniqueAssetPath(CatalogPreparation.Root+"/Prefabs/"+definition.id+"-assembled.prefab");recipe.bakedPrefab=PrefabUtility.SaveAsPrefabAsset(root,prefabPath);EditorUtility.SetDirty(recipe);AssetDatabase.SaveAssets();return recipe.bakedPrefab;
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
 }
}
