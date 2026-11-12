using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Snowglow.Editor {
 [InitializeOnLoad]
 public static class QuickJobMenuAuthoring {
  const string Folder="Logs/QuickJobMenu";
  static QuickJobMenuAuthoring(){EditorApplication.update+=Pending;}
  static void Pending(){
   if(!File.Exists(Folder+".request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
   File.Delete(Folder+".request");
   try{Apply();}catch(Exception e){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/Validation.txt","FAIL: "+e);Debug.LogException(e);}
  }
  static PrototypeHUD Hud(Scene scene)=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PrototypeHUD>(true)).Single();
  static Transform Dashboard(PrototypeHUD hud)=>hud.productionMenu.pages[Array.IndexOf(hud.productionMenu.pageNames,"dashboard")].transform;
  static void Validate(PrototypeHUD hud){
   ProductionMenuChecks.ValidateAuthored(hud);
   var page=Dashboard(hud);
   foreach(string name in new[]{"Quick Job","Career","Garage"}){
    var card=(RectTransform)page.Find(name);
    var art=card.GetComponentsInChildren<Image>(true).Single(i=>i.name.StartsWith("Game capture | "));
    if(Mathf.Abs(card.sizeDelta.y-326.83784f)>.01f||Mathf.Abs(art.rectTransform.sizeDelta.y-185.83784f)>.01f||!art.preserveAspect)throw new InvalidOperationException("Card fit: "+name);
    var button=card.GetComponentInChildren<Button>(true).GetComponent<RectTransform>();
    if(-button.anchoredPosition.y+button.sizeDelta.y>card.sizeDelta.y-11)throw new InvalidOperationException("Card button overflow: "+name);
   }
  }
  static void Apply(){
   var scene=SceneManager.GetActiveScene();
   if(scene.path!=SceneValidation.ScenePath||scene.isDirty)throw new InvalidOperationException("Open clean saved Pinefall before targeted UI authoring.");
   Directory.CreateDirectory(Folder);File.Copy(scene.path,Folder+"/Pinefall-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");
   ProductionMenuAuthoring.PrepareArt();
   var hud=Hud(scene);ProductionMenuAuthoring.FitDashboardCards(Dashboard(hud));
   EditorUtility.SetDirty(hud);EditorSceneManager.MarkSceneDirty(scene);Validate(hud);SceneValidation.Validate(scene);
   if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed.");
   AssetDatabase.SaveAssets();scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);hud=Hud(scene);Validate(hud);
   try{
    hud.driving.SetActive(false);hud.menuRoot.SetActive(true);hud.productionMenu.gameObject.SetActive(true);hud.productionMenu.Show("dashboard");
    ProductionMenuChecks.Capture(hud,"quick-job-card-updated",1280,720);
    File.Copy(ProductionMenuAuthoring.Folder+"/quick-job-card-updated.png",Folder+"/MainMenu.png",true);
   }finally{scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);}
   SceneValidation.Validate(scene);
   File.WriteAllText(Folder+"/Validation.txt","PASS: imported full 2960 x 1440 Quick Job screenshot through production UI intake (1024 max, Android ASTC 6x6); original PNG retained in SourceAssets. Saved/reopened Pinefall with 382 x 185.83784 image regions and 382 x 326.83784 aligned cards. Text/buttons shifted down 10.83784 units; aspect preserved. Existing button bindings, scene validation and native capture passed. No Play entry or profile writes. Phone appearance remains untested.");
  }
 }
}
