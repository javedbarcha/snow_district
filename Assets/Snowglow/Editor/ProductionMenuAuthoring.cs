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
   var previous=hud.layout.Find("Production menus | saved before Play");if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
   var old=hud.layout.Find("Menu layer");if(old)old.gameObject.SetActive(false);
   var root=Rect("Production menus | saved before Play",hud.layout,0,0,1280,720);Fill(root,Paper);ui=root.gameObject.AddComponent<ProductionMenu>();hud.productionMenu=ui;hud.menuRoot=root.gameObject;
   var content=Rect("Screen content",root,0,0,1280,720);ui.contentGroup=content.gameObject.AddComponent<CanvasGroup>();
   var header=Rect("Navy header",content,0,0,1280,78);Fill(header,Navy);var logo=Text(header,"SNOW DISTRICT",44,14,470,36,30,Color.white);logo.fontStyle=FontStyle.Bold;Text(header,"WINTER OPERATIONS",46,51,440,20,12,new Color(.78f,.87f,.93f));Text(header,"1,800 coins",780,27,210,35,21,Yellow,"wallet");Button(header,"Settings","nav:settings",1000,16,112,46);
   var names=new[]{"dashboard","regions","towns","contracts","machinery","garage","settings","pause","results","store"};var pages=new List<GameObject>();
   foreach(var name in names){var page=Rect(name,content,0,78,1280,564);pages.Add(page.gameObject);if(name!="dashboard")Button(page,"‹  Main Menu","nav:dashboard",44,12,175,42);
    switch(name){
     case "dashboard":
      Title(page,"YOUR NEXT SHIFT STARTS HERE","A whole town. A fresh snowfall.");Button(page,"Continue Job","continue",1000,116,232,48);
      string[] cardNames={"Quick Job","Career","Garage"},cardArt={"quick","pinefall","garage"},actions={"quick","career","nav:garage"},descs={"Get straight to the snow.","Build your winter operations.","Your machines. Ready for the next shift."};
      for(int i=0;i<3;i++){float x=44+i*405;var card=Rect(cardNames[i],page,x,209,382,316);Fill(card,Color.white);Art(card,cardArt[i],0,0,382,175);Text(card,cardNames[i],18,185,340,35,25);Text(card,descs[i],18,224,346,30,17,Muted);Button(card,i==0?"Start Plowing":i==1?"View Career":"Open Garage",actions[i],18,260,346,44,true);}
      break;
     case "regions":
      Title(page,"CAREER","Choose your region","Pinefall is available. Other destinations are planned.");
      for(int i=0;i<5;i++){float x=44+(i%3)*405,y=235+(i/3)*142;Button(page,ProductionMenu.RegionNames[i]+"\n"+(i==0?"PINEFALL AVAILABLE":"PLANNED LOCATIONS"),"region:"+i,x,y,382,120,i==0);}
      break;
     case "towns":
      Title(page,"CAREER / DESTINATIONS","Choose your town","Only Pinefall is playable. Destination names are plans, not new districts.");Text(page,"Mountain & Alpine",44,220,1150,34,24,Ink,"town-title");
      for(int i=0;i<4;i++)Button(page,"Town","town:"+i,44+(i%2)*608,275+(i/2)*115,584,92,i==0,"town-"+i);
      Button(page,"‹  Regions","nav:regions",1000,12,232,42);break;
     case "contracts":
      Title(page,"PINEFALL","Choose your job","Clear 90% of the marked area. Late completion reduces pay. Existing snow and reward rules apply.");Text(page,"CAREER / ALPINE / PINEFALL",44,220,1150,25,15,Muted,"job-mode");
      var jn=new[]{"Depot Access","Parking Lot","Main Street"};var jd=new[]{"X −65 / Z −90\n12 minute deadline","X 94 / Z 21\n15 minute deadline","X 0 / Z 0\n15 minute deadline"};for(int i=0;i<3;i++){float x=44+i*405;Art(page,i==0?"quick":"pinefall",x,260,382,150);Button(page,jn[i]+"\n"+jd[i],"job:"+i,x,416,382,97,true);}
      Button(page,"Free Work","free",1000,12,232,42);break;
     case "machinery":
      Title(page,"PINEFALL / JOB PREPARATION","Choose your machinery","Swipe left or right to find your machine.");
      ui.machinePicture=Art(page,"tractor",240,210,800,190);ui.machinePicture.raycastTarget=true;var swipe=ui.machinePicture.gameObject.AddComponent<MachinerySwipe>();swipe.menu=ui;
      Button(page,"‹","browse:-1",140,310,68,68);Button(page,"›","browse:1",1072,310,68,68);Fill(Rect("Ownership badge",page,240,210,345,32),Paper);Text(page,"OWNED",254,214,320,30,17,Ink,"machine-owned");Text(page,"Golden Snow Tractor",240,410,800,36,28,Ink,"machine-name");Text(page,"Front plow",240,452,730,28,18,Muted,"machine-kind");
      Text(page,"1 / 2 · Swipe to browse",510,489,390,25,17,Muted,"machine-position");Button(page,"View Shop","nav:shop",1040,438,192,45,false,"machine-shop");break;
     case "garage":
      Title(page,"PINEFALL WORKSHOP","Garage & Shop");
      Button(page,"Prototype coins","nav:store",1000,12,232,42);
      var garageTab=Button(page,"Garage","workshop:garage",44,202,594,46,true);var shopTab=Button(page,"Shop","workshop:shop",642,202,594,46);ui.tabImages=new[]{garageTab.GetComponent<Image>(),shopTab.GetComponent<Image>()};ui.selectedColor=Yellow;ui.tabColor=Line;
      ui.workshopPicture=Art(page,"tractor",44,262,660,210);Text(page,"Golden Snow Tractor",60,475,620,32,24,Ink,"garage-name");Text(page,"OWNED",728,262,480,28,17,Muted,"garage-owned");
      var g=Group(page,"Garage tab",718,296,514,260);ui.garageContent=g;
      ui.garageTabImages=new[]{Button(g.transform,"Overview","garage-tab:overview",0,0,156,42).GetComponent<Image>(),Button(g.transform,"Repair / Fuel","garage-tab:service",166,0,176,42).GetComponent<Image>(),Button(g.transform,"Upgrades","garage-tab:upgrades",352,0,162,42).GetComponent<Image>()};
      ui.overviewContent=Group(g.transform,"Overview",0,50,514,210);Text(ui.overviewContent.transform,"Fuel / Condition",0,0,514,110,20,Ink,"garage-status");Button(ui.overviewContent.transform,"Select & Drive","drive",0,143,514,48,true,"drive");
      ui.serviceContent=Group(g.transform,"Service",0,50,514,210);Button(ui.serviceContent.transform,"Refuel","confirm:fuel",0,0,247,48,false,"fuel");Button(ui.serviceContent.transform,"Repair","confirm:repair",257,0,257,48,false,"repair");Button(ui.serviceContent.transform,"Recover to Depot","confirm:recover",0,65,514,48);Text(ui.serviceContent.transform,"Service requires a stopped machine at the depot.\nRecovery leaves carried snow at the pickup location.",0,126,514,65,18,Muted);
      ui.upgradesContent=Group(g.transform,"Upgrades",0,50,514,210);Button(ui.upgradesContent.transform,"Upgrade tyres","confirm:tyres",0,0,514,48,false,"tyres");Button(ui.upgradesContent.transform,"Upgrade engine","confirm:engine",0,65,514,48,false,"engine");Text(ui.upgradesContent.transform,"Three levels per machine. +12% grip / +18% torque per level. Depot fitting required.",0,126,514,65,18,Muted);
      var shop=Group(page,"Shop tab",718,296,514,260);ui.shopContent=shop;Text(shop.transform,"Golden Snow Loader",0,0,514,38,26);Text(shop.transform,"Move piles and transport snow to the disposal yard.\nIncludes the compatible 1.6 m³ bucket.",0,48,514,76,20,Muted);Text(shop.transform,"1,400 coins",0,130,514,40,28,Ink,"shop-price");Button(shop.transform,"Purchase Loader","confirm:buy",0,188,250,48,true,"buy");Button(shop.transform,"View in Garage","owned",260,188,254,48,false,"owned");
      Button(page,"Tractor","garage:0",44,516,320,42);Button(page,"Loader","garage:1",384,516,320,42);break;
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
      Title(page,"PINEFALL / SHIFT REPORT","A job well done.");Text(page,"Shift complete",80,225,1090,230,26,Ink,"results");Button(page,"Next Job","nav:contracts",800,470,432,54,true);Button(page,"Garage / Shop","nav:garage",320,470,432,54);break;
     case "store":
      Title(page,"PROTOTYPE CURRENCY","Simulated store","No real money. Uses the existing duplicate-safe test transaction flow.");Button(page,"New transaction","dummy:new",44,270,360);Button(page,"Success / redeliver","dummy:success",454,270,360);Button(page,"Cancel","dummy:cancel",864,270,360);Button(page,"Fail","dummy:fail",44,350,360);break;
    }
    page.gameObject.SetActive(name=="dashboard");
   }
   // Shared bottom band. Job-specific footer appears only on the selector.
   var footer=Rect("Footer",content,0,642,1280,78);Fill(footer,Line);Text(footer,"Welcome to Pinefall.",44,10,890,48,17,Muted,"notice");Text(footer,"LEVEL 1 · 0 XP",44,57,800,19,13,Muted,"level");Button(footer,"Back to Job","continue",1000,14,232,48);
   var selector=pages[names.ToList().IndexOf("machinery")];Text(selector.transform,"Depot Access",44,530,850,32,18,Ink,"machine-job");Button(selector.transform,"Start Job","start",1000,508,232,48,true,"start");
   var modal=Rect("Confirmation overlay",root,0,0,1280,720);Fill(modal,new Color(.03f,.09f,.14f,.88f));ui.dialog=modal.gameObject;var dialog=Rect("Dialog",modal,300,174,680,372);Fill(dialog,Paper);Text(dialog,"Confirm",30,28,620,65,28,Ink,"dialog-title");Text(dialog,"Details",30,107,620,100,20,Muted,"dialog-body");Text(dialog,"Cost",30,220,620,40,22,Ink,"dialog-price");Button(dialog,"Cancel","cancel",30,290,280,50,false,"cancel");Button(dialog,"Confirm","accept",330,290,320,50,true,"accept");modal.gameObject.SetActive(false);
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
