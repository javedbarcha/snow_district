using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Snowglow.Editor {
 public static class ControlSheetAuthoring {
  const string Folder="Logs/ControlSheet",Source=CatalogPreparation.Root+"/Sources/ControlIconSheet/",Request="Logs/ControlSheet.request";
  static readonly string[] Icons={"camera","garage","pause","lights-off","lights-on","steer-left","steer-right","blade-left","blade-straight","blade-right","dump","lift-toggle","forward","reverse"};
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;EditorApplication.update-=Pending;File.Delete(Request);try{Install();}catch(Exception e){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/Validation.txt","FAIL: "+e);Debug.LogException(e);}}
  static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("Control sheet: "+message);}
  static Sprite Icon(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Source+name+".png");
  static void Prepare(){
   var library=CatalogPreparation.Library();foreach(var name in Icons){string path=Source+name+".png";var imp=(TextureImporter)AssetImporter.GetAtPath(path);Check(imp,"Missing extracted icon "+name);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.filterMode=FilterMode.Bilinear;imp.wrapMode=TextureWrapMode.Clamp;imp.maxTextureSize=512;imp.npotScale=TextureImporterNPOTScale.None;
    var android=imp.GetPlatformTextureSettings("Android");android.overridden=true;android.maxTextureSize=512;android.format=TextureImporterFormat.ASTC_6x6;imp.SetPlatformTextureSettings(android);imp.SaveAndReimport();
    string id="control-sheet-"+name;var asset=library.Find(id);if(!asset){asset=ScriptableObject.CreateInstance<CatalogAsset>();asset.id=id;AssetDatabase.CreateAsset(asset,CatalogPreparation.Root+"/Entries/"+id+".asset");library.entries.Add(asset);}
    asset.displayName="Driving control | "+name;asset.kind=CatalogKind.UI;asset.status=PreparationStatus.NeedsReview;asset.sourceType=AssetSource.ReferenceImage;asset.sourcePath=path;asset.sourceGuid=AssetDatabase.AssetPathToGUID(path);asset.creationSourcePath="SourceAssets/ControlIconSheet/Original.png";asset.provenance="User supplied transparent control sheet. Original SHA256 and pixel extraction recorded in SourceAssets/ControlIconSheet/Manifest.json; no source instructions executed.";asset.assumptions="Pixel crop only, no resampling or recoloring; alpha <=16 removed as transparent background residue. Aspect preserved by UI Image. Android ASTC 6x6, no mipmaps. Needs phone review.";asset.blocksSnow=false;asset.minimumScale=.25f;asset.maximumScale=2;asset.tags=new[]{"ui","driving","control-sheet"};asset.uiSprites=new[]{Icon(name)};
    var root=new GameObject(asset.displayName,typeof(RectTransform),typeof(Image),typeof(Button));try{((RectTransform)root.transform).sizeDelta=asset.uiSprites[0].rect.size;var image=root.GetComponent<Image>();image.sprite=asset.uiSprites[0];image.preserveAspect=true;root.AddComponent<CatalogPlacement>().assetId=id;asset.prefab=PrefabUtility.SaveAsPrefabAsset(root,CatalogPreparation.Root+"/Prefabs/"+id+".prefab");}finally{UnityEngine.Object.DestroyImmediate(root);}CatalogPreparation.RecordMetrics(asset);Check(CatalogValidation.Check(asset).Ok,"Catalogue entry "+id);EditorUtility.SetDirty(asset);
   }EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
  }
  static UICommand Command(PrototypeHUD hud,string key)=>hud.driving.GetComponentsInChildren<UICommand>(true).First(c=>c.command==key);
  static void Place(RectTransform rect,Transform parent,float x,float y,float w,float h,bool top=false,bool right=false){rect.SetParent(parent,false);rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;rect.anchorMin=rect.anchorMax=new Vector2(right?1:0,top?1:0);rect.pivot=new Vector2(right?1:0,top?1:0);rect.anchoredPosition=new Vector2(x,top?-y:y);rect.sizeDelta=new Vector2(w,h);}
  static void Skin(Button button,string icon){
   var found=button.transform.Find("PSD kit artwork");var child=found?found:new GameObject("PSD kit artwork",typeof(RectTransform),typeof(Image)).transform;child.SetParent(button.transform,false);child.SetAsFirstSibling();var r=(RectTransform)child;r.localScale=Vector3.one;r.localRotation=Quaternion.identity;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=new Vector2(0,0);var image=child.GetComponent<Image>();image.sprite=Icon(icon);image.preserveAspect=true;image.raycastTarget=false;image.color=Color.white;button.GetComponent<Image>().color=Color.clear;button.targetGraphic=image;button.transition=Selectable.Transition.ColorTint;button.navigation=new Navigation{mode=Navigation.Mode.None};foreach(var text in button.GetComponentsInChildren<Text>(true))text.gameObject.SetActive(false);EditorUtility.SetDirty(button);
  }
  static void Bind(UICommand c,string key){c.command=key;c.name=key;var b=c.GetComponent<Button>();b.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(b.onClick,c.Click);}
  public static void ApplyIfPrepared(PrototypeHUD hud){
   if(!Icon("blade-left")||!hud.leftControls||!hud.rightControls)return;
   var root=hud.driving.transform;var angles=root.GetComponentsInChildren<UICommand>(true).Where(c=>c.command=="angle"||c.command.StartsWith("angle:")).ToArray();if(angles.Length==0)return;
   var template=angles.First();var ordered=new UICommand[3];for(int i=0;i<3;i++){string key="angle:"+(i-1);var command=angles.FirstOrDefault(c=>c.command==key);if(!command){command=i==0&&template.command=="angle"?template:UnityEngine.Object.Instantiate(template.gameObject,hud.leftControls).GetComponent<UICommand>();}Bind(command,key);ordered[i]=command;Place((RectTransform)command.transform,hud.leftControls,i*100,174,84,84);Skin(command.GetComponent<Button>(),Icons[7+i]);}
   hud.bladeAngleButtons=ordered.Select(c=>c.gameObject).ToArray();var dump=Command(hud,"dump");Place((RectTransform)dump.transform,hud.leftControls,0,174,84,84);Skin(dump.GetComponent<Button>(),"dump");hud.loaderDumpButton=dump.gameObject;
   foreach(var touch in root.GetComponentsInChildren<TouchControl>(true)){string icon=null;switch(touch.channel){case "steerLeft":Place((RectTransform)touch.transform,hud.leftControls,0,36,112,112);icon="steer-left";break;case "steerRight":Place((RectTransform)touch.transform,hud.leftControls,160,36,112,112);icon="steer-right";break;case "forward":Place((RectTransform)touch.transform,hud.rightControls,0,180,110,110,false,true);icon="forward";break;case "reverse":Place((RectTransform)touch.transform,hud.rightControls,0,36,110,110,false,true);icon="reverse";break;}if(icon!=null)Skin(touch.GetComponent<Button>(),icon);}
   var work=Command(hud,"work");Place((RectTransform)work.transform,hud.rightControls,-148,36,90,90,false,true);Skin(work.GetComponent<Button>(),"lift-toggle");hud.workSkinImage=work.transform.Find("PSD kit artwork").GetComponent<Image>();hud.plowWorkSprite=hud.loaderWorkSprite=Icon("lift-toggle");
   foreach(var pair in new[]{new[]{"camera","camera"},new[]{"open:garage","garage"},new[]{"pause","pause"}}){var c=Command(hud,pair[0]);bool right=pair[0]=="pause";Place((RectTransform)c.transform,right?hud.rightControls:hud.leftControls,0,pair[0]=="open:garage"?202:112,72,72,true,right);Skin(c.GetComponent<Button>(),pair[1]);}
   var lights=root.GetComponentsInChildren<VehicleLightsButton>(true).Single();Place((RectTransform)lights.transform,hud.rightControls,0,260,64,64,true,true);lights.onIcon=Icon("lights-on");lights.offIcon=Icon("lights-off");lights.Refresh(null);
   foreach(var recover in root.GetComponentsInChildren<UICommand>(true).Where(c=>c.command=="recover:depot"||c.command=="recover").ToArray())UnityEngine.Object.DestroyImmediate(recover.gameObject);
   foreach(var reset in root.GetComponentsInChildren<UICommand>(true).Where(c=>c.command=="camera:reset").ToArray())UnityEngine.Object.DestroyImmediate(reset.gameObject);
   var responsive=hud.GetComponent<ResponsiveDrivingHUD>();if(!responsive)responsive=hud.gameObject.AddComponent<ResponsiveDrivingHUD>();responsive.hud=hud;
   responsive.forward=(RectTransform)root.GetComponentsInChildren<TouchControl>(true).Single(t=>t.channel=="forward").transform;responsive.reverse=(RectTransform)root.GetComponentsInChildren<TouchControl>(true).Single(t=>t.channel=="reverse").transform;
   responsive.steerLeft=(RectTransform)root.GetComponentsInChildren<TouchControl>(true).Single(t=>t.channel=="steerLeft").transform;responsive.steerRight=(RectTransform)root.GetComponentsInChildren<TouchControl>(true).Single(t=>t.channel=="steerRight").transform;responsive.lift=(RectTransform)work.transform;
   responsive.cameraButton=(RectTransform)Command(hud,"camera").transform;responsive.garage=(RectTransform)Command(hud,"open:garage").transform;responsive.pause=(RectTransform)Command(hud,"pause").transform;responsive.lights=(RectTransform)lights.transform;
   responsive.reset=null;
   var stats=hud.safeArea.GetComponentInChildren<MobilePerformanceStats>(true);if(stats)responsive.stats=(RectTransform)stats.toggleButton.transform;
   responsive.Reflow(hud.safeArea.rect.width,hud.safeArea.rect.height);EditorUtility.SetDirty(responsive);
   hud.RefreshEquipmentControls(false);EditorUtility.SetDirty(hud);
  }
  [MenuItem("Snowglow/Asset intake/Import driving control icon sheet")]
  public static void Install(){
   Check(!EditorApplication.isPlayingOrWillChangePlaymode,"Stop Play first");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();Check(scene.path==SceneValidation.ScenePath,"Open Pinefall first");Directory.CreateDirectory(Folder);if(!File.Exists(Folder+"/Pinefall-before.unity"))File.Copy(scene.path,Folder+"/Pinefall-before.unity");if(scene.isDirty)EditorSceneManager.SaveScene(scene,Folder+"/Pinefall-unsaved-before.unity",true);
   Prepare();var hud=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PrototypeHUD>(true)).Single();DrivingButtonsAuthoring.Apply(hud);Validate(hud);SceneValidation.Validate(scene);EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"Save failed");AssetDatabase.SaveAssets();scene=EditorSceneManager.OpenScene(scene.path);hud=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PrototypeHUD>(true)).Single();Validate(hud);SceneValidation.Validate(scene);MechanicsChecks.Run();
   var preview=typeof(CompactHUDAuthoring).GetMethod("Preview",BindingFlags.Static|BindingFlags.NonPublic);Directory.CreateDirectory("Logs/CompactHUD");hud.RefreshEquipmentControls(false);preview.Invoke(null,new object[]{hud});File.Copy("Logs/CompactHUD/Preview.png",Folder+"/Tractor.png",true);hud.RefreshEquipmentControls(true);preview.Invoke(null,new object[]{hud});File.Copy("Logs/CompactHUD/Preview.png",Folder+"/Loader.png",true);EditorSceneManager.OpenScene(SceneValidation.ScenePath);
   File.WriteAllText(Folder+"/Validation.txt","PASS: Unity "+Application.unityVersion+"; imported 14 original-sheet crops through catalogue with reusable Needs Review prefabs, ASTC Android sprites and preserved aspect. Saved/reopened responsive 5% safe-area HUD; forward/reverse pedals retained and stacked; direct -1/0/+1 blade selection; loader shows Dump in left blade slot with no blade-angle buttons. Persistent bindings, mode visibility, aspect preservation, all catalogue entries, repeat layout, scene validation and 19 snow checks passed. Native tractor/loader HUD previews rendered. Actual phone touch, safe-area devices and driving appearance remain untested. No snow, terrain, vehicle physics or player-save changes.");Debug.Log(File.ReadAllText(Folder+"/Validation.txt"));
  }
  static void Validate(PrototypeHUD hud){
   Check(hud.leftControls.anchorMin.x==.05f&&hud.rightControls.anchorMin.x==.95f,"Safe-area anchors");Check(hud.bladeAngleButtons.Length==3,"Three angle buttons");
   foreach(bool loader in new[]{false,true}){hud.RefreshEquipmentControls(loader);Check(hud.bladeAngleButtons.All(b=>b.activeSelf==!loader)&&hud.loaderDumpButton.activeSelf==loader,"Machine-specific visibility");}
   foreach(var c in hud.bladeAngleButtons.Select(b=>b.GetComponent<UICommand>())){Check(c.GetComponent<Button>().onClick.GetPersistentTarget(0)==c&&c.GetComponent<Button>().onClick.GetPersistentMethodName(0)=="Click","Angle listener");Check(new[]{"angle:-1","angle:0","angle:1"}.Contains(c.command),"Angle target");}
   foreach(var name in Icons){var a=CatalogPreparation.Library().Find("control-sheet-"+name);Check(a&&CatalogValidation.Check(a).Ok,"Catalogue validation "+name);Check(a.uiSprites.Single()==Icon(name),"Sprite binding "+name);}
   var controls=hud.driving.GetComponentsInChildren<TouchControl>(true);Check(controls.Count(t=>t.channel=="forward")==1&&controls.Count(t=>t.channel=="reverse")==1,"Directional pedals");
   foreach(var t in controls){var art=t.transform.Find("PSD kit artwork");if(art)Check(art.GetComponent<Image>().preserveAspect&&!art.GetComponent<Image>().raycastTarget,"Aspect/raycast");}
   var dump=(RectTransform)hud.loaderDumpButton.transform;var left=(RectTransform)hud.bladeAngleButtons[0].transform;Check(dump.anchoredPosition==left.anchoredPosition&&dump.parent==left.parent,"Dump left slot");hud.RefreshEquipmentControls(false);
  }
 }
}
