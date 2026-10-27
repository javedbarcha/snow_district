using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Snowglow.Editor {
 // Model-specific Editor intake. Does not replace Pinefall or execute source scripts.
 public static class PlowerModelIntake {
  const string Source=CatalogPreparation.Root+"/Sources/LowPolySnowPlower/";
  const string Request="Logs/PlowerIntake.request";
  const string Report="Logs/PlowerIntake/UnityIntake.txt";
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){
   if(!File.Exists(Request)){EditorApplication.update-=Pending;return;}
   if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
   EditorApplication.update-=Pending;
   string action=File.ReadAllText(Request).Trim();File.Delete(Request);
   try{if(action=="prepare")Prepare();else if(action=="preview")OpenReview();else Inspect();}catch(Exception e){File.WriteAllText(Report,e.ToString());Debug.LogException(e);}
  }
  [MenuItem("Snowglow/Asset intake/Inspect supplied tractor")]
  public static void Inspect(){
   var log=new StringBuilder("Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+"\n");
   foreach(string name in new[]{"TractorBody","PlowAttachment","RoofBeacon"}){
    var model=AssetDatabase.LoadAssetAtPath<GameObject>(Source+name+".fbx");
    if(!model)throw new InvalidOperationException("Import missing: "+name);
    log.AppendLine(name+" bounds "+CatalogValidation.BoundsOf(model));
    foreach(var t in model.GetComponentsInChildren<Transform>())log.AppendLine(t.name+" position "+t.position.ToString("F4")+" scale "+t.lossyScale.ToString("F4"));
    foreach(var r in model.GetComponentsInChildren<Renderer>())log.AppendLine("materials: "+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"MISSING")));
   }
   Directory.CreateDirectory(Path.GetDirectoryName(Report));File.WriteAllText(Report,log.ToString());Debug.Log("Snowglow tractor inspection complete: "+Report);
  }
  static CatalogAsset Entry(string id,string model,CatalogKind kind,string title){
   var library=CatalogPreparation.Library();if(library.Find(id))throw new InvalidOperationException("Already prepared: "+id+". Edit existing entries instead of duplicating intake.");
   var draft=CatalogPreparation.Draft(title,kind,AssetSource.ImportedModel,null,"Supplied low_poly_snow_plower.blend; source SHA256 8F48568B0051F2379707F15809558090DE7128C23637BAD6E6A8774BA815B85C. Embedded text was not executed.");
   draft.id=id;draft.creationSourcePath="SourceAssets/LowPolySnowPlower/low_poly_snow_plower.blend";
   return CatalogPreparation.Prepare(AssetDatabase.LoadAssetAtPath<GameObject>(Source+model+".fbx"),kind,title,1,Vector3.zero,AssetSource.ImportedModel,draft.provenance,draft);
  }
  static void Edit(CatalogAsset a,Action<GameObject> action){
   string path=AssetDatabase.GetAssetPath(a.prefab);var root=PrefabUtility.LoadPrefabContents(path);
   try{action(root);ApplyPalette(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
   CatalogPreparation.RecordMetrics(a);EditorUtility.SetDirty(a);
  }
  static void ApplyPalette(GameObject root){
   string folder=Source+"Materials";Directory.CreateDirectory(folder);
   foreach(var r in root.GetComponentsInChildren<Renderer>(true)){
    var materials=r.sharedMaterials;
    for(int i=0;i<materials.Length;i++){
     string name=materials[i].name;
     if(name.Contains(" | prepared URP"))name=name.Replace(" | prepared URP","");
     int code; if(name.Length<2||!int.TryParse(name.Substring(0,2),out code)||code<1||code>9)throw new InvalidOperationException("Unknown palette: "+name);
     string path=folder+"/Color"+code.ToString("00")+".mat";
     var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
     if(!mat){
      mat=new Material(Shader.Find("Universal Render Pipeline/Simple Lit")){name=name,enableInstancing=true};
      Color[] palette={Color.white,new Color(.95f,.60f,.055f),new Color(.95f,.32f,.055f),new Color(.16f,.16f,.19f),new Color(.23f,.24f,.29f),new Color(.48f,.51f,.58f),new Color(.98f,.94f,.72f),new Color(.14f,.15f,.17f),new Color(.76f,.76f,.8f),new Color(.53f,.72f,.8f,.22f)};
      // Blender stores scene-linear colors; material UI values in Unity are sRGB.
      Color color=palette[code].gamma;color.a=palette[code].a;mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.15f);
      if(code==9){mat.SetFloat("_Surface",1);mat.SetFloat("_SrcBlend",5);mat.SetFloat("_DstBlend",10);mat.SetFloat("_ZWrite",0);mat.SetFloat("_Cull",0);mat.SetOverrideTag("RenderType","Transparent");mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.renderQueue=3000;}
      AssetDatabase.CreateAsset(mat,path);
     }materials[i]=mat;
    }r.sharedMaterials=materials;
   }
  }
  [MenuItem("Snowglow/Asset intake/Prepare supplied tractor")]
  public static void Prepare(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
   // Verify the conversion before persisting any entries.
   var raw=AssetDatabase.LoadAssetAtPath<GameObject>(Source+"TractorBody.fbx");
   if(!raw)throw new InvalidOperationException("Export the FBX files first.");
   var front=raw.GetComponentsInChildren<Transform>().Single(t=>t.name=="wheel_fl");
   if(Vector3.Distance(front.position,new Vector3(-.91f,.56f,.91f))>.02f)throw new InvalidOperationException("Unexpected source conversion: "+front.position.ToString("F4"));
   var body=Entry("imported-tractor-body","TractorBody",CatalogKind.Vehicle,"Golden tractor | body and wheels");
   var blade=Entry("imported-tractor-plow","PlowAttachment",CatalogKind.Attachment,"Golden tractor | curved plow and lift frame");
   var beacon=Entry("imported-tractor-beacon","RoofBeacon",CatalogKind.Accessory,"Golden tractor | roof beacon");
   var def=body.vehicle;def.mass=2800;def.mountId="tractor-front-frame";def.wheelRadius=.578f;def.wheelbase=1.93f;def.track=1.82f;def.maxSpeed=8;def.steering=28;def.dryCenterOfMass=new Vector3(0,.85f,-.25f);def.maxWorkingWidth=3;def.maxAttachmentMass=700;
   body.tags=new[]{"tractor","snowplow","four-wheels","unequal-tyres","yellow"};
   body.assumptions="Metre scale from source. Four independently bound wheels; front radius 0.578 m, rear 0.888 m including treads. Mass 2800 kg, speed 8 m/s and COM are provisional. Uniform four-wheel replacement socket omitted because this tractor has unequal axles. Rendered Blender source inspected; Unity appearance and handling need review.";
   Edit(body,root=>{
    var rig=root.GetComponent<PreparedVehicleRig>();
    // Keep the original source ground in the assembly despite intake centering.
    var fl=rig.wheels[0].transform.localPosition;
    var offset=new Vector3(-.91f,.578f,.91f)-fl;
    foreach(Transform child in root.transform)child.localPosition+=offset;
    for(int i=0;i<4;i++){
     var w=rig.wheels[i];w.radius=i<2?.578f:.888f;w.mass=i<2?40:90;w.suspensionDistance=.2f;
     var spring=w.suspensionSpring;spring.spring=42000;spring.damper=4500;spring.targetPosition=.5f;w.suspensionSpring=spring;
    }
    rig.maximumWheelRadius=.92f;rig.maximumWheelWidth=.6f;
    var box=root.GetComponentInChildren<BoxCollider>();box.center=new Vector3(0,1.0f,-.05f);box.size=new Vector3(1.25f,.8f,3.1f);
    var socket=root.GetComponentsInChildren<AssetSocket>().Single(s=>s.id=="front");socket.interfaceId=def.mountId;socket.transform.localPosition=new Vector3(0,.568f,1.12f);socket.maximumWidth=3;socket.maximumMass=700;
    var wheels=root.GetComponentsInChildren<AssetSocket>().Single(s=>s.id=="wheels");UnityEngine.Object.DestroyImmediate(wheels.gameObject);
    var roof=root.GetComponentsInChildren<AssetSocket>().Single(s=>s.id=="roof");roof.transform.localPosition=new Vector3(0,2.763f,-.65f);roof.maximumMass=30;roof.maximumWidth=.5f;
   });
   blade.interfaceId=def.mountId;blade.attachment.mountId=def.mountId;blade.attachment.width=2.85f;blade.attachment.mass=450;blade.tags=new[]{"blade","curved","orange","tractor-front-frame"};
   blade.assumptions="Plow and lift hardware grouped as a single rigid attachment. Connection is lower chassis pivot (source X1.12/Z0.55). Working edge is cutting-edge bottom centre. Source shows no yaw joint/animation rig; generic vertical lift/yaw would move all hardware rigidly and needs clearance review before gameplay approval.";
   Edit(blade,root=>{
    // Intake centers each module. Recover original coordinate offset from its blade mesh.
    var mesh=root.GetComponentsInChildren<MeshFilter>().Single();Vector3 delta=root.transform.InverseTransformPoint(mesh.transform.TransformPoint(Vector3.zero));
    var anchors=root.GetComponentsInChildren<AssetAnchor>();
    anchors.Single(a=>a.role==AnchorRole.Connection).transform.localPosition=delta+new Vector3(0,.55f,1.12f);
    anchors.Single(a=>a.role==AnchorRole.WorkingEdge).transform.localPosition=delta+new Vector3(0,.12f,2.52f);
   });
   beacon.addedMass=5;beacon.tags=new[]{"beacon","roof","amber"};
   beacon.assumptions="Static roof beacon mesh and base; no flashing light behavior. Five kilogram mass is provisional.";
   Edit(beacon,root=>{
    var b=CatalogValidation.BoundsOf(root);root.GetComponentInChildren<AssetAnchor>().transform.localPosition=new Vector3(b.center.x,b.min.y,b.center.z);
   });
   foreach(var a in new[]{body,blade,beacon}){a.reviewNotes="Needs Unity visual/movement review before Ready publication.";EditorUtility.SetDirty(a);}
   EditorUtility.SetDirty(def);EditorUtility.SetDirty(blade.attachment);
   var plowFit=Fit(body,blade,"front","TractorPlowFit");var beaconFit=Fit(body,beacon,"roof","TractorBeaconFit");
   var recipe=ScriptableObject.CreateInstance<AssemblyRecipe>();recipe.id="golden-tractor-plow";recipe.displayName="Golden tractor with snowplow";recipe.vehicle=body;recipe.fittings.Add(plowFit);recipe.fittings.Add(beaconFit);
   AssetDatabase.CreateAsset(recipe,CatalogPreparation.Root+"/Recipes/GoldenTractor.asset");AssetDatabase.SaveAssets();
   // A saved Editor preview; it deliberately has no MachineController and is not a gameplay-ready assembly.
   var preview=CatalogAssembly.Preview(recipe,false);
   try{PrefabUtility.SaveAsPrefabAsset(preview,CatalogPreparation.Root+"/Prefabs/GoldenTractor-REVIEW.prefab");}finally{UnityEngine.Object.DestroyImmediate(preview);}
   var report=new StringBuilder("Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+"\n");
   foreach(var a in new[]{body,blade,beacon}){
    var check=CatalogValidation.Check(a);if(!check.Ok)throw new InvalidOperationException(check.ToString());
    report.AppendLine(a.id+" "+a.localBounds+" triangles="+a.triangles+" renderers="+a.renderers+" slots="+a.materialSlots+"\n"+check);
    // Reopen saved prefabs and verify persistent mesh/material references.
    var reopened=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(a.prefab));
    try{foreach(var filter in reopened.GetComponentsInChildren<MeshFilter>())if(!filter.sharedMesh||!AssetDatabase.Contains(filter.sharedMesh))throw new InvalidOperationException("Unsaved mesh");}finally{PrefabUtility.UnloadPrefabContents(reopened);}
   }
   foreach(var f in recipe.fittings){var check=CatalogValidation.CheckFit(f,false);if(!check.Ok)throw new InvalidOperationException(check.ToString());report.AppendLine("Fit structural pass: "+f.socketId);}
   bool blocked=false;try{CatalogAssembly.Bake(recipe);}catch(InvalidOperationException){blocked=true;}if(!blocked)throw new InvalidOperationException("Unreviewed bake must be blocked");
   report.AppendLine("PASS: saved/reopened prefabs, persistent meshes, separate wheel bindings, structural fit checks, unreviewed gameplay bake blocked. No Pinefall changes. Physical articulation, Unity rendering/driving, Android APK and Galaxy A07 performance remain untested.");
   AssetDatabase.SaveAssets();File.WriteAllText(Report,report.ToString());Debug.Log("Snowglow tractor preparation complete: "+Report);
  }
  static FittingProfile Fit(CatalogAsset body,CatalogAsset part,string socket,string name){var f=ScriptableObject.CreateInstance<FittingProfile>();f.vehicle=body;f.part=part;f.socketId=socket;f.reviewNotes="Inferred from named source parts and measured attachment positions. Review full movement envelope.";AssetDatabase.CreateAsset(f,CatalogPreparation.Root+"/Recipes/"+name+".asset");return f;}
  [MenuItem("Snowglow/Asset intake/Open tractor review prefab")]
  public static void OpenReview(){
   var stage=UnityEditor.SceneManagement.PrefabStageUtility.OpenPrefab(CatalogPreparation.Root+"/Prefabs/GoldenTractor-REVIEW.prefab");
   Selection.activeGameObject=stage.prefabContentsRoot;
   var bounds=CatalogValidation.BoundsOf(stage.prefabContentsRoot);
   if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(bounds.center,Quaternion.Euler(25,215,0),5,true,true);
  }
 }
}

