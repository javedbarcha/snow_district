using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Snowglow.Editor {
 public static class VehicleRepairWarningAuthoring {
  const string Request="Logs/RepairWarning.request";
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete(Request);EditorApplication.update-=Pending;try{Install();}catch(Exception e){Directory.CreateDirectory("Logs/RepairWarning");File.WriteAllText("Logs/RepairWarning/Validation.txt","FAIL: "+e);Debug.LogException(e);}}
  public static void Apply(PrototypeHUD hud){
   var old=hud.driving.transform.Find("Repair warning | rise and fade");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
   var go=new GameObject("Repair warning | rise and fade",typeof(RectTransform));go.layer=5;go.transform.SetParent(hud.driving.transform,false);
   var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,0);rect.sizeDelta=new Vector2(340,48);
   var text=go.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.text="REPAIR REQUIRED";text.fontSize=23;text.fontStyle=FontStyle.Bold;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(1,.42f,.10f,0);text.raycastTarget=false;
   var outline=go.AddComponent<Outline>();outline.effectColor=new Color(.04f,.09f,.13f,.95f);outline.effectDistance=new Vector2(1.4f,-1.4f);
   var warning=go.AddComponent<VehicleRepairWarning>();warning.label=text;
   var game=UnityEngine.Object.FindFirstObjectByType<PrototypeSession>();warning.vehicleHeights=game.Fleet.Select(m=>{var renderers=m.GetComponentsInChildren<MeshRenderer>(true);float top=m.transform.position.y+3;foreach(var renderer in renderers)if(renderer.enabled&&renderer.gameObject.activeInHierarchy)top=Mathf.Max(top,renderer.bounds.max.y);return top-m.transform.position.y+.35f;}).ToArray();EditorUtility.SetDirty(hud);
  }
  [MenuItem("Snowglow/UI/Install vehicle repair warning")]
  public static void Install(){
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=SceneValidation.ScenePath)throw new InvalidOperationException("Open Pinefall first.");Directory.CreateDirectory("Logs/RepairWarning");File.Copy(scene.path,"Logs/RepairWarning/Pinefall-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");
   var hud=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PrototypeHUD>(true)).Single();Apply(hud);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);hud=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PrototypeHUD>(true)).Single();ProductionMenuChecks.Run(hud);SceneValidation.Validate(scene);MechanicsChecks.Run();File.WriteAllText("Logs/RepairWarning/Validation.txt","PASS: Saved/reopened repair warning. Native driving press/release/hold, repair, menu and vehicle-switch gates plus fade lifetime checked with production UI fixtures. Scene and snow checks passed. No profile writes. Phone visual acceptance remains separate.");
  }
 }
}
