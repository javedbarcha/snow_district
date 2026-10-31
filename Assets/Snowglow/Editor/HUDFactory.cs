using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor.Events;
using System.Collections.Generic;
namespace Snowglow.Editor {
 public static class HUDFactory {
  static Font font;static Color paper=new Color(.94f,.97f,.98f,.97f),ink=new Color(.13f,.23f,.29f),teal=new Color(.18f,.4f,.43f);
  static RectTransform Rect(string name,Transform parent,float x,float y,float width,float height){
   var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);return r;
  }
  static RectTransform Full(string name,Transform parent){var r=Rect(name,parent,0,0,0,0);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
  static void Image(RectTransform r,Color color){r.gameObject.AddComponent<Image>().color=color;}
  static Text Text(Transform parent,string value,float x,float y,float width,float height,int size=20){
   var r=Rect(value.Split('\n')[0],parent,x,y,width,height);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=ink;t.raycastTarget=false;t.verticalOverflow=VerticalWrapMode.Overflow;return t;
  }
  static RectTransform Button(Transform parent,string value,string command,float x,float y,float width=210,float height=50){
   var r=Rect(value,parent,x,y,width,height);Image(r,teal);var b=r.gameObject.AddComponent<Button>();var colors=b.colors;colors.highlightedColor=new Color(.8f,.92f,.95f);b.colors=colors;
   var t=Text(r,value,8,6,width-16,height-10,17);t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;
   if(command!=""){var action=r.gameObject.AddComponent<UICommand>();action.command=command;UnityEventTools.AddPersistentListener(b.onClick,action.Click);}return r;
  }
  static void Hold(Transform parent,string title,string channel,float value,float x,float y,float width=110,float height=72){
   var r=Button(parent,title,"",x,y,width,height);var c=r.gameObject.AddComponent<TouchControl>();c.channel=channel;c.value=value;
  }
  public static PrototypeHUD Build(){
   font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
   var canvas=new GameObject("HUD | preauthored mobile Canvas",typeof(RectTransform)).AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;
   var scaler=canvas.gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;canvas.gameObject.AddComponent<GraphicRaycaster>();
   var safe=Full("Safe area",canvas.transform);var design=Rect("1280 design canvas",safe,0,0,1280,720);design.anchorMin=design.anchorMax=new Vector2(.5f,.5f);design.pivot=new Vector2(.5f,.5f);design.anchoredPosition=Vector2.zero;
   var hud=canvas.gameObject.AddComponent<PrototypeHUD>();hud.safeArea=safe;hud.layout=design;
   var driving=Full("Driving HUD",design);hud.driving=driving.gameObject;
   hud.status=Text(driving,"SNOWGLOW",18,12,400,76,14);
   hud.notice=Text(driving,"Ready",440,12,400,76,14);
   hud.objective=Text(driving,"CONTRACT",862,12,400,76,14);
   Button(driving,"PAUSE","pause",1100,119,160,48);hud.cameraModeText=Button(driving,"CAMERA","camera",18,119,150,48).GetComponentInChildren<Text>();Button(driving,"DEPOT MENU","open:garage",18,175,150,48);
   var cameraTools=Full("Camera tools",driving);hud.cameraTools=cameraTools.gameObject;
   Button(cameraTools,"VIEW RESET","camera:reset",18,235,150,42);
   hud.cameraHelp=Text(driving,"ORBIT | drag empty view, pinch or scroll to zoom",350,202,580,54,14);cameraTools.gameObject.SetActive(false);hud.cameraHelp.gameObject.SetActive(false);
   Hold(driving,"STEER  <       >","steer",1,20,564,270,118);
   Hold(driving,"BRAKE","brake",1,1014,580,110,104);Hold(driving,"DRIVE","throttle",1,1142,564,118,120);
   Button(driving,"F / R","reverse",1142,503,118,48);
   Button(driving,"WORK / CARRY","work",358,582,173,50);Button(driving,"ANGLE L/C/R","angle",738,582,176,50);
   Button(driving,"DUMP","dump",358,642,173,42);Button(driving,"RECOVER","recover",738,642,176,42);
   hud.debugText=Text(driving,"PHYSICS",940,230,320,240,15);
   var menu=Full("Menu layer",design);var background=menu.gameObject.AddComponent<Image>();background.color=new Color(.09f,.17f,.23f,.85f);
   var info=Rect("Career summary",menu,35,30,1210,157);Image(info,paper);hud.menuInfo=Text(info,"SNOWGLOW",18,12,1170,140,18);
   var names=new[]{"dashboard","contracts","garage","shop","parts","service","store","settings","pause","results"};var panels=new List<GameObject>();
   foreach(string name in names){
    var panel=Rect(name,menu,35,204,1210,480);Image(panel,paper);panels.Add(panel.gameObject);
    Text(panel,name.ToUpperInvariant(),22,16,1150,42,28);
    switch(name){
     case "dashboard":
      Text(panel,"A one-kilometre winter world, with a compact working district.\nDrive from the depot to the marked job. Plow snow sideways, then use the loader to move piles.\nBoth machinery and environment are editable primitive assemblies.",24,75,1140,100);
      Button(panel,"CONTINUE JOB","continue",24,210);Button(panel,"CONTRACT BOARD","open:contracts",250,210);Button(panel,"GARAGE","open:garage",476,210);Button(panel,"SHOPS","open:shop",702,210);Button(panel,"SETTINGS","open:settings",928,210);
      Button(panel,"FREE WORK","free",24,282);Button(panel,"FRESH SNOWFALL","fresh",250,282);Button(panel,"DUMMY STORE","open:store",476,282);
      Text(panel,"DESKTOP: W/S drive/reverse | A/D steer | Space brake | E work/carry | Q blade angle\nV Auto/Manual | R/F lift | T/G tilt | B dump | C cycles 3 cameras | H recover | Esc pause\nOrbit: right-drag or J/L + I/K | scroll or +/- zoom | Home resets",24,371,1140,70,17);break;
     case "contracts":
      Text(panel,"Clear 90% of the marked area. Late completion reduces pay.\nSnow placed back in the work area obstructs it again. Use the storage yard at X94 / Z-85.",24,76,1140,74);
      Button(panel,"DEPOT ACCESS\nX -65, Z -90","job:0",24,192,350,90);Button(panel,"PARKING LOT\nX 94, Z 21","job:1",426,192,350,90);Button(panel,"MAIN STREET\nX 0, Z 0","job:2",828,192,350,90);
      Button(panel,"FREE WORK","free",24,313);break;
     case "garage":
      Text(panel,"Switch at the depot after stopping. The current contract and piles remain.\nMount interfaces, equipment mass, width and hydraulic requirements are validated.\nTruck: municipal front blade. Loader: quick-coupler bucket. Future equipment uses its own definition.",24,75,1140,110);
      Button(panel,"SELECT PLOW","switch:0",24,222,320,60);Button(panel,"SELECT LOADER","switch:1",365,222,320,60);Button(panel,"PARTS","open:parts",706,222);
      Button(panel,"FUEL / REPAIRS","open:service",24,309);Button(panel,"MACHINERY SHOP","open:shop",250,309);break;
     case "shop":
      Text(panel,"MACHINERY SHOP\nMunicipal plow: starter owned.\nWheeled loader + compatible bucket: 1,400 coins.\nPrototype machines remain at the depot until selected.",24,75,1140,145);
      Button(panel,"BUY LOADER - 1,400","buy:1",24,258,310,60);Button(panel,"PARTS SHOP","open:parts",365,258);Button(panel,"DUMMY STORE","open:store",600,258);break;
     case "parts":
      Text(panel,"PARTS FOR SELECTED MACHINE\nTyres: +12% grip per level. Engine: +18% available torque per level.\nThree levels, costing 200 / 400 / 600 coins. Fitting requires the depot.\nBlade and bucket swaps are limited to the supplied compatible equipment in this build.",24,75,1140,145);
      Button(panel,"UPGRADE TYRES","tyres",24,264,310,60);Button(panel,"UPGRADE ENGINE","engine",365,264,310,60);break;
     case "service":
      Text(panel,"DEPOT SERVICE\nRefuel: 2 coins per litre. Repair: 3 coins per condition point.\nReturn to the depot to service your machine.\nFree recovery preserves the bucket contents and the district snow.",24,75,1140,145);
      Button(panel,"REFUEL","fuel",24,264);Button(panel,"REPAIR","repair",250,264);Button(panel,"RECOVER TO DEPOT","recover",476,264,300);break;
     case "store":
      Text(panel,"SIMULATED PURCHASES ONLY - NO REAL MONEY\nCreate a test transaction, then choose success, cancel or failure.\nSuccess grants 2,000 prototype coins. Delivering the same transaction twice grants nothing.",24,75,1140,110);
      Button(panel,"NEW TRANSACTION","dummy:new",24,235,260);Button(panel,"SUCCESS / REDELIVER","dummy:success",300,235,290);Button(panel,"CANCEL","dummy:cancel",606,235);Button(panel,"FAIL","dummy:fail",832,235);break;
     case "settings":
      Text(panel,"ANDROID / GALAXY A07 REFERENCE TARGET\n30 FPS target. Low/standard render scale and shadow distance.\nAssistance affects traction and stability. Gameplay menus pause the simulation.\nPerformance, rendering and handling still require testing on the actual phone.",24,75,1140,145);
      Button(panel,"LOW / STANDARD","quality",24,264,260);Button(panel,"SOUND ON / OFF","sound",300,264,260);Button(panel,"ASSISTS ON / OFF","assist",576,264,260);Button(panel,"DEBUG OVERLAY","debug",852,264,300);break;
     case "pause":
      Text(panel,"The simulation, fuel consumption, deadline and combo are paused.\nResume or visit a menu. Abandoning resets the active worksite.",24,75,1140,100);
      Button(panel,"RESUME","continue",24,222);Button(panel,"DASHBOARD","open:dashboard",250,222);Button(panel,"SETTINGS","open:settings",476,222);Button(panel,"ABANDON JOB","abandon",702,222);Button(panel,"FINISH FREE WORK","finish",928,222);break;
     case "results":
      Text(panel,"SHIFT COMPLETE\nRewards are committed once and saved.\nSelect a fresh contract for another snowfall, or visit the garage to improve your machinery.",24,75,1140,120);
      Button(panel,"DASHBOARD","open:dashboard",24,250);Button(panel,"CONTRACTS","open:contracts",250,250);Button(panel,"GARAGE","open:garage",476,250);break;
    }
    if(name!="dashboard")Button(panel,"DASHBOARD","open:dashboard",24,407,210,45);
    Button(panel,"BACK TO JOB","continue",950,407,225,45);panel.gameObject.SetActive(name=="dashboard");
   }
   hud.panels=panels.ToArray();hud.names=names;hud.menuRoot=menu.gameObject;
   var events=new GameObject("EventSystem | Input System UI");events.AddComponent<EventSystem>();var module=events.AddComponent<InputSystemUIInputModule>();module.AssignDefaultActions();
   CompactHUDAuthoring.Apply(hud);DrivingButtonsAuthoring.Apply(hud);return hud;
  }
 }
}
