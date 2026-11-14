using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace Snowglow {
 [Serializable]public sealed class MenuTextBinding {public string key;public Text text;}
 [Serializable]public sealed class MenuButtonBinding {public string key;public Button button;}
 public sealed class ProductionMenu:MonoBehaviour {
  public GameObject[] pages;public string[] pageNames;
  public MenuTextBinding[] labels;public MenuButtonBinding[] buttons;
  public GameObject dialog,garageContent,shopContent,overviewContent,serviceContent,upgradesContent;
  public Image[] upgradeSegments;public CanvasGroup contentGroup;public WorkshopPreview workshopPreview;public int PartIndex {get;private set;}
  public CareerCardGallery careerGallery;
  public GameObject[] settingPages;public Image machinePicture,workshopPicture;
  public Sprite[] vehiclePictures;public Image[] tabImages,garageTabImages,settingsTabImages;public Color selectedColor,tabColor;
  public string ScreenName {get;private set;}="dashboard";
  public int PreviewIndex {get;private set;}
  public int RegionIndex {get;private set;}
  public int JobIndex {get;private set;}
  public bool ShopTab {get;private set;}
  public string GarageTab {get;private set;}="overview";
  public int SettingsTab {get;private set;}
  public bool QuickJob {get;private set;}
  public int TownIndex {get;private set;}
  public GameObject quickTools;public GameObject[] jobCards=new GameObject[0];
  public string PendingConfirmation {get;private set;}="";
  string lastPanel,garageReturn="dashboard",settingsReturn="dashboard";float nextRefresh;int garageIndex,pendingJob,quickPage;bool pendingFree,pendingQuick;string pendingJobTransaction,pendingTown;
  readonly System.Random quickRandom=new System.Random();TownJob[] quickJobs=new TownJob[0];
  static readonly string[] JobNames={"Depot Access","Parking Lot","Main Street"};
  public static readonly string[] RegionNames={"Mountain & Alpine","City & Urban","Highway & Open Country","Suburban & Commercial","Industrial & Extreme Weather"};
  public static readonly string[][] Destinations={new[]{"Pinefall","The Alps · Switzerland / Austria","Svalbard · Norway","The Andes · Chile / Argentina"},new[]{"Aomori City · Japan","Sapporo (Hokkaido) · Japan","Harbin · China"},new[]{"Rogers Pass · British Columbia, Canada","Coquihalla Highway · Canada","Tromsø · Norway"},new[]{"Buffalo · New York, USA","Anchorage · Alaska, USA","Minneapolis–St. Paul · Minnesota, USA"},new[]{"Norilsk · Siberia, Russia","Yakutsk · Russia","McMurdo Station · Antarctica"}};
  PrototypeSession Game=>PrototypeSession.Instance;
  public void SetLabel(string key,string value){foreach(var b in labels)if(b.key==key){b.text.text=value;return;}}
  public Text Label(string key){foreach(var b in labels)if(b.key==key)return b.text;return null;}
  public Button Button(string key){foreach(var b in buttons)if(b.key==key)return b.button;return null;}
  void Enable(string key,bool enabled){var b=Button(key);if(b)b.interactable=enabled;}
  string SelectedTown=>CareerProgression.TownIds[RegionIndex][TownIndex];
  static bool Workshop(string panel)=>panel=="garage"||panel=="shop"||panel=="parts"||panel=="service";
  public void Show(string panel){
   if(lastPanel!=panel){
    contentGroup.interactable=true;PendingConfirmation="";if(dialog)dialog.SetActive(false);
    if(panel=="settings")settingsReturn=lastPanel??"dashboard";
    if(Workshop(panel)){
     bool restore=lastPanel=="settings"||lastPanel=="store"||Workshop(lastPanel);
     if(!restore){garageReturn=lastPanel??"dashboard";garageIndex=Game&&Game.Profile!=null?Game.Profile.active:0;PartIndex=0;}
     if(!restore||panel!="garage"){ShopTab=panel=="shop";GarageTab=panel=="parts"?"upgrades":panel=="service"?"service":"overview";}
    }
    lastPanel=panel;
   }
   ScreenName=panel=="shop"||panel=="parts"||panel=="service"?"garage":panel;
   for(int i=0;i<pages.Length;i++)pages[i].SetActive(pageNames[i]==ScreenName);
   if(careerGallery)careerGallery.Refresh(RegionIndex);
   Refresh();
  }
  public void Navigate(string screen){PendingConfirmation="";dialog.SetActive(false);contentGroup.interactable=true;Game.Open(screen);}
  public void Back(){
   if(PendingConfirmation!=""){Execute("cancel");return;}
   string target=ScreenName=="garage"?garageReturn:ScreenName=="settings"?settingsReturn:ScreenName=="store"?"shop":ScreenName=="towns"?"regions":ScreenName=="contracts"?(QuickJob?"dashboard":"towns"):ScreenName=="pause"?"":"dashboard";
   Navigate(target);
  }
  public void ShuffleQuick(){quickJobs=CareerProgression.QuickJobs(Game.Profile,quickRandom);quickPage=0;Refresh();}
  public TownJob? JobAtSlot(int slot){
   if(slot<0||slot>=3)return null;if(!QuickJob)return new TownJob(SelectedTown,slot);
   int index=quickPage*3+slot;return index<quickJobs.Length?(TownJob?)quickJobs[index]:null;
  }
  void SelectJob(int slot){var job=JobAtSlot(slot);if(!job.HasValue)return;JobIndex=job.Value.kind;Game.StartJob(job.Value.kind,false,QuickJob,job.Value.town);}
  public void Browse(int step){if(!Game||Game.Fleet.Length==0)return;if(ScreenName=="garage"){if(ShopTab)PartIndex=(PartIndex+step+2)%2;else garageIndex=(garageIndex+step+Game.Fleet.Length)%Game.Fleet.Length;}else PreviewIndex=(PreviewIndex+step+Game.Fleet.Length)%Game.Fleet.Length;Refresh();}
  public void Execute(string command){
   var g=Game;if(!g||g.Profile==null)return;
   var c=command.Split(':');var arg=c.Length>1?c[1]:"";
   switch(c[0]){
    case "nav":Navigate(arg);break;
    case "back":Back();return;
    case "quick":QuickJob=true;ShuffleQuick();Navigate("contracts");break;
    case "shuffle":ShuffleQuick();break;
    case "quick-page":if(quickJobs.Length>0)quickPage=(quickPage+int.Parse(arg)+(quickJobs.Length+2)/3)%((quickJobs.Length+2)/3);break;
    case "career":QuickJob=false;Navigate("regions");break;
    case "region":int region=int.Parse(arg);if(region>=0&&region<RegionNames.Length){RegionIndex=region;TownIndex=0;Navigate("towns");}break;
    case "town":int town=int.Parse(arg);if(CareerProgression.TownUnlocked(g.Profile,RegionIndex,town)&&CareerProgression.Available(CareerProgression.TownIds[RegionIndex][town])){TownIndex=town;QuickJob=false;Navigate("contracts");}break;
    case "job":SelectJob(int.Parse(arg));break;
    case "browse":Browse(int.Parse(arg));break;
    case "preview":PreviewIndex=int.Parse(arg);break;
    case "start":Navigate("garage");break;
    case "continue":if(!g.Profile.inJob){QuickJob=false;RegionIndex=TownIndex=0;}g.Continue();break;
    case "free":g.StartJob(0,true);break;
    case "workshop":ShopTab=arg=="shop";break;
    case "garage":garageIndex=int.Parse(arg);break;
    case "garage-tab":GarageTab=arg;break;
    case "settings-tab":SettingsTab=int.Parse(arg);break;
    case "drive":if(g.Fleet[garageIndex].Owned&&(garageIndex==g.Profile.active||g.TrySelectMachine(garageIndex,false)))Back();return;
    case "replay":QuickJob=g.Profile.quickPlay;g.StartJob(g.Profile.job,g.Profile.freeWork,g.Profile.quickPlay,g.Profile.jobTownId);break;
    case "next-job":if(!g.Profile.quickPlay&&!g.Profile.freeWork&&g.Profile.job+1<CareerProgression.JobNames.Length){QuickJob=false;g.StartJob(g.Profile.job+1,false,false,ProfileTown(g.Profile));}break;
    case "buy-vehicle":Confirm("vehicle");return;
    case "buy-part":Confirm(PartIndex==0?"tyres":"engine");return;
    case "owned":garageIndex=1;ShopTab=false;GarageTab="overview";break;
    case "confirm":Confirm(arg);return;
    case "cancel":PendingConfirmation="";dialog.SetActive(false);contentGroup.interactable=true;break;
    case "dialog-secondary":bool resume=PendingConfirmation=="replace-job";Execute("cancel");if(resume)g.Continue();return;
    case "accept":Accept();return;
    case "quality":g.Profile.quality=1-g.Profile.quality;g.ApplyQuality();g.Save();break;
    case "sound":g.Profile.muted=!g.Profile.muted;g.Audio.SetMuted(g.Profile.muted);g.Save();break;
    case "assist":g.Profile.assists=!g.Profile.assists;g.ApplyQuality();g.Save();break;
    case "debug":g.Profile.debug=!g.Profile.debug;g.Save();break;
    case "dummy":g.Dummy(arg);break;
    case "fresh":g.FreshDistrict();break;
   }
   Refresh();
  }
  public int Quote(string action){var g=Game;var m=g.Fleet[Mathf.Clamp(garageIndex,0,g.Fleet.Length-1)];switch(action){case "vehicle":return m.definition.price;case "buy":return g.Fleet[1].definition.price;case "fuel":return Mathf.CeilToInt((m.definition.fuelCapacity-m.Fuel)*2);case "repair":return Mathf.CeilToInt((100-m.Condition)*3);case "tyres":return m.Tyres>=3?0:200+m.Tyres*200;case "engine":return m.Engine>=3?0:200+m.Engine*200;default:return 0;}}
  public void Confirm(string action){
   SetLabel("cancel","Cancel");SetLabel("accept","Confirm");Label("dialog-price").gameObject.SetActive(true);
   PendingConfirmation=action;int cost=Quote(action);string title=(action=="buy"||action=="vehicle")?"Purchase "+Game.Fleet[action=="buy"?1:garageIndex].definition.displayName+"?":action=="recover"?"Recover to the depot?":action=="abandon"?"Abandon this shift?":action=="finish"?"Finish free work?":"Confirm "+action+"?";
   SetLabel("dialog-title",title);SetLabel("dialog-body",action=="recover"?"Attachment raised. Bucket snow stays at the pickup location. Fuel and condition stay unchanged.":action=="abandon"?"Current job progress resets. Your purchased machinery stays in your fleet.":(action=="buy"||action=="vehicle")?"Includes its compatible snow attachment. Available at the depot.":"Depot service and upgrades use your selected machine. Existing compatibility and location rules apply.");
   SetLabel("dialog-price",cost>0?cost.ToString("N0")+" coins  ·  Balance after: "+(Game.Profile.coins-cost).ToString("N0"):"No charge");
   Enable("accept",cost<=Game.Profile.coins);contentGroup.interactable=false;dialog.SetActive(true);Button("cancel").Select();
  }
  public void ShowJobConflict(int kind,bool free,bool quick=false,string town=CareerProgression.Pinefall){
   pendingJob=kind;pendingFree=free;pendingQuick=quick;pendingTown=town;pendingJobTransaction=Game.Profile.transaction;PendingConfirmation="replace-job";
   string current=Game.Profile.freeWork?"Free Work":JobNames[Mathf.Clamp(Game.Profile.job,0,2)];
   string next=free?"Free Work":JobNames[Mathf.Clamp(kind,0,2)];
   SetLabel("dialog-title","A job is already in progress");
   SetLabel("dialog-body","Current: "+current+"\nRequested: "+next+"\nContinue and finish your current job, or abandon it to start the new one. Abandoning forfeits progress and unpaid rewards.");
   Label("dialog-price").gameObject.SetActive(false);SetLabel("cancel","Continue Current Job");SetLabel("accept","Abandon & Start New Job");
   Enable("accept",true);contentGroup.interactable=false;dialog.SetActive(true);Button("cancel").Select();
  }
  public void Accept(){
   string action=PendingConfirmation;PendingConfirmation="";dialog.SetActive(false);contentGroup.interactable=true;if(action=="")return;
   var g=Game;
   if(action=="replace-job"){if(g.Profile.inJob&&g.Profile.transaction==pendingJobTransaction)g.AbandonAndStartJob(pendingJob,pendingFree,pendingQuick,pendingTown);Refresh();return;}
   if(action=="buy"||action=="vehicle")g.BuyMachine(action=="buy"?1:garageIndex);
   else if(action=="recover")g.Recover();else if(action=="abandon")g.Abandon();else if(action=="finish")g.Finish();
   else if(garageIndex==g.Profile.active||g.TrySelectMachine(garageIndex,false)){
    if(action=="fuel"||action=="repair")g.Service(action=="fuel");else if(action=="tyres"||action=="engine")g.Upgrade(action=="tyres");
   }
   Refresh();
  }
  public void Refresh(){
   var g=Game;if(!g||g.Profile==null||g.Fleet==null||g.Fleet.Length<2)return;
   PreviewIndex=Mathf.Clamp(PreviewIndex,0,g.Fleet.Length-1);garageIndex=Mathf.Clamp(garageIndex,0,g.Fleet.Length-1);
   var p=g.Profile;var selected=g.Fleet[PreviewIndex];var m=g.Fleet[garageIndex];
   SetLabel("wallet",p.coins.ToString("N0")+" coins");SetLabel("notice",g.Notice);SetLabel("level","LEVEL "+g.Level+" · "+p.xp+" XP");
   SetLabel("job-mode",QuickJob?"QUICK PLAY / UNLOCKED JOBS":"CAREER / "+Destinations[RegionIndex][TownIndex]);
   if(quickTools)quickTools.SetActive(QuickJob);
   SetLabel("quick-page",(quickPage+1)+" / "+Mathf.Max(1,(quickJobs.Length+2)/3));
   Enable("quick-prev",quickJobs.Length>3);Enable("quick-next",quickJobs.Length>3);
   for(int slot=0;slot<3;slot++){
    var job=JobAtSlot(slot);bool exists=job.HasValue;var b=Button("job-"+slot);if(!b)continue;
    b.gameObject.SetActive(exists);if(slot<jobCards.Length&&jobCards[slot])jobCards[slot].SetActive(exists);bool unlocked=exists&&CareerProgression.JobUnlocked(p,job.Value.town,job.Value.kind);b.interactable=unlocked;
    if(exists)SetLabel("job-"+slot,CareerProgression.JobNames[job.Value.kind]+"\n"+(QuickJob?"Pinefall · ":"")+(unlocked?(CareerProgression.Completed(p,job.Value.town,job.Value.kind)?"COMPLETED / REPLAY":"AVAILABLE"):"LOCKED · Complete previous job"));
   }
   SetLabel("resume-footer",p.inJob?"Resume Job":"Choose a Job");
   SetLabel("back-garage","Main Menu");
   SetLabel("back-settings",Workshop(settingsReturn)?"Back to Garage":"Back");
   var next=Button("next-job");if(next)next.gameObject.SetActive(!p.quickPlay&&!p.freeWork&&p.job+1<CareerProgression.JobNames.Length&&CareerProgression.JobUnlocked(p,ProfileTown(p),p.job+1));
   var settings=Button("global-settings");if(settings)settings.gameObject.SetActive(ScreenName!="settings"&&ScreenName!="pause");
   var resumeButton=Button("resume-footer");if(resumeButton)resumeButton.gameObject.SetActive(ScreenName!="results"&&ScreenName!="pause");
   SetLabel("machine-name",selected.definition.displayName);SetLabel("machine-kind",selected.definition.loader?"1.6 m³ bucket · Best for piles":"Front plow · Best for road clearing");SetLabel("machine-owned",selected.Owned?"OWNED / SELECTED":selected.definition.price.ToString("N0")+" COINS · LOCKED");SetLabel("machine-position",(PreviewIndex+1)+" / "+g.Fleet.Length+"  ·  Swipe to browse");
   SetLabel("machine-job",JobNames[Mathf.Clamp(JobIndex,0,2)]+" · "+selected.definition.displayName);Enable("start",selected.Owned&&!p.inJob);Enable("machine-shop",!selected.Owned);
   if(machinePicture)machinePicture.sprite=vehiclePictures[PreviewIndex];if(workshopPicture)workshopPicture.sprite=vehiclePictures[garageIndex];
   garageContent.SetActive(!ShopTab);shopContent.SetActive(ShopTab);
   overviewContent.SetActive(false);serviceContent.SetActive(!ShopTab);upgradesContent.SetActive(!ShopTab);
   if(workshopPreview){workshopPreview.previewCamera.enabled=ScreenName=="garage";workshopPreview.gameObject.SetActive(ScreenName=="garage");if(ScreenName=="garage")workshopPreview.Show(garageIndex,PartIndex,ShopTab);}
   if(tabImages.Length>=2){tabImages[0].color=ShopTab?tabColor:selectedColor;tabImages[1].color=ShopTab?selectedColor:tabColor;}
   for(int i=0;i<settingsTabImages.Length;i++)settingsTabImages[i].color=SettingsTab==i?selectedColor:tabColor;
   SetLabel("garage-name",ShopTab?(PartIndex==0?"Snow traction tyres":"Heavy-duty engine"):m.definition.displayName);
   SetLabel("garage-owned",ShopTab?"FOR "+m.definition.displayName:(m.Owned?(garageIndex==p.active?"OWNED / SELECTED":"OWNED"):m.definition.price.ToString("N0")+" COINS"));
   SetLabel("garage-status",ShopTab?(PartIndex==0?"TRACTION\nGrip bonus: +12% / level\nCurrent level: "+m.Tyres+" / 3":"POWER\nTorque bonus: +18% / level\nCurrent level: "+m.Engine+" / 3"):("VEHICLE STATS\nSpeed: "+(m.definition.maxSpeed*3.6f).ToString("F0")+" km/h\nFuel: "+Mathf.RoundToInt(m.FuelRatio*100)+"%\nCondition: "+m.Condition.ToString("F0")+"%\nTyres: "+m.Tyres+" / 3\nEngine: "+m.Engine+" / 3"));
   SetLabel("workshop-position",ShopTab?(PartIndex+1)+" / 2 PARTS":(garageIndex+1)+" / "+g.Fleet.Length+" VEHICLES");
   SetLabel("part-description",(PartIndex==0?"Snow traction tyres\n\n+12% grip per level.\nThree levels available.":"Heavy-duty engine\n\n+18% torque per level.\nThree levels available.")+"\n\nCompatible with: "+m.definition.displayName+"\n\nPurchased and fitted together at the depot.");
   Enable("drive",m.Owned);Enable("vehicle-buy",!m.Owned);SetLabel("drive",garageIndex==p.active?"Selected / Back":garageReturn==""&&p.inJob?"Select & Resume":"Select & Back");
   Enable("buy",!g.Fleet[1].Owned);Enable("owned",g.Fleet[1].Owned);
   bool service=m.Owned&&g.Active.AtDepot&&g.Active.Speed<=1;
   foreach(string action in new[]{"fuel","repair","tyres","engine"}){int level=action=="tyres"?m.Tyres:m.Engine;SetLabel(action,(action=="fuel"?"Refuel":action=="repair"?"Repair":action=="tyres"?"Tyres "+m.Tyres+" / 3":"Engine "+m.Engine+" / 3")+" · "+Quote(action)+" coins");Enable(action,service&&(action!="tyres"||m.Tyres<3)&&(action!="engine"||m.Engine<3));}
   if(upgradeSegments!=null)for(int i=0;i<upgradeSegments.Length;i++)upgradeSegments[i].color=i%3<(i<3?m.Tyres:m.Engine)?selectedColor:tabColor;
   string partAction=PartIndex==0?"tyres":"engine";bool max=(PartIndex==0?m.Tyres:m.Engine)>=3;
   SetLabel("part-buy",max?"OWNED / MAX LEVEL":"Buy & Fit · "+Quote(partAction)+" coins");Enable("part-buy",service&&!max);
   SetLabel("workshop-location",g.Active.AtDepot?"AT DEPOT · Stop to select, service or fit parts":"Return to the depot to select, service or fit parts");
   SetLabel("settings-sound",p.muted?"Sound: OFF":"Sound: ON");SetLabel("settings-quality",p.quality==0?"Graphics: LOW":"Graphics: STANDARD");SetLabel("settings-assist",p.assists?"Driving assists: ON":"Driving assists: OFF");SetLabel("settings-debug",p.debug?"Physics overlay: ON":"Physics overlay: OFF");for(int i=0;i<settingPages.Length;i++)settingPages[i].SetActive(i==SettingsTab);
   for(int i=0;i<RegionNames.Length;i++){bool unlocked=CareerProgression.RegionUnlocked(p,i);SetLabel("region-"+i,RegionNames[i]+"\n"+(unlocked?"UNLOCKED":"LOCKED · Complete previous region"));if(careerGallery)careerGallery.SetRegionUnlocked(i,unlocked);}
   SetLabel("town-title",RegionNames[Mathf.Clamp(RegionIndex,0,4)]);for(int i=0;i<4;i++){var b=Button("town-"+i);bool exists=i<Destinations[RegionIndex].Length;b.gameObject.SetActive(exists);if(exists){bool unlocked=CareerProgression.TownUnlocked(p,RegionIndex,i),available=CareerProgression.Available(CareerProgression.TownIds[RegionIndex][i]);SetLabel("town-"+i,Destinations[RegionIndex][i]+"\n"+(unlocked?(available?(CareerProgression.TownComplete(p,CareerProgression.TownIds[RegionIndex][i])?"COMPLETED / REPLAY":"AVAILABLE"):"UNLOCKED / COMING SOON"):"LOCKED"));b.interactable=unlocked&&available;}}
   SetLabel("pause-job",p.freeWork?"PINEFALL / FREE WORK":"PINEFALL / "+JobNames[Mathf.Clamp(p.job,0,2)]);Enable("finish",p.inJob&&p.freeWork);SetLabel("results",(string.IsNullOrEmpty(g.ResultSummary)?"Complete a shift to see your results.":g.ResultSummary)+"\n\nWallet: "+p.coins.ToString("N0")+" coins\nLevel "+g.Level+" · "+p.xp+" XP");
  }
  static string ProfileTown(ProfileSave p)=>string.IsNullOrEmpty(p.jobTownId)?CareerProgression.Pinefall:p.jobTownId;
  void OnDisable(){if(workshopPreview){workshopPreview.previewCamera.enabled=false;workshopPreview.gameObject.SetActive(false);}}
  void Update(){if(!Game)return;if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.2f;Refresh();}var k=Keyboard.current;if(k==null||PendingConfirmation!="")return;if(ScreenName=="machinery"||ScreenName=="garage"){if(k.leftArrowKey.wasPressedThisFrame)Browse(-1);if(k.rightArrowKey.wasPressedThisFrame)Browse(1);}}
 }
}
