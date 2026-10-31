using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
namespace Snowglow.Editor {
 public static class PSDUIIntake {
  const string Source=CatalogPreparation.Root+"/Sources/SnowDistrictUI/",Request="Logs/PSDUI.request";
  [Serializable]public class Export {public string sha256;public int atlasWidth,atlasHeight;public Item[] buttons;}
  [Serializable]public class Item {public string name,slug;public int width,height,x,y;}
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){if(!File.Exists(Request)){EditorApplication.update-=Pending;return;}if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}if(EditorApplication.isPlayingOrWillChangePlaymode)return;EditorApplication.update-=Pending;File.Delete(Request);try{Install();}catch(Exception e){Directory.CreateDirectory("Logs/SnowDistrictUI");File.WriteAllText("Logs/SnowDistrictUI/Validation.txt","FAIL: "+e);Debug.LogException(e);}}
  static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException("PSD UI intake: "+message);}
  static Sprite SpriteFor(int id)=>CatalogPreparation.Library().Find("snow-district-ui-"+id.ToString("D2"))?.uiSprites.FirstOrDefault();
  static void Prepare(Export data){
   string path=Source+"ButtonsAtlas.png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=2048;importer.spritePixelsPerUnit=100;importer.npotScale=TextureImporterNPOTScale.None;
   importer.spritesheet=data.buttons.Select(i=>new SpriteMetaData{name=i.slug,rect=new Rect(i.x,i.y,i.width,i.height),pivot=new Vector2(.5f,.5f),alignment=(int)SpriteAlignment.Center}).ToArray();importer.SaveAndReimport();
   var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s=>s.name);Require(sprites.Count==35,"35 source sprites required");
   var library=CatalogPreparation.Library();
   foreach(var item in data.buttons.Take(32)){
    int number=int.Parse(item.slug.Substring(0,2));string id="snow-district-ui-"+number.ToString("D2");var a=library.Find(id);
    if(!a){a=ScriptableObject.CreateInstance<CatalogAsset>();a.id=id;AssetDatabase.CreateAsset(a,CatalogPreparation.Root+"/Entries/"+id+".asset");library.entries.Add(a);}
    a.displayName=item.name+" | Photoshop UI";a.kind=CatalogKind.UI;a.sourceType=AssetSource.PhotoshopDocument;a.status=PreparationStatus.NeedsReview;a.visualReviewed=false;
    a.sourcePath=path;a.sourceGuid=AssetDatabase.AssetPathToGUID(path);a.creationSourcePath="SourceAssets/SnowDistrictUI/Snow_District_UI_Buttons.psd";a.provenance="User-supplied layered Photoshop kit; SHA256 "+data.sha256+". Original retained unchanged. Raster layer names treated as data, not instructions.";
    a.tags=new[]{"ui","photoshop","snow-district-buttons"};a.blocksSnow=false;a.minimumScale=.25f;a.maximumScale=2;
    a.uiSprites=number==31?new[]{sprites[item.slug],sprites["play-normal"],sprites["play-pressed"],sprites["play-disabled"]}:new[]{sprites[item.slug]};
    a.assumptions="Canvas pixel dimensions, not world metres. Transparent raster artwork; labels remain separate. Only Play has source-provided Pressed/Disabled art; other states use Unity color feedback. Entries remain Needs Review; unavailable gameplay actions are not enabled.";
    var root=new GameObject(a.displayName,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));
    try{((RectTransform)root.transform).sizeDelta=new Vector2(item.width,item.height);root.GetComponent<Image>().sprite=a.uiSprites[0];root.GetComponent<Image>().preserveAspect=true;if(number==31){root.GetComponent<Image>().sprite=a.uiSprites[1];var play=root.GetComponent<Button>();play.transition=Selectable.Transition.SpriteSwap;play.spriteState=new SpriteState{highlightedSprite=a.uiSprites[1],selectedSprite=a.uiSprites[1],pressedSprite=a.uiSprites[2],disabledSprite=a.uiSprites[3]};}root.AddComponent<CatalogPlacement>().assetId=id;a.prefab=PrefabUtility.SaveAsPrefabAsset(root,CatalogPreparation.Root+"/Prefabs/"+id+".prefab");}
    finally{UnityEngine.Object.DestroyImmediate(root);}
    CatalogPreparation.RecordMetrics(a);var report=CatalogValidation.Check(a);Require(report.Ok,id+": "+report);EditorUtility.SetDirty(a);
   }
   EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
  }
  static void PrepareAbandon(){
   string path=Source+"AbandonJob.png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);Require(importer,"derived Abandon icon missing");
   importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=256;importer.spritePixelsPerUnit=100;importer.SaveAndReimport();
   var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);Require(sprite,"Abandon sprite import failed");var library=CatalogPreparation.Library();const string id="snow-district-ui-33";var a=library.Find(id);
   if(!a){a=ScriptableObject.CreateInstance<CatalogAsset>();a.id=id;AssetDatabase.CreateAsset(a,CatalogPreparation.Root+"/Entries/"+id+".asset");library.entries.Add(a);}
   a.displayName="Abandon Job | derived winter UI";a.kind=CatalogKind.UI;a.sourceType=AssetSource.Prompt;a.status=PreparationStatus.NeedsReview;a.visualReviewed=false;a.blocksSnow=false;a.uiSprites=new[]{sprite};a.minimumScale=.25f;a.maximumScale=2;
   a.sourcePath=path;a.sourceGuid=AssetDatabase.AssetPathToGUID(path);a.creationSourcePath="SourceAssets/SnowDistrictUI/Derived/AbandonJob-generated.png";a.tags=new[]{"ui","snow-district-buttons","derived","abandon-job"};
   a.provenance="Built-in imagegen edit of supplied PSD Career artwork: preserved snowy teal/ivory clipboard; replaced checkmark with amber X. Original PSD and 32 buttons unchanged. Exact prompt retained under SourceAssets/SnowDistrictUI/Derived.";
   a.assumptions="Canvas pixels. Imported at max 256px; transparent alpha. Menu Abandon action retains its text label and existing behavior. Needs visual/device review.";
   var root=new GameObject(a.displayName,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));try{((RectTransform)root.transform).sizeDelta=new Vector2(196,166);root.GetComponent<Image>().sprite=sprite;root.GetComponent<Image>().preserveAspect=true;root.AddComponent<CatalogPlacement>().assetId=id;a.prefab=PrefabUtility.SaveAsPrefabAsset(root,CatalogPreparation.Root+"/Prefabs/"+id+".prefab");}finally{UnityEngine.Object.DestroyImmediate(root);}
   CatalogPreparation.RecordMetrics(a);Require(CatalogValidation.Check(a).Ok,"derived Abandon entry invalid");EditorUtility.SetDirty(a);EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
  }
  static int Artwork(Button b){
   var t=b.GetComponent<TouchControl>();if(t){switch(t.channel){case "steerLeft":return 2;case "steerRight":return 3;case "throttle":return 4;case "brake":return 5;}}
   var c=b.GetComponent<UICommand>();if(!c)return 0;var key=c.command;
   if(key.StartsWith("switch:"))return 17;if(key.StartsWith("buy:"))return 18;if(key.StartsWith("job:"))return 25;
   switch(key){case "abandon":return 33;case "reverse":return 6;case "camera":case "camera:reset":return 7;case "pause":return 8;case "work":return 9;case "angle":return 10;case "dump":return 13;case "recover":case "recover:depot":return 15;case "sound":return 16;
    case "open:garage":return 17;case "open:shop":return 18;case "open:parts":return 19;case "tyres":case "engine":return 20;case "repair":case "open:service":return 21;case "fuel":return 22;case "open:settings":case "quality":case "assist":return 23;case "open:dashboard":return 24;case "open:contracts":return 25;case "free":case "fresh":return 27;case "continue":case "finish":return 31;default:return 0;}
  }
  public static void ApplyPrepared(PrototypeHUD hud){
   if(!SpriteFor(2))return;
   foreach(var button in hud.GetComponentsInChildren<Button>(true)){
    int art=Artwork(button);if(art==0)continue;var sprite=SpriteFor(art);Require(sprite,"mapped sprite missing");
    var child=button.transform.Find("PSD kit artwork");if(!child)child=new GameObject("PSD kit artwork",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).transform;
    child.SetParent(button.transform,false);child.SetAsFirstSibling();var image=child.GetComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;image.color=Color.white;
    var background=button.GetComponent<Image>();background.color=Color.clear;background.raycastTarget=true;button.targetGraphic=image;button.transition=Selectable.Transition.ColorTint;
    var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=Color.white;colors.pressedColor=new Color(.73f,.85f,.9f,1);colors.selectedColor=Color.white;colors.disabledColor=new Color(.6f,.65f,.7f,.5f);button.colors=colors;
    var size=((RectTransform)button.transform).sizeDelta;var r=(RectTransform)child;bool tall=size.y>=90;
    r.anchorMin=r.anchorMax=tall?new Vector2(.5f,.61f):new Vector2(0,.5f);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=tall?Vector2.zero:new Vector2(28,0);r.sizeDelta=tall?new Vector2(size.x-4,size.y-26):new Vector2(54,size.y-2);
    var label=button.GetComponentInChildren<Text>(true);if(label){label.gameObject.SetActive(true);var lr=label.rectTransform;lr.anchorMin=Vector2.zero;lr.anchorMax=tall?new Vector2(1,0):Vector2.one;
     if(tall){lr.pivot=new Vector2(.5f,.5f);lr.anchoredPosition=new Vector2(0,12);lr.sizeDelta=new Vector2(size.x,22);}else{lr.offsetMin=new Vector2(58,3);lr.offsetMax=new Vector2(-3,-3);}
     label.color=new Color(.09f,.21f,.25f);label.fontSize=14;label.resizeTextForBestFit=true;label.resizeTextMinSize=11;label.resizeTextMaxSize=14;
     var outline=label.GetComponent<Outline>();if(!outline)outline=label.gameObject.AddComponent<Outline>();outline.effectColor=new Color(1,1,1,.8f);outline.effectDistance=new Vector2(.6f,-.6f);
    }
    if(button.transform.IsChildOf(hud.driving.transform)){
     if(label)label.gameObject.SetActive(false);
     const float aspect=196f/166f;float h=Mathf.Min(size.y-4,(size.x-4)/aspect);
     r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(h*aspect,h);
    }
    if(art==31){var states=CatalogPreparation.Library().Find("snow-district-ui-31").uiSprites;image.sprite=states[1];button.transition=Selectable.Transition.SpriteSwap;button.spriteState=new SpriteState{highlightedSprite=states[1],selectedSprite=states[1],pressedSprite=states[2],disabledSprite=states[3]};}
    if(art==9){hud.workSkinImage=image;hud.plowWorkSprite=SpriteFor(9);hud.loaderWorkSprite=SpriteFor(11);}
    EditorUtility.SetDirty(button);
   }
   EditorUtility.SetDirty(hud);
  }
  [MenuItem("Snowglow/Asset intake/Import Photoshop UI button kit")]
  public static void Install(){
   Require(!EditorApplication.isPlayingOrWillChangePlaymode,"stop Play first");var scene=SceneManager.GetActiveScene();Require(scene.path==SceneValidation.ScenePath,"open Pinefall first");Directory.CreateDirectory("Logs/SnowDistrictUI");if(!File.Exists("Logs/SnowDistrictUI/Pinefall-before.unity"))File.Copy(scene.path,"Logs/SnowDistrictUI/Pinefall-before.unity");
   var session=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PrototypeSession>()).Single();string seed=AssetDatabase.GetAssetPath(session.Snow.seed),hash=AssetDatabase.GetAssetDependencyHash(seed).ToString();
   var ids=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CatalogPlacement>(true)).Where(p=>!string.IsNullOrEmpty(p.placementId)).ToDictionary(p=>p.placementId,p=>p.assetId+"|"+p.transform.localToWorldMatrix.ToString("R"));
   var manifest=JsonUtility.FromJson<Export>(File.ReadAllText(Source+"ExportManifest.json"));Prepare(manifest);PrepareAbandon();DrivingButtonsAuthoring.Apply(session.HUD);ApplyPrepared(session.HUD);SceneValidation.Validate(scene);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();Require(EditorSceneManager.SaveScene(scene),"save failed");
   scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);SceneValidation.Validate(scene);session=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PrototypeSession>()).Single();var library=CatalogPreparation.Library();
   for(int i=1;i<=33;i++){var a=library.Find("snow-district-ui-"+i.ToString("D2"));Require(a&&a.kind==CatalogKind.UI&&CatalogValidation.Check(a).Ok,"saved UI entry invalid");}
   foreach(var p in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CatalogPlacement>(true)).Where(p=>!string.IsNullOrEmpty(p.placementId)))Require(ids[p.placementId]==p.assetId+"|"+p.transform.localToWorldMatrix.ToString("R"),"world placement changed");Require(hash==AssetDatabase.GetAssetDependencyHash(seed).ToString(),"snow seed changed");
   var art=session.HUD.GetComponentsInChildren<Image>(true).Where(i=>i.name=="PSD kit artwork").ToArray();Require(art.Length>=11&&art.All(i=>i.sprite&&!i.raycastTarget),"saved UI artwork missing");Require(!session.HUD.driving.GetComponentsInChildren<UICommand>(true).Any(c=>c.command=="auto"||c.command=="camera:in"||c.command=="camera:out"),"removed controls restored");
   foreach(var button in session.HUD.driving.GetComponentsInChildren<Button>(true)){
    var label=button.GetComponentInChildren<Text>(true);Require(!label||!label.gameObject.activeSelf,"driving button label still visible");
    var icon=button.transform.Find("PSD kit artwork");if(icon){var size=((RectTransform)icon).sizeDelta;Require(Mathf.Abs(size.x/size.y-196f/166f)<.001f,"inconsistent driving icon aspect");}
   }
   var abandon=session.HUD.GetComponentsInChildren<UICommand>(true).Single(c=>c.command=="abandon");Require(abandon.GetComponentInChildren<Text>(true).gameObject.activeSelf&&abandon.transform.Find("PSD kit artwork"),"Abandon menu icon/label missing");
   typeof(CompactHUDAuthoring).GetMethod("Preview",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{session.HUD});File.Copy("Logs/CompactHUD/Preview.png","Logs/SnowDistrictUI/Driving-preview.png",true);EditorSceneManager.OpenScene(SceneValidation.ScenePath);
   File.WriteAllText("Logs/SnowDistrictUI/Validation.txt","PASS: Unity "+Application.unityVersion+"; 33 stable UI catalog entries/prefabs; 35 PSD sprites plus one derived Abandon sprite. Icon-only driving buttons share 196:166 artwork ratio; menu labels retained. Source PSD preserved by SHA256. Saved/reopened "+art.Length+" matched UI buttons, source Play states, dynamic authored loader/plow Work artwork; world IDs/poses and snow seed unchanged. Catalog/scene validation passed. Entries remain Needs Review. Native driving UI preview rendered. No removed controls or future systems enabled; physical touch/phone performance untested.");Debug.Log(File.ReadAllText("Logs/SnowDistrictUI/Validation.txt"));
  }
 }
}
