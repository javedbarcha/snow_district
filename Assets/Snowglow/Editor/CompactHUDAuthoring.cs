using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Snowglow.Editor {
 public static class CompactHUDAuthoring {
  const string Request="Logs/CompactHUD.request";
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){
   if(!File.Exists(Request)){EditorApplication.update-=Pending;return;}
   if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
   if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}
   if(EditorApplication.isPlayingOrWillChangePlaymode)return;
   EditorApplication.update-=Pending;File.Delete(Request);
   try{Install();}catch(Exception e){Directory.CreateDirectory("Logs/CompactHUD");File.WriteAllText("Logs/CompactHUD/Validation.txt","FAIL: "+e);Debug.LogException(e);}
  }
  public static void Apply(PrototypeHUD hud){
   // Only the old information panels lose their background. Driving buttons and
   // menu panels retain their authored appearance and event bindings.
   foreach(string name in new[]{"Top HUD","Equipment status"}){
    var panel=hud.driving.transform.Find(name);
    if(panel){var image=panel.GetComponent<Image>();if(image)UnityEngine.Object.DestroyImmediate(image);}
   }
   var texts=new[]{hud.status,hud.notice,hud.objective};
   for(int i=0;i<texts.Length;i++){
    var text=texts[i];var rect=text.rectTransform;rect.SetParent(hud.driving.transform,false);
    rect.anchorMin=rect.anchorMax=new Vector2(i*.5f,1);rect.pivot=new Vector2(i*.5f,1);
    rect.anchoredPosition=new Vector2(i==0?18:i==2?-18:0,-12);rect.sizeDelta=new Vector2(400,76);
    text.fontSize=14;text.resizeTextForBestFit=true;text.resizeTextMinSize=12;text.resizeTextMaxSize=14;
    text.alignment=i==0?TextAnchor.UpperLeft:i==1?TextAnchor.UpperCenter:TextAnchor.UpperRight;
    text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
    text.raycastTarget=false;
    var outline=text.GetComponent<Outline>();if(!outline)outline=text.gameObject.AddComponent<Outline>();
    outline.effectColor=new Color(1,1,1,.75f);outline.effectDistance=new Vector2(.7f,-.7f);
    EditorUtility.SetDirty(text);
   }
  }
  static PrototypeHUD HUD(Scene scene)=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PrototypeHUD>(true)).Single();
  public static void Batch(){EditorSceneManager.OpenScene(SceneValidation.ScenePath);Install();}
  [MenuItem("Snowglow/UI/Use compact text-only driving readouts")]
  public static void Install(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
   var scene=SceneManager.GetActiveScene();if(scene.path!=SceneValidation.ScenePath)throw new InvalidOperationException("Open Pinefall first.");
   Directory.CreateDirectory("Logs/CompactHUD");
   if(!File.Exists("Logs/CompactHUD/Pinefall-before.unity"))File.Copy(scene.path,"Logs/CompactHUD/Pinefall-before.unity");
   if(scene.isDirty&&!File.Exists("Logs/CompactHUD/Pinefall-unsaved-before.unity"))EditorSceneManager.SaveScene(scene,"Logs/CompactHUD/Pinefall-unsaved-before.unity",true);
   var hud=HUD(scene);Apply(hud);SceneValidation.Validate(scene);EditorSceneManager.MarkSceneDirty(scene);
   if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save compact HUD.");
   AssetDatabase.SaveAssets();scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);SceneValidation.Validate(scene);hud=HUD(scene);
   foreach(var text in new[]{hud.status,hud.notice,hud.objective})
    if(text.transform.parent!=hud.driving.transform||text.fontSize!=14||text.raycastTarget)throw new InvalidOperationException("Readout layout did not persist.");
   Preview(hud);EditorSceneManager.OpenScene(SceneValidation.ScenePath);
   File.WriteAllText("Logs/CompactHUD/Validation.txt","PASS: Unity "+Application.unityVersion+"; saved/reopened compact driving HUD. Status top left, notice/equipment top centre, objective top right; 14px text (12px best-fit minimum), 400x76 design-space regions, no readout backgrounds, no text raycast targets. Existing driving controls and menu panels retained. Scene validation passed. Native UI preview rendered at 1280x720. Physical-phone readability remains untested.");
   Debug.Log(File.ReadAllText("Logs/CompactHUD/Validation.txt"));
  }
  static void Preview(PrototypeHUD hud){
   var canvas=hud.GetComponent<Canvas>();var cameraObject=new GameObject("Temporary HUD preview camera");var camera=cameraObject.AddComponent<Camera>();
   var rt=new RenderTexture(1280,720,24);var previous=RenderTexture.active;Texture2D texture=null;
   bool driving=hud.driving.activeSelf,menu=hud.menuRoot.activeSelf;var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;
   var texts=new[]{hud.status,hud.notice,hud.objective};var contents=texts.Select(t=>t.text).ToArray();
   var transforms=hud.GetComponentsInChildren<Transform>(true);var layers=transforms.Select(t=>t.gameObject.layer).ToArray();
   try{
    foreach(var t in transforms)t.gameObject.layer=5;
    camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.85f,.93f,.96f);camera.cullingMask=1<<5;camera.orthographic=true;camera.orthographicSize=360;camera.nearClipPlane=.1f;camera.farClipPlane=100;
    camera.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
    hud.driving.SetActive(true);hud.menuRoot.SetActive(false);
    hud.status.text=@"SNOWGLOW | Golden snow-plowing tractor
0 km/h  REVERSE  FUEL 91%  CONDITION 60%";
    hud.notice.text=@"Welcome to Pinefall. Start at the depot.
Blade UP | angle -1";
    hud.objective.text=@"CONTRACT 3  0% cleared
1x combo  LATE 2 min  400 coins";
    Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes("Logs/CompactHUD/Preview.png",texture.EncodeToPNG());
   }finally{
    for(int i=0;i<transforms.Length;i++)transforms[i].gameObject.layer=layers[i];
    for(int i=0;i<texts.Length;i++)texts[i].text=contents[i];
    hud.driving.SetActive(driving);hud.menuRoot.SetActive(menu);canvas.renderMode=mode;canvas.worldCamera=oldCamera;
    RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);if(texture)UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(cameraObject);
   }
  }
 }
}
