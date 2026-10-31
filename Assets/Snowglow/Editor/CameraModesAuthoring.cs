using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Snowglow.Editor {
 public static class CameraModesAuthoring {
  const string Request="Logs/CameraModes.request";
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){
   if(!File.Exists(Request)){EditorApplication.update-=Pending;return;}if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
   EditorApplication.update-=Pending;File.Delete(Request);try{Install();}catch(Exception e){Directory.CreateDirectory("Logs/CameraModes");File.WriteAllText("Logs/CameraModes/Validation.txt","FAIL: "+e);Debug.LogException(e);}
  }
  static T[] All<T>(Scene s) where T:Component=>s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
  static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
  static Text Label(Transform parent,string name,string value,float x,float y,float w,float h,int size){var r=Rect(name,parent,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=size;t.color=new Color(.13f,.23f,.29f);t.text=value;t.raycastTarget=false;t.verticalOverflow=VerticalWrapMode.Overflow;return t;}
  static void Button(Transform parent,string label,string command,float x,float y,float w){
   if(parent.GetComponentsInChildren<UICommand>(true).Any(c=>c.command==command))return;
   var r=Rect(label,parent,x,y,w,42);r.gameObject.AddComponent<Image>().color=new Color(.18f,.4f,.43f);var button=r.gameObject.AddComponent<Button>();var action=r.gameObject.AddComponent<UICommand>();action.command=command;UnityEventTools.AddPersistentListener(button.onClick,action.Click);var text=Label(r,label+" label",label,8,6,w-16,30,16);text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;
  }
  [MenuItem("Snowglow/Install gameplay cameras and validate")]
  public static void Install(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");var scene=SceneManager.GetActiveScene();if(scene.path!=SceneValidation.ScenePath||scene.isDirty)throw new InvalidOperationException("Open saved Pinefall before camera authoring.");
   Directory.CreateDirectory("Logs/CameraModes");if(!File.Exists("Logs/CameraModes/Pinefall-before.unity"))File.Copy(scene.path,"Logs/CameraModes/Pinefall-before.unity");
   var placements=All<CatalogPlacement>(scene).Where(p=>!string.IsNullOrEmpty(p.placementId)).ToDictionary(p=>p.placementId,p=>p.assetId+"|"+p.transform.localToWorldMatrix.ToString("R"));
   var session=All<PrototypeSession>(scene).Single();var paths=All<Terrain>(scene).Select(t=>AssetDatabase.GetAssetPath(t.terrainData)).Concat(session.Snow.GetComponentsInChildren<MeshFilter>().Select(m=>AssetDatabase.GetAssetPath(m.sharedMesh))).Concat(new[]{AssetDatabase.GetAssetPath(session.Snow.seed)}).Where(p=>!string.IsNullOrEmpty(p)).Distinct().ToDictionary(p=>p,p=>AssetDatabase.GetAssetDependencyHash(p).ToString());
   var rig=session.CameraRig;rig.terrain=All<Terrain>(scene).Single();rig.SetMode(CameraMode.Isometric);rig.name="Camera | isometric follow and orbit";rig.groundClearance=1;rig.collisionRadius=.4f;rig.orbitPitch=25;rig.orbitDistance=15;rig.scenicPitch=18;rig.scenicDistance=40;rig.scenicSpeed=4;
   var hud=session.HUD;var camera=hud.driving.GetComponentsInChildren<UICommand>(true).Single(c=>c.command=="camera");hud.cameraModeText=camera.GetComponentInChildren<Text>(true);hud.cameraModeText.text="CAM: ISOMETRIC";
   if(!hud.cameraTools){var group=Rect("Camera tools",hud.driving.transform,0,0,0,0);group.anchorMin=Vector2.zero;group.anchorMax=Vector2.one;group.offsetMin=group.offsetMax=Vector2.zero;hud.cameraTools=group.gameObject;}
   Button(hud.cameraTools.transform,"VIEW RESET","camera:reset",18,235,150);hud.cameraTools.SetActive(false);
   if(!hud.cameraHelp)hud.cameraHelp=Label(hud.driving.transform,"Camera view help","ORBIT | drag empty view, pinch or scroll to zoom",350,202,580,54,14);hud.cameraHelp.gameObject.SetActive(false);
   foreach(var text in hud.GetComponentsInChildren<Text>(true).Where(t=>t.text.StartsWith("DESKTOP: W/S",StringComparison.Ordinal)))text.text="DESKTOP: W/S drive/reverse | A/D steer | Space brake | E work/carry | Q blade angle\nV Auto/Manual | R/F lift | T/G tilt | B dump | C cycles 3 cameras | H recover | Esc pause\nOrbit: right-drag or J/L + I/K | scroll or +/- zoom | Home resets";
   DrivingButtonsAuthoring.Apply(hud);
   EditorUtility.SetDirty(rig);EditorUtility.SetDirty(hud);SceneValidation.Validate(scene);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Camera scene save failed.");
   scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);SceneValidation.Validate(scene);session=All<PrototypeSession>(scene).Single();
   if(!session.CameraRig.terrain||!session.HUD.cameraHelp||!session.HUD.cameraModeText||!session.HUD.cameraTools)throw new InvalidOperationException("Saved camera/HUD bindings missing.");
   foreach(string command in new[]{"camera:reset"})if(session.HUD.cameraTools.GetComponentsInChildren<UICommand>(true).Count(c=>c.command==command)!=1)throw new InvalidOperationException("Camera UI command missing/duplicated: "+command);
   var current=All<CatalogPlacement>(scene).Where(p=>!string.IsNullOrEmpty(p.placementId)).ToDictionary(p=>p.placementId,p=>p.assetId+"|"+p.transform.localToWorldMatrix.ToString("R"));foreach(var p in placements)if(!current.TryGetValue(p.Key,out var value)||value!=p.Value)throw new InvalidOperationException("Environment placement changed: "+p.Key);foreach(var p in paths)if(p.Value!=AssetDatabase.GetAssetDependencyHash(p.Key).ToString())throw new InvalidOperationException("Terrain/snow asset changed: "+p.Key);
   CameraModesChecks.Run();
  }
 }
}
