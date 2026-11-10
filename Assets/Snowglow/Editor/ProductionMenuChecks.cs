using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Snowglow.Editor {
 public static class ProductionMenuChecks {
  static int checks;
  static void Check(bool value,string why){checks++;if(!value)throw new InvalidOperationException("Production UI: "+why);}
  public static void ValidateAuthored(PrototypeHUD hud){
   var ui=hud.productionMenu;Check(ui&&hud.menuRoot==ui.gameObject,"Menu binding");Check(ui.pages.Length==10&&ui.pageNames.Distinct().Count()==10,"Screen inventory");Check(ui.vehiclePictures.Length==2&&ui.vehiclePictures.All(s=>s&&AssetDatabase.Contains(s)),"Saved vehicle captures");Check(ui.dialog&&ui.garageContent&&ui.shopContent&&ui.contentGroup,"Tabs/modal binding");
   foreach(var a in ui.GetComponentsInChildren<ProductionMenuAction>(true)){var b=a.GetComponent<Button>();Check(b&&a.menu==ui&&b.onClick.GetPersistentEventCount()==1&&b.onClick.GetPersistentTarget(0)==a&&b.onClick.GetPersistentMethodName(0)=="Click","Persistent action: "+a.name);}
   Check(ui.GetComponentsInChildren<MachinerySwipe>(true).Length==1,"Single swiping preview");Check(hud.panels.Length==10,"Legacy fallback retained");
   Check(ui.labels.Select(x=>x.key).Distinct().Count()==ui.labels.Length,"Duplicate label keys");Check(ui.buttons.Select(x=>x.key).Distinct().Count()==ui.buttons.Length,"Duplicate button keys");
  }
  public static void Run(PrototypeHUD original){
   checks=0;ValidateAuthored(original);var source=original.GetComponentInParent<Canvas>();var real=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<PrototypeSession>(true)).Single();
   var preview=EditorSceneManager.NewPreviewScene();var root=new GameObject("Disposable production UI fixture");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);var field=typeof(PrototypeSession).GetField("<Instance>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic);var prior=PrototypeSession.Instance;float time=Time.timeScale;
   try{
    var game=root.AddComponent<PrototypeSession>();field.SetValue(null,game);game.Profile=new ProfileSave();game.Input=root.AddComponent<PrototypeInput>();game.Audio=root.AddComponent<SynthAudio>();game.Audio.effects=root.AddComponent<AudioSource>();game.Audio.effects.mute=true;game.Audio.chime=real.Audio.chime;game.CameraRig=real.CameraRig;game.Snow=root.AddComponent<SnowField>();game.Snow.seed=real.Snow.seed;
    game.Fleet=real.Fleet.Select(m=>{var copy=UnityEngine.Object.Instantiate(m.gameObject);copy.transform.SetParent(root.transform,false);copy.transform.position=new Vector3(-95,6,-90);var machine=copy.GetComponent<MachineController>();machine.Initialize();machine.Owned=!machine.definition.loader;machine.Fuel=machine.definition.fuelCapacity*.5f;machine.Condition=80;return machine;}).ToArray();
    var canvas=UnityEngine.Object.Instantiate(source.gameObject);canvas.transform.SetParent(root.transform,false);game.HUD=canvas.GetComponent<PrototypeHUD>();var ui=game.HUD.productionMenu;ui.gameObject.SetActive(true);ValidateAuthored(game.HUD);
    game.Open("dashboard");Check(!game.IsRunning&&Time.timeScale==0,"Home pauses");ui.Execute("career");Check(ui.ScreenName=="regions","Career route");
    for(int r=0;r<5;r++){ui.Execute("region:"+r);Check(ui.ScreenName=="towns","Town route");for(int i=0;i<ProductionMenu.Destinations[r].Length;i++)Check(ui.Button("town-"+i).interactable==(r==0&&i==0),"Planned town gate");}
    ui.Execute("region:0");ui.Execute("town:0");Check(game.Panel=="contracts","Pinefall route");ui.Execute("job:1");Check(ui.ScreenName=="machinery"&&ui.JobIndex==1,"Job preparation");int coins=game.Profile.coins;
    ui.Browse(1);Check(ui.PreviewIndex==1&&!ui.Button("start").interactable,"Loader locked");ui.Execute("start");Check(!game.Profile.inJob&&game.Profile.active==0&&game.Profile.coins==coins,"Cannot start locked machine");ui.Browse(1);Check(ui.PreviewIndex==0,"Carousel wraps");
    var swipe=ui.GetComponentInChildren<MachinerySwipe>(true);var data=new PointerEventData(null){pointerId=3,position=new Vector2(600,200)};swipe.OnBeginDrag(data);data.position=new Vector2(300,200);swipe.OnEndDrag(data);Check(ui.PreviewIndex==1,"Swipe left");data.position=new Vector2(300,200);swipe.OnBeginDrag(data);data.position=new Vector2(600,200);swipe.OnEndDrag(data);Check(ui.PreviewIndex==0,"Swipe right");data.position=new Vector2(600,200);swipe.OnBeginDrag(data);data.position=new Vector2(601,500);swipe.OnEndDrag(data);Check(ui.PreviewIndex==0,"Vertical drag rejection");
    ui.Execute("nav:shop");Check(ui.ScreenName=="garage"&&ui.ShopTab,"Unified shop route");ui.Execute("confirm:buy");Check(ui.dialog.activeSelf&&game.Profile.coins==coins,"Purchase needs confirmation");ui.Execute("cancel");Check(!game.Fleet[1].Owned&&game.Profile.coins==coins,"Cancel purchase");ui.Execute("confirm:buy");ui.Execute("accept");Check(game.Fleet[1].Owned&&game.Profile.coins==coins-game.Fleet[1].definition.price,"Actual purchase price");int after=game.Profile.coins;ui.Execute("accept");Check(game.Profile.coins==after,"Double confirm inert");ui.Execute("owned");Check(!ui.ShopTab&&ui.garageContent.activeSelf&&!ui.shopContent.activeSelf,"Garage tab");
    ui.Execute("garage:0");ui.Execute("garage-tab:service");Check(ui.serviceContent.activeSelf&&!ui.overviewContent.activeSelf,"Garage service tab");int cost=ui.Quote("repair");Check(cost==60,"Repair uses live condition");ui.Execute("confirm:repair");ui.Execute("accept");Check(game.Fleet[0].Condition==100&&game.Profile.coins==after-cost,"Actual repair");
    game.Fleet[0].Tyres=2;Check(ui.Quote("tyres")==600,"Upgrade level pricing");game.Fleet[0].Tyres=3;ui.Refresh();Check(!ui.Button("tyres").interactable,"Upgrade max gate");
    ui.Execute("job:2");ui.Execute("preview:1");ui.Execute("start");Check(game.Profile.inJob&&game.Profile.job==2&&game.Profile.active==1&&game.IsRunning&&Time.timeScale==1,"Actual loader job start");Check(game.HUD.driving.activeSelf&&!game.HUD.menuRoot.activeSelf,"Driving preserved");
    game.Open("pause");Check(Time.timeScale==0&&!game.HUD.driving.activeSelf,"Pause screen");ui.Execute("continue");Check(game.IsRunning&&Time.timeScale==1,"Resume");game.Open("shop");Check(ui.ShopTab&&Time.timeScale==0,"Shop pauses");
    game.Profile.coins=0;ui.Execute("garage:0");game.Fleet[0].Condition=50;ui.Execute("confirm:repair");Check(!ui.Button("accept").interactable,"Insufficient currency confirmation");ui.Execute("cancel");
    foreach(var name in ui.pageNames){game.Open(name);ui.Refresh();Check(ui.pages.Count(p=>p.activeSelf)==1,"Only one screen: "+name);Canvas.ForceUpdateCanvases();foreach(var button in ui.GetComponentsInChildren<Button>()){if(!button.gameObject.activeInHierarchy)continue;var corners=new Vector3[4];((RectTransform)button.transform).GetWorldCorners(corners);foreach(var corner in corners){var point=game.HUD.layout.InverseTransformPoint(corner);Check(point.x>=-641&&point.x<=641&&point.y>=-361&&point.y<=361,"Control outside design: "+button.name+" "+point);}}}
    foreach(var name in new[]{"dashboard","regions","towns","contracts","machinery","garage","shop","settings","pause","results"}){game.Open(name);Capture(game.HUD,name,1280,720);}
    game.Open("garage");Capture(game.HUD,"garage-wide",1920,1080);
    File.WriteAllText(ProductionMenuAuthoring.Folder+"/Checks.txt","PASS: "+checks+" authored and actual-session fixture checks. Routes, all planned location gates, carousel wrap/swipes/vertical rejection, ownership, purchase/cancel/double confirmation, real repair/upgrade quotes, actual loader job start, pause/resume, insufficient funds, saved references and screen control bounds. Ten native screen renders plus 1920x1080 garage. Fixture ready=false; no player-save writes. Actual phone touch and Play traversal remain untested.");
   }finally{field.SetValue(null,prior);Time.timeScale=time;EditorSceneManager.ClosePreviewScene(preview);}
  }
  static void Capture(PrototypeHUD hud,string name,int width,int height){
   var canvas=hud.GetComponent<Canvas>();foreach(var t in hud.GetComponentsInChildren<Transform>(true))t.gameObject.layer=5;var parent=canvas.transform.parent;var fixtureScene=canvas.gameObject.scene;canvas.transform.SetParent(null,false);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvas.gameObject,UnityEngine.SceneManagement.SceneManager.GetActiveScene());
   var obj=new GameObject("Temporary UI capture camera");var camera=obj.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.gray;camera.cullingMask=1<<5;camera.orthographic=true;camera.nearClipPlane=.1f;camera.farClipPlane=100;obj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
   var target=new RenderTexture(width,height,24);var old=RenderTexture.active;var texture=new Texture2D(width,height,TextureFormat.RGB24,false);var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;
   try{
    camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
    foreach(var graphic in hud.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();Canvas.ForceUpdateCanvases();
    File.WriteAllText(ProductionMenuAuthoring.Folder+"/RenderDiagnostic.txt","active "+canvas.gameObject.activeInHierarchy+" enabled "+canvas.enabled+" rect "+((RectTransform)canvas.transform).rect+" ui "+hud.productionMenu.gameObject.activeInHierarchy+" position "+hud.productionMenu.transform.position+" scale "+hud.productionMenu.transform.lossyScale+" camera "+camera.pixelRect+" graphics "+hud.GetComponentsInChildren<Graphic>().Length);
    camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();
    File.WriteAllBytes(ProductionMenuAuthoring.Folder+"/"+name+".png",texture.EncodeToPNG());Check(texture.GetPixels32().Count(p=>Math.Abs(p.r-p.b)>8)>width*height/4,"UI render is empty: "+name);
   }finally{
    canvas.renderMode=mode;canvas.worldCamera=oldCamera;RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(texture);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(obj);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvas.gameObject,fixtureScene);canvas.transform.SetParent(parent,false);
   }
  }
 }
}
