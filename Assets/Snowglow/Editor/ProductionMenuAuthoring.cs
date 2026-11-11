using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace Snowglow.Editor {
 // Builds only in Edit mode. Runtime binds and toggles this saved hierarchy.
 public static class ProductionMenuAuthoring {
  public const string ArtFolder="Assets/Snowglow/UI/Production",Folder="Logs/ProductionUI";
  const string Request="Logs/ProductionUI.request";
  static readonly Color Navy=new Color(.063f,.17f,.25f),Paper=new Color(.94f,.965f,.98f),Ink=new Color(.08f,.18f,.26f),Muted=new Color(.35f,.44f,.5f),Yellow=new Color(1,.74f,.09f),Line=new Color(.85f,.9f,.93f);
  static ProductionMenu ui;static Font font;static List<MenuTextBinding> labels;static List<MenuButtonBinding> buttons;
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete(Request);EditorApplication.update-=Pending;try{Install();}catch(Exception e){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/Validation.txt","FAIL: "+e);Debug.LogException(e);}}
  static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h){var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
  static Image Fill(RectTransform r,Color color){var im=r.gameObject.AddComponent<Image>();im.color=color;return im;}
  static Text Text(Transform parent,string text,float x,float y,float w,float h,int size=20,Color? color=null,string key=null){var r=Rect(key??text.Split('\n')[0],parent,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.text=text;t.color=color??Ink;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;if(key!=null)labels.Add(new MenuTextBinding{key=key,text=t});return t;}
  static Button Button(Transform parent,string label,string action,float x,float y,float w=220,float h=48,bool primary=false,string key=null){var r=Rect(key??label,parent,x,y,w,h);var image=Fill(r,primary?Yellow:Line);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;var colors=b.colors;colors.highlightedColor=new Color(.9f,.95f,1);colors.pressedColor=new Color(.72f,.82f,.9f);colors.disabledColor=new Color(.7f,.73f,.76f,.65f);b.colors=colors;var t=Text(r,label,10,5,w-20,h-10,18,Ink,key);t.alignment=TextAnchor.MiddleCenter;var a=r.gameObject.AddComponent<ProductionMenuAction>();a.menu=ui;a.command=action;UnityEventTools.AddPersistentListener(b.onClick,a.Click);if(key!=null)buttons.Add(new MenuButtonBinding{key=key,button=b});return b;}
  static Image Art(Transform parent,string name,float x,float y,float w,float h){var r=Rect("Game capture | "+name,parent,x,y,w,h);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x+w*.5f,-y-h*.5f);var im=Fill(r,Color.white);im.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder+"/"+name+".jpg");im.preserveAspect=true;im.raycastTarget=false;return im;}
  static void Title(Transform parent,string eyebrow,string title,string subtitle=""){Text(parent,eyebrow,44,100,1160,24,15,Muted);var h=Text(parent,title,44,130,1160,48,34);h.fontStyle=FontStyle.Bold;if(subtitle!="")Text(parent,subtitle,44,184,1160,42,18,Muted);}
  static GameObject Group(Transform parent,string name,float x,float y,float w,float h)=>Rect(name,parent,x,y,w,h).gameObject;
  static void PrepareArt(){Directory.CreateDirectory(ArtFolder);foreach(string name in new[]{"pinefall","quick","garage","tractor","loader","mountains"}){string to=ArtFolder+"/"+name+".jpg";if(!File.Exists(to))File.Copy("work/ui-preview/"+name+".jpg",to);AssetDatabase.ImportAsset(to,ImportAssetOptions.ForceSynchronousImport);var imp=(TextureImporter)AssetImporter.GetAtPath(to);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.mipmapEnabled=false;imp.npotScale=TextureImporterNPOTScale.None;imp.maxTextureSize=1024;imp.wrapMode=TextureWrapMode.Clamp;imp.alphaIsTransparency=false;var android=imp.GetPlatformTextureSettings("Android");android.name="Android";android.overridden=true;android.maxTextureSize=1024;android.format=TextureImporterFormat.ASTC_6x6;imp.SetPlatformTextureSettings(android);imp.SaveAndReimport();}}
  public static void ApplyIfPrepared(PrototypeHUD hud){if(AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder+"/tractor.jpg"))Apply(hud);}
  public static void Apply(PrototypeHUD hud){
   font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");labels=new List<MenuTextBinding>();buttons=new List<MenuButtonBinding>();
   var previous=hud.layout.Find("Production menus | saved before Play");if(previous){var priorMenu=previous.GetComponent<ProductionMenu>();if(priorMenu&&priorMenu.workshopPreview)UnityEngine.Object.DestroyImmediate(priorMenu.workshopPreview.gameObject);UnityEngine.Object.DestroyImmediate(previous.gameObject);}
   var old=hud.layout.Find("Menu layer");if(old)old.gameObject.SetActive(false);
   var root=Rect("Production menus | saved before Play",hud.layout,0,0,1280,720);Fill(root,Paper);ui=root.gameObject.AddComponent<ProductionMenu>();hud.productionMenu=ui;hud.menuRoot=root.gameObject;
   var content=Rect("Screen content",root,0,0,1280,720);ui.contentGroup=content.gameObject.AddComponent<CanvasGroup>();
   var header=Rect("Navy header",content,0,0,1280,78);Fill(header,Navy);var logo=Text(header,"SNOW DISTRICT",44,14,470,36,30,Color.white);logo.fontStyle=FontStyle.Bold;Text(header,"WINTER OPERATIONS",46,51,440,20,12,new Color(.78f,.87f,.93f));Text(header,"1,800 coins",780,27,210,35,21,Yellow,"wallet");Button(content,"Settings","nav:settings",1100,90,132,42,false,"global-settings");
   var names=new[]{"dashboard","regions","towns","contracts","machinery","garage","settings","pause","results","store"};var pages=new List<GameObject>();
   foreach(var name in names){var page=Rect(name,content,0,78,1280,564);pages.Add(page.gameObject);if(name!="dashboard"&&name!="results")Button(page,name=="store"?"< Back to Shop":name=="pause"||name=="results"?"< Main Menu":"< Back",name=="store"?"nav:shop":name=="pause"||name=="results"?"nav:dashboard":"back",44,12,175,42,false,name=="store"?"store-back":"back-"+name);
    switch(name){
     case "dashboard":
      Title(page,"YOUR NEXT SHIFT STARTS HERE","A whole town. A fresh snowfall.");
      string[] cardNames={"Quick Job","Career","Garage"},cardArt={"quick","pinefall","garage"},actions={"quick","career","nav:garage"},descs={"Get straight to the snow.","Build your winter operations.","Your machines. Ready for the next shift."};
      for(int i=0;i<3;i++){float x=44+i*405;var card=Rect(cardNames[i],page,x,209,382,316);Fill(card,Color.white);Art(card,cardArt[i],0,0,382,175);Text(card,cardNames[i],18,185,340,35,25);Text(card,descs[i],18,224,346,30,17,Muted);Button(card,i==0?"Start Plowing":i==1?"View Career":"Open Garage",actions[i],18,260,346,44,true);}
      break;
     case "regions":
      Title(page,"CAREER","Choose your region","Pinefall is available. Other destinations are planned.");
      for(int i=0;i<5;i++){float x=44+(i%3)*405,y=235+(i/3)*142;Button(page,ProductionMenu.RegionNames[i]+"\n"+(i==0?"PINEFALL AVAILABLE":"PLANNED LOCATIONS"),"region:"+i,x,y,382,120,i==0,"region-"+i);}
      break;
     case "towns":
      Title(page,"CAREER / DESTINATIONS","Choose your town","Only Pinefall is playable. Destination names are plans, not new districts.");Text(page,"Mountain & Alpine",44,220,1150,34,24,Ink,"town-title");
      for(int i=0;i<4;i++)Button(page,"Town","town:"+i,44+(i%2)*608,275+(i/2)*115,584,92,i==0,"town-"+i);
      break;
     case "contracts":
      Title(page,"PINEFALL","Choose your job","Choose a job to start with your selected vehicle. Change machinery and fit parts in Garage.");Text(page,"CAREER / ALPINE / PINEFALL",44,220,640,25,15,Muted,"job-mode");
      var jn=new[]{"Depot Access","Parking Lot","Main Street"};var jd=new[]{"X −65 / Z −90\n12 minute deadline","X 94 / Z 21\n15 minute deadline","X 0 / Z 0\n15 minute deadline"};ui.jobCards=new GameObject[3];for(int i=0;i<3;i++){float x=44+i*405;var card=Group(page,"Job slot "+i,x,260,382,253);ui.jobCards[i]=card;Art(card.transform,i==0?"quick":"pinefall",0,0,382,150);Button(card.transform,jn[i]+"\n"+jd[i],"job:"+i,0,156,382,97,true,"job-"+i);}
      Button(page,"Free Work","free",916,12,172,42);Button(page,"Garage","nav:garage",730,12,172,42);
      ui.quickTools=Group(page,"Quick Play shuffle",700,216,532,40);
      Button(ui.quickTools.transform,"Shuffle","shuffle",0,0,150,36);Button(ui.quickTools.transform,"<","quick-page:-1",164,0,50,36,false,"quick-prev");Text(ui.quickTools.transform,"1 / 1",226,6,80,28,17,Muted,"quick-page");Button(ui.quickTools.transform,">","quick-page:1",316,0,50,36,false,"quick-next");
      break;
     case "machinery":
      Title(page,"PINEFALL / JOB PREPARATION","Choose your machinery","Swipe left or right to find your machine.");
      ui.machinePicture=Art(page,"tractor",240,210,800,190);ui.machinePicture.raycastTarget=true;var swipe=ui.machinePicture.gameObject.AddComponent<MachinerySwipe>();swipe.menu=ui;
      Button(page,"‹","browse:-1",140,310,68,68);Button(page,"›","browse:1",1072,310,68,68);Fill(Rect("Ownership badge",page,240,210,345,32),Paper);Text(page,"OWNED",254,214,320,30,17,Ink,"machine-owned");Text(page,"Golden Snow Tractor",240,410,800,36,28,Ink,"machine-name");Text(page,"Front plow",240,452,730,28,18,Muted,"machine-kind");
      Text(page,"1 / 2 · Swipe to browse",510,489,390,25,17,Muted,"machine-position");Button(page,"View Shop","nav:shop",1040,438,192,45,false,"machine-shop");break;
     case "garage":
      Text(page,"PINEFALL WORKSHOP",44,143,1160,36,28);
      var garageTab=Button(page,"Garage","workshop:garage",44,68,230,46,true);var shopTab=Button(page,"Shop","workshop:shop",286,68,230,46);ui.tabImages=new[]{garageTab.GetComponent<Image>(),shopTab.GetComponent<Image>()};ui.selectedColor=Yellow;ui.tabColor=Line;
      // Three columns follow the supplied sketches: stats / preview / upgrades or description.
      Text(page,"VEHICLE STATS",44,229,245,220,19,Ink,"garage-status");
      Text(page,"Golden Snow Tractor",326,208,628,36,25,Ink,"garage-name").alignment=TextAnchor.MiddleCenter;
      ui.workshopPreview=WorkshopPreviewAuthoring.Build(root,UnityEngine.Object.FindFirstObjectByType<PrototypeSession>().Fleet);
      var previewRect=Rect("3D vehicle and part view",page,356,250,568,212);var raw=previewRect.gameObject.AddComponent<RawImage>();raw.texture=ui.workshopPreview.previewCamera.targetTexture;var workshopSwipe=previewRect.gameObject.AddComponent<MachinerySwipe>();workshopSwipe.menu=ui;
      Button(page,"<","browse:-1",278,320,64,64);Button(page,">","browse:1",938,320,64,64);
      Text(page,"OWNED",356,465,568,25,16,Muted,"garage-owned").alignment=TextAnchor.MiddleCenter;Text(page,"1 / 2",356,493,568,23,15,Muted,"workshop-position").alignment=TextAnchor.MiddleCenter;
      Text(page,"AT DEPOT",44,187,1190,24,16,Muted,"workshop-location");
      ui.garageContent=Group(page,"Garage tab",0,0,1280,564);
      ui.overviewContent=Group(ui.garageContent.transform,"Legacy overview",0,0,1,1);
      ui.serviceContent=Group(ui.garageContent.transform,"Service",44,419,220,105);
      Button(ui.serviceContent.transform,"Refuel","confirm:fuel",0,0,220,32,false,"fuel");Button(ui.serviceContent.transform,"Repair","confirm:repair",0,35,220,32,false,"repair");Button(ui.serviceContent.transform,"Recover to Depot","confirm:recover",0,70,220,32);
      ui.upgradesContent=Group(ui.garageContent.transform,"Upgrades",1022,229,210,260);
      Text(ui.upgradesContent.transform,"UPGRADES",0,0,210,30,20);
      Button(ui.upgradesContent.transform,"Tyres","confirm:tyres",0,42,210,57,false,"tyres");Text(ui.upgradesContent.transform,"+12% grip per level",0,105,210,28,16,Muted);
      Button(ui.upgradesContent.transform,"Engine","confirm:engine",0,143,210,57,false,"engine");Text(ui.upgradesContent.transform,"+18% torque per level",0,206,210,28,16,Muted);
      Button(ui.garageContent.transform,"Select & Drive","drive",356,518,278,42,true,"drive");Button(ui.garageContent.transform,"Buy Vehicle","buy-vehicle",646,518,278,42,false,"vehicle-buy");
      ui.shopContent=Group(page,"Shop tab",0,0,1280,564);Button(ui.shopContent.transform,"Test Transactions","nav:store",846,12,242,42,false,"shop-transactions");Text(ui.shopContent.transform,"Part details",1022,229,210,270,17,Ink,"part-description");Button(ui.shopContent.transform,"Buy & Fit","buy-part",356,518,568,42,true,"part-buy");
      ui.upgradeSegments=new Image[6];for(int i=0;i<6;i++)ui.upgradeSegments[i]=Fill(Rect("Upgrade level "+i,ui.upgradesContent.transform,(i%3)*70,i<3?133:234,64,8),Line);
      ui.garageTabImages=new Image[0];break;
     case "settings":
      Title(page,"PREFERENCES","Make yourself comfortable.");
      ui.settingsTabImages=new Image[3];for(int i=0;i<3;i++)ui.settingsTabImages[i]=Button(page,new[]{"Audio","Graphics","Controls"}[i],"settings-tab:"+i,44,222+i*64,250,48,i==0).GetComponent<Image>();
      ui.settingPages=new GameObject[3];for(int i=0;i<3;i++)ui.settingPages[i]=Group(page,"Settings "+i,340,222,892,300);
      Button(ui.settingPages[0].transform,"Sound: ON","sound",0,0,600,56,false,"settings-sound");Text(ui.settingPages[0].transform,"Engine, work layers, reverse warning and effects use the existing sound setting.",0,82,840,100,20,Muted);
      Button(ui.settingPages[1].transform,"Graphics: STANDARD","quality",0,0,600,56,false,"settings-quality");Button(ui.settingPages[1].transform,"Physics overlay: OFF","debug",0,80,600,56,false,"settings-debug");Text(ui.settingPages[1].transform,"Galaxy A07 reference · 30 FPS target\nUse the STATS overlay to measure actual phone performance.",0,160,840,80,20,Muted);
      Button(ui.settingPages[2].transform,"Driving assists: ON","assist",0,0,600,56,false,"settings-assist");Text(ui.settingPages[2].transform,"W / ↑ Forward · S / ↓ Reverse · A/D Steering · Space Brake\nE Work / Carry · Q Blade angle · B Dump · C Camera\nOrbit: right drag · wheel zoom · Home reset\nTouch uses the existing directional pedals and steering buttons.",0,82,850,170,20,Muted);break;
     case "pause":
      Title(page,"PINEFALL","Take a breather.","Your shift is paused. Your machine and snow will be waiting.");Text(page,"PINEFALL / JOB",44,240,660,40,23,Ink,"pause-job");
      string[] pl={"Resume Job","Garage / Shop","Settings","Recover to Depot","Abandon Job","Finish Free Work"},pa={"continue","nav:garage","nav:settings","confirm:recover","confirm:abandon","confirm:finish"};for(int i=0;i<6;i++)Button(page,pl[i],pa[i],800,105+i*71,432,54,i==0,i==5?"finish":null);break;
     case "results":
      Title(page,"PINEFALL / SHIFT REPORT","A job well done.");Text(page,"Shift complete",80,225,1090,230,26,Ink,"results");Button(page,"Next Job","next-job",858,470,374,54,true,"next-job");Button(page,"Replay","replay",44,470,374,54,false,"replay");Button(page,"Main Menu","nav:dashboard",451,470,374,54,false,"results-home");break;
     case "store":
      Title(page,"PROTOTYPE CURRENCY","Simulated store","No real money. Uses the existing duplicate-safe test transaction flow.");Button(page,"New transaction","dummy:new",44,270,360);Button(page,"Success / redeliver","dummy:success",454,270,360);Button(page,"Cancel","dummy:cancel",864,270,360);Button(page,"Fail","dummy:fail",44,350,360);break;
    }
    page.gameObject.SetActive(name=="dashboard");
   }
   // Shared bottom band. Job-specific footer appears only on the selector.
   var footer=Rect("Footer",content,0,642,1280,78);Fill(footer,Line);Text(footer,"Welcome to Pinefall.",44,10,890,48,17,Muted,"notice");Text(footer,"LEVEL 1 · 0 XP",44,57,800,19,13,Muted,"level");Button(footer,"Resume Job","continue",1000,14,232,48,false,"resume-footer");
   var selector=pages[names.ToList().IndexOf("machinery")];Text(selector.transform,"Depot Access",44,530,850,32,18,Ink,"machine-job");Button(selector.transform,"Start Job","start",1000,508,232,48,true,"start");
   var modal=Rect("Confirmation overlay",root,0,0,1280,720);Fill(modal,new Color(.03f,.09f,.14f,.88f));ui.dialog=modal.gameObject;var dialog=Rect("Dialog",modal,300,174,680,372);Fill(dialog,Paper);Text(dialog,"Confirm",30,28,530,65,28,Ink,"dialog-title");Text(dialog,"Details",30,107,620,135,20,Muted,"dialog-body");Text(dialog,"Cost",30,220,620,40,22,Ink,"dialog-price");Button(dialog,"Cancel","dialog-secondary",30,290,280,50,false,"cancel");Button(dialog,"Confirm","accept",330,290,320,50,true,"accept");Button(dialog,"\u00D7","cancel",596,16,52,44,false,"dialog-close");modal.gameObject.SetActive(false);
   ui.pages=pages.ToArray();ui.pageNames=names;ui.labels=labels.ToArray();ui.buttons=buttons.ToArray();ui.vehiclePictures=new[]{AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder+"/tractor.jpg"),AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder+"/loader.jpg")};EditorUtility.SetDirty(hud);EditorUtility.SetDirty(ui);
  }
  public static void Batch(){EditorSceneManager.OpenScene(SceneValidation.ScenePath);Install();}
  [MenuItem("Snowglow/UI/Install reviewed production menus")]
  public static void Install(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=SceneValidation.ScenePath)throw new InvalidOperationException("Open Pinefall");Directory.CreateDirectory(Folder);
   File.Copy(scene.path,Folder+"/Pinefall-before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");if(scene.isDirty)EditorSceneManager.SaveScene(scene,Folder+"/Pinefall-unsaved-before.unity",true);
   SceneValidation.Validate(scene);PrepareArt();var hud=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<PrototypeHUD>(true)).Single();Apply(hud);ProductionMenuChecks.ValidateAuthored(hud);SceneValidation.Validate(scene);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed");AssetDatabase.SaveAssets();scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);hud=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<PrototypeHUD>(true)).Single();ProductionMenuChecks.Run(hud);scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);SceneValidation.Validate(scene);MechanicsChecks.Run();File.WriteAllText(Folder+"/Validation.txt","PASS: Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+". Production menus authored, saved/reopened. Native routing/ownership/quotes/confirmations/swipe/pause/layout checks, scene validation and 19 snow checks passed. Existing controls and live economy retained. No district rebake or player-save writes. Live Play, Android packaging and Galaxy A07 touch/performance remain untested.");
  }
 }
}
