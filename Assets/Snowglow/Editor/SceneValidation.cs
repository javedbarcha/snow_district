using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Snowglow.Editor {
 public static class SceneValidation {
  public const string ScenePath="Assets/Scenes/Pinefall.unity";
  static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException("Snowglow validation: "+message);}

  // Runs against deserialized scene objects, without entering Play or constructing content.
  public static void Validate(Scene scene){
   Require(scene.IsValid()&&scene.isLoaded,"district scene must be loaded");
   var roots=scene.GetRootGameObjects();
   var objects=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
   foreach(var t in objects)Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"missing script on "+t.name);
   var sessions=roots.SelectMany(r=>r.GetComponentsInChildren<PrototypeSession>(true)).ToArray();
   Require(sessions.Length==1,"expected one authored session; bake the district first");
   var session=sessions[0];
   Require(session.Input&&session.HUD&&session.CameraRig&&session.Audio,"session presentation/input references missing");
   Require(session.HUD.panels.Length==10&&session.HUD.panels.All(p=>p),"menu panels missing");
   Require(session.Fleet!=null&&session.Fleet.Length==2,"expected plow and loader");
   foreach(var m in session.Fleet){
    Require(m&&m.definition&&m.equipment&&m.Tool&&m.GetComponent<Rigidbody>(),"machine rig references missing");
    Require(m.equipment.Validate(m.definition)==null,"incompatible attachment on "+m.name);
    Require(AssetDatabase.Contains(m.definition)&&AssetDatabase.Contains(m.equipment),"machine definitions must be saved assets");
    Require(PrefabUtility.IsPartOfPrefabInstance(m),"machine must be a connected authored prefab");
    Require(m.wheels.Length==4&&m.wheels.All(w=>w&&w.radius>0&&w.suspensionDistance>0),"invalid WheelCollider rig");
    Require(m.wheelVisuals.Length==4&&m.wheelVisuals.All(w=>w),"wheel visual anchors missing");
    Require(m.Tool.mount&&m.Tool.mouth&&m.Tool.spray,"attachment anchors/effects missing");
    if(m.definition.loader)Require(m.Tool.boom&&m.Tool.bucket&&m.Tool.loadVisual,"loader articulation/load anchors missing");
   }
   var snow=session.Snow;
   Require(snow&&snow.seed&&AssetDatabase.Contains(snow.seed),"saved snow seed missing");
   Require(snow.seed.grid.depth.Length==65536&&snow.seed.grid.credit.Length==65536&&snow.seed.grid.blocked.Length==65536,"invalid snow seed dimensions");
   Require(snow.transform.childCount==256,"expected 256 baked snow chunks");
   foreach(Transform chunk in snow.transform){
    var mf=chunk.GetComponent<MeshFilter>();var mc=chunk.GetComponent<MeshCollider>();
    Require(mf&&mf.sharedMesh&&AssetDatabase.Contains(mf.sharedMesh)&&mc,"saved snow mesh/collider missing on "+chunk.name);
    if(mc.enabled)Require(mc.sharedMesh&&AssetDatabase.Contains(mc.sharedMesh),"saved pile collision mesh missing");
   }
   var terrains=roots.SelectMany(r=>r.GetComponentsInChildren<Terrain>(true)).ToArray();
   Require(terrains.Length==1&&terrains[0].terrainData&&AssetDatabase.Contains(terrains[0].terrainData),"saved terrain missing");
   Require(terrains[0].terrainData.size.x==1000&&terrains[0].terrainData.size.z==1000,"terrain must span 1000 metres");
   foreach(var renderer in roots.SelectMany(r=>r.GetComponentsInChildren<Renderer>(true)))
    foreach(var material in renderer.sharedMaterials)Require(material&&material.shader&&material.shader.name!="Hidden/InternalErrorShader"&&AssetDatabase.Contains(material),"invalid saved material on "+renderer.name);
   Require(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset,"URP must be assigned");
   Require(EditorBuildSettings.scenes.Any(s=>s.enabled&&s.path==ScenePath),"district missing from build scenes");
  }

  [MenuItem("Snowglow/Validate saved district and snow ledger")]
  public static void Run(){
   Require(!EditorApplication.isPlayingOrWillChangePlaymode,"stop Play before validation");
   if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   Directory.CreateDirectory("Logs");
   try{
    var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
    Validate(scene);MechanicsChecks.Run();
    string report="PASS: Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+"\nSaved district reopened; authored rigs, terrain, materials, snow meshes/colliders, URP and nine ledger invariants validated.\nPlay handling, rendering, touch, Android build and Galaxy A07 performance require separate testing.\n";
    File.WriteAllText("Logs/SceneValidation.txt",report);Debug.Log(report);
   }catch(Exception e){File.WriteAllText("Logs/SceneValidation.txt","FAIL: "+e);throw;}
  }

  // Batch entry point for a disposable project copy; deliberately replaces its district.
  public static void BakeAndRun(){PrototypeAuthoring.Bake();Run();}
 }
}
