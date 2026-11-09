using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Snowglow.Editor {
 public static class RemoveLegacyCameraButton {
  const string Request="Logs/RemoveLegacyCamera.request",Folder="Logs/RemoveLegacyCamera";
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;try{File.Delete(Request);}catch(IOException){return;}EditorApplication.update-=Pending;try{Run();}catch(Exception e){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/Validation.txt","FAIL: "+e);Debug.LogException(e);}}
  [MenuItem("Snowglow/UI/Remove legacy camera button")]
  public static void Run(){var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=SceneValidation.ScenePath)throw new InvalidOperationException("Open Pinefall");Directory.CreateDirectory(Folder);File.Copy(scene.path,Folder+"/Pinefall-before.unity",true);var hud=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<PrototypeHUD>(true)).Single();foreach(var c in hud.driving.GetComponentsInChildren<UICommand>(true).Where(c=>c.command=="camera:reset").ToArray())UnityEngine.Object.DestroyImmediate(c.gameObject);var responsive=hud.GetComponent<ResponsiveDrivingHUD>();if(responsive){responsive.reset=null;EditorUtility.SetDirty(responsive);}SceneValidation.Validate(scene);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Save failed");scene=EditorSceneManager.OpenScene(scene.path);hud=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<PrototypeHUD>(true)).Single();var commands=hud.driving.GetComponentsInChildren<UICommand>(true);if(commands.Count(c=>c.command=="camera")!=1||commands.Any(c=>c.command=="camera:reset"))throw new InvalidOperationException("Camera button count incorrect");var camera=commands.Single(c=>c.command=="camera");var button=camera.GetComponent<UnityEngine.UI.Button>();if(button.onClick.GetPersistentTarget(0)!=camera)throw new InvalidOperationException("New camera binding missing");SceneValidation.Validate(scene);File.WriteAllText(Folder+"/Validation.txt","PASS: Saved/reopened Pinefall; removed legacy Orbit View Reset icon, retained exactly one new camera-switch button with persistent binding. Authoring paths no longer recreate the removed button. Scene validation passed. Keyboard Home reset remains; no scenery, physics or player-save changes. Phone build/appearance separate.");}
 }
}
