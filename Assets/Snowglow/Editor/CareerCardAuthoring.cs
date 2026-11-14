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
 public static class CareerCardAuthoring {
  public const string Folder="Logs/CareerCards",Art=ProductionMenuAuthoring.ArtFolder;
  static readonly string[] Regions={"Mountain_Alpine","City_Urban","Highway_Open_Country","Suburban_Commercial","Industrial_Extreme_Weather"};
  static CareerCardAuthoring(){EditorApplication.update+=Pending;}
  static void Pending(){
   if(!File.Exists(Folder+".request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
   File.Delete(Folder+".request");
   try{Install();}catch(Exception e){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/Validation.txt","FAIL: "+e);Debug.LogException(e);}
  }
  static void Import(string path){
   AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);
   if(!importer)throw new InvalidOperationException("Missing career card art: "+path);
   importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
   importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=1024;
   importer.wrapMode=TextureWrapMode.Clamp;importer.alphaIsTransparency=false;
   var android=importer.GetPlatformTextureSettings("Android");android.name="Android";android.overridden=true;
   android.maxTextureSize=1024;android.format=TextureImporterFormat.ASTC_6x6;importer.SetPlatformTextureSettings(android);importer.SaveAndReimport();
  }
  public static void PrepareArt(){
   foreach(string region in Regions)Import(Art+"/Regions/"+region+".png");
   foreach(string id in CareerProgression.TownIds.SelectMany(r=>r).Where(id=>id!="pinefall"))Import(Art+"/Towns/"+id+".jpg");
  }
  static RectTransform Rect(string name,Transform parent,float x,float y,float width,float height){
   var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);
   var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
   rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);return rect;
  }
  public static void ApplyIfPrepared(PrototypeHUD hud){if(AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/Towns/alps.jpg"))Apply(hud);}
  public static void Apply(PrototypeHUD hud){
   var ui=hud.productionMenu;var gallery=ui.GetComponent<CareerCardGallery>();if(!gallery)gallery=ui.gameObject.AddComponent<CareerCardGallery>();
   ui.careerGallery=gallery;gallery.menu=ui;gallery.regionBorders=new Outline[5];gallery.townPictures=new Image[4];
   gallery.destinationArt=CareerProgression.TownIds.SelectMany(r=>r).Select(id=>AssetDatabase.LoadAssetAtPath<Sprite>(id=="pinefall"?Art+"/pinefall.jpg":Art+"/Towns/"+id+".jpg")).ToArray();
   if(gallery.destinationArt.Any(s=>!s))throw new InvalidOperationException("Incomplete destination artwork.");
   for(int i=0;i<5;i++){
    var button=ui.Button("region-"+i);var image=button.GetComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/Regions/"+Regions[i]+".png");image.color=Color.white;
    var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.97f,.83f);button.colors=colors;
    var old=button.transform.Find("Region label scrim");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
    var scrim=Rect("Region label scrim",button.transform,0,72,382,48).gameObject.AddComponent<Image>();scrim.color=new Color(.035f,.12f,.19f,.88f);scrim.raycastTarget=false;
    var text=ui.Label("region-"+i);text.transform.SetAsLastSibling();text.rectTransform.anchoredPosition=new Vector2(8,-73);text.rectTransform.sizeDelta=new Vector2(366,45);text.fontSize=16;text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;
    var border=button.GetComponent<Outline>();if(!border)border=button.gameObject.AddComponent<Outline>();border.effectColor=new Color(1,.74f,.09f);border.effectDistance=new Vector2(2,-2);border.enabled=i==0;gallery.regionBorders[i]=border;
   }
   for(int i=0;i<4;i++){
    var button=ui.Button("town-"+i);button.GetComponent<Image>().color=Color.white;
    var old=button.transform.Find("Destination artwork");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
    var picture=Rect("Destination artwork",button.transform,0,0,280.75f,136.581f).gameObject.AddComponent<Image>();picture.raycastTarget=false;picture.color=Color.white;picture.preserveAspect=false;picture.transform.SetAsFirstSibling();gallery.townPictures[i]=picture;
    var label=ui.Label("town-"+i);label.fontSize=17;label.alignment=TextAnchor.UpperLeft;label.color=new Color(.08f,.18f,.26f);
    var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.96f,.8f);colors.disabledColor=new Color(.86f,.89f,.91f);button.colors=colors;
   }
   gallery.Refresh(0);EditorUtility.SetDirty(gallery);EditorUtility.SetDirty(ui);
  }
  public static void Validate(PrototypeHUD hud){
   var ui=hud.productionMenu;var gallery=ui.careerGallery;
   if(!gallery||gallery.destinationArt.Length!=16||gallery.destinationArt.Any(s=>!s)||gallery.townPictures.Length!=4||gallery.regionBorders.Length!=5)throw new InvalidOperationException("Career card bindings incomplete.");
   for(int i=0;i<5;i++)if(!ui.Button("region-"+i).GetComponent<Image>().sprite||ui.Label("region-"+i).color!=Color.white)throw new InvalidOperationException("Region card appearance missing.");
   for(int region=0;region<5;region++){
    gallery.Refresh(region);int count=CareerProgression.TownIds[region].Length;
    for(int i=0;i<4;i++){
     var card=ui.Button("town-"+i).GetComponent<RectTransform>();
     if(card.gameObject.activeSelf!=(i<count))throw new InvalidOperationException("Destination count mismatch.");
     if(i>=count)continue;
     var picture=gallery.townPictures[i].rectTransform;
     if(!gallery.townPictures[i].sprite||-card.anchoredPosition.y+card.sizeDelta.y>564||card.anchoredPosition.x+card.sizeDelta.x>1236.1f||Mathf.Abs(picture.sizeDelta.x/picture.sizeDelta.y-2960f/1440)>.001f)throw new InvalidOperationException("Town card overflow/aspect.");
    }
   }
   gallery.Refresh(0);ProductionMenuChecks.ValidateAuthored(hud);
  }
  static PrototypeHUD Hud(Scene scene)=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PrototypeHUD>(true)).Single();
  [MenuItem("Snowglow/UI/Install region and destination card artwork")]
  public static void Install(){
   var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=SceneValidation.ScenePath)throw new InvalidOperationException("Open Pinefall with Play stopped.");
   Directory.CreateDirectory(Folder);File.Copy(scene.path,Folder+"/Pinefall-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");
   if(scene.isDirty&&!EditorSceneManager.SaveScene(scene,Folder+"/Pinefall-unsaved-before.unity",true))throw new IOException("Unsaved backup failed.");
   PrepareArt();var hud=Hud(scene);Apply(hud);Validate(hud);SceneValidation.Validate(scene);
   EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed.");AssetDatabase.SaveAssets();
   scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);hud=Hud(scene);Validate(hud);ProductionMenuChecks.Run(hud);
   scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);SceneValidation.Validate(scene);
   File.WriteAllText(Folder+"/Validation.txt","PASS: five supplied region cards and fifteen individually generated town illustrations imported and saved/reopened; sixteen destination sprites include existing Pinefall capture. Region labels/actions and town lock/playable gates retained. Three/four-card layouts fit the existing page with consistent 2.056:1 image areas. Native menu/flow checks and image/geometry/binding validation passed. No Play entry or player-save writes. Android phone acceptance remains separate.");
  }
 }
}
