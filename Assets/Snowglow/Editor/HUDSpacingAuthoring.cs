using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Snowglow.Editor {
 public static class HUDSpacingAuthoring {
  const string Request="Logs/HUDSpacing.request",Folder="Logs/HUDSpacing";static int checks;
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;try{File.Delete(Request);}catch(IOException){return;}EditorApplication.update-=Pending;try{Run();}catch(Exception e){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/Validation.txt","FAIL: "+e);Debug.LogException(e);}}
  static void Check(bool condition,string message){checks++;if(!condition)throw new InvalidOperationException(message);}
  static void Equal(float a,float b,string message){Check(Mathf.Abs(a-b)<.02f,message+": "+a+" vs "+b);}
  [MenuItem("Snowglow/UI/Apply percentage driving HUD spacing")]
  public static void Run(){
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();Check(scene.path==SceneValidation.ScenePath,"Open Pinefall");Check(!EditorApplication.isPlayingOrWillChangePlaymode,"Stop Play");Directory.CreateDirectory(Folder);File.Copy(scene.path,Folder+"/Pinefall-before.unity",true);if(scene.isDirty)EditorSceneManager.SaveScene(scene,Folder+"/Pinefall-unsaved-before.unity",true);
   var hud=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PrototypeHUD>(true)).Single();ControlSheetAuthoring.ApplyIfPrepared(hud);var r=hud.GetComponent<ResponsiveDrivingHUD>();Check(r,"Responsive component missing");
   Check(!hud.driving.GetComponentsInChildren<UICommand>(true).Any(c=>c.command=="recover"||c.command=="recover:depot"),"Driving recovery remains");
   var service=hud.panels[Array.IndexOf(hud.names,"service")];var recovery=service.GetComponentsInChildren<UICommand>(true).Single(c=>c.command=="recover"||c.command=="recover:depot");var recoveryButton=recovery.GetComponent<UnityEngine.UI.Button>();Check(recoveryButton.onClick.GetPersistentEventCount()==1&&recoveryButton.onClick.GetPersistentTarget(0)==recovery&&recoveryButton.onClick.GetPersistentMethodName(0)=="Click","Service recovery binding missing");
   var garage=hud.panels[Array.IndexOf(hud.names,"garage")];Check(garage.GetComponentsInChildren<UICommand>(true).Any(c=>c.command=="open:service"),"Garage service route missing");
   foreach(var size in new[]{new Vector2(1280,720),new Vector2(1600,720),new Vector2(1920,1080),new Vector2(1024,768),new Vector2(2340,1080)}){
    float w=size.x,h=size.y,fit=Mathf.Min(w/1280,h/720);r.Reflow(w,h);
    Equal(r.forward.sizeDelta.x/r.reverse.sizeDelta.x,1.25f,"Gas width ratio");Equal(r.forward.sizeDelta.y/r.reverse.sizeDelta.y,1.25f,"Gas height ratio");
    Equal(r.reverse.anchoredPosition.y,h*.05f,"Brake bottom margin");Equal(r.steerLeft.anchoredPosition.y,h*.05f,"Steering bottom margin");Equal(r.lift.anchoredPosition.y-r.forward.anchoredPosition.y-r.forward.sizeDelta.y,h*.05f,"Gas-to-lift gap");
    Equal(-r.reverse.anchoredPosition.x-r.forward.sizeDelta.x,w*.04f,"Gas-brake gap");Equal(r.steerRight.anchoredPosition.x-r.steerLeft.anchoredPosition.x-r.steerLeft.sizeDelta.x,w*.04f,"Steering gap");
    Equal(((RectTransform)hud.bladeAngleButtons[0].transform).anchoredPosition.y-r.steerLeft.anchoredPosition.y-r.steerLeft.sizeDelta.y,h*.08f,"Steer-angle gap");Equal(((RectTransform)hud.loaderDumpButton.transform).anchoredPosition.y-r.steerLeft.anchoredPosition.y-r.steerLeft.sizeDelta.y,h*.08f,"Steer-dump gap");
    for(int i=1;i<hud.bladeAngleButtons.Length;i++){var previous=(RectTransform)hud.bladeAngleButtons[i-1].transform;var current=(RectTransform)hud.bladeAngleButtons[i].transform;Equal(current.anchoredPosition.x-previous.anchoredPosition.x-previous.sizeDelta.x,w*.015f,"Tilt button gap");}
    Equal(-r.cameraButton.anchoredPosition.y,h*.1f,"Top camera margin");Equal(-r.pause.anchoredPosition.y,h*.1f,"Top pause margin");Equal(hud.leftControls.anchorMin.x,.05f,"Left width margin");Equal(hud.rightControls.anchorMin.x,.95f,"Right width margin");
    Check((-r.stats.anchoredPosition.y+r.stats.sizeDelta.y)*fit<h*.1f,"Stats exceeds reserved top band");
    foreach(bool loader in new[]{false,true}){hud.RefreshEquipmentControls(loader);Check(hud.bladeAngleButtons.All(b=>b.activeSelf==!loader)&&hud.loaderDumpButton.activeSelf==loader,"Equipment visibility");}
   }
   MenuRecoveryChecks.Run();
   foreach(var text in hud.GetComponentsInChildren<UnityEngine.UI.Text>(true))if(text.text.Contains("Free recovery preserves")){text.text=text.text.Replace("Free recovery preserves the bucket contents and the district snow.","Recovery leaves bucket snow at the pickup location. Fuel and condition are unchanged.");EditorUtility.SetDirty(text);}
   hud.RefreshEquipmentControls(false);r.Reflow(hud.safeArea.rect.width,hud.safeArea.rect.height);EditorUtility.SetDirty(r);EditorUtility.SetDirty(hud);SceneValidation.Validate(scene);EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Save failed");scene=EditorSceneManager.OpenScene(scene.path);
   hud=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PrototypeHUD>(true)).Single();r=hud.GetComponent<ResponsiveDrivingHUD>();Check(r&&r.forward&&r.reverse&&r.lift&&r.stats,"Saved refs missing");Equal(DirectionalDriveState.StopBuffer,.1f,"Direction wait");DirectionBufferChecks.Run();
   Directory.CreateDirectory("Logs/CompactHUD");var preview=typeof(CompactHUDAuthoring).GetMethod("Preview",BindingFlags.Static|BindingFlags.NonPublic);r.Reflow(1280,720);hud.RefreshEquipmentControls(false);preview.Invoke(null,new object[]{hud});File.Copy("Logs/CompactHUD/Preview.png",Folder+"/Tractor.png",true);hud.RefreshEquipmentControls(true);preview.Invoke(null,new object[]{hud});File.Copy("Logs/CompactHUD/Preview.png",Folder+"/Loader.png",true);EditorSceneManager.OpenScene(scene.path);
   File.WriteAllText(Folder+"/Validation.txt","PASS: Unity "+Application.unityVersion+"; "+checks+" spacing/binding checks at five landscape aspect ratios. Saved/reopened responsive references; 25% larger Forward width/height; 5% safe-area width side margins, 10% height top band, 5% height bottom and pedal gap, 8% height steering-to-angle/Dump gap, 4% width steering and 4% width gas/brake gap, 1.5% width between angle buttons; no driving Recover button, service-menu recovery retained. Stats remains in top band. Coasting and stopped 0.1s reversal checks, recovery physics, scene validation and 19 snow checks passed. Tractor/loader native HUD previews rendered. Native screen percentages are based on usable safe area; actual phone touch/notches/ergonomics remain untested. No player-save or scenery edits.");Debug.Log(File.ReadAllText(Folder+"/Validation.txt"));
  }
 }
}
