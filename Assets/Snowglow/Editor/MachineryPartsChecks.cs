using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Snowglow.Editor {
 public static class MachineryPartsChecks {
  const string Folder="Logs/MachineryParts";static int checks;
  static void Check(bool value,string reason){checks++;if(!value)throw new InvalidOperationException("Machinery parts: "+reason);}
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){
   const string request="Logs/MachineryParts.request";
   if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
   File.Delete(request);Directory.CreateDirectory(Folder);
   try{
    var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=SceneValidation.ScenePath)throw new InvalidOperationException("Open Pinefall");
    File.Copy(scene.path,Folder+"/Pinefall-before-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".unity");
    if(scene.isDirty&&!EditorSceneManager.SaveScene(scene,Folder+"/Pinefall-unsaved-before.unity",true))throw new IOException("Unsaved scene backup failed");
    var game=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<PrototypeSession>(true)).Single();
    WorkshopPreviewAuthoring.BuildParts(game.HUD.productionMenu.workshopPreview,game.Fleet);ProductionMenuAuthoring.ConfigurePartsUI(game.HUD.productionMenu);
    SceneValidation.Validate(scene);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed");AssetDatabase.SaveAssets();
    scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);game=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<PrototypeSession>(true)).Single();
    ProductionMenuChecks.Run(game.HUD);SceneValidation.Validate(scene);MechanicsChecks.Run();RecoveryPhysicsChecks.Run();LoaderCycleChecks.Run();
    File.WriteAllText(Folder+"/Validation.txt","PASS: saved/reopened 14 part previews, purchase/fit/individual upgrades/save migration and UI/flow/Career snow/scene/snow regressions. No Play entry or real player-save writes. Handling balance, Android packaging and device performance remain untested.");
   }catch(Exception e){File.WriteAllText(Folder+"/Validation.txt","FAIL: "+e);Debug.LogException(e);}
  }
  public static void Run(PrototypeSession game,ProductionMenu ui){
   checks=0;game.Profile=new ProfileSave{coins=20000,legacyUnlockedJobs=3};
   Check(ui.upgradeSegments.Length==9&&ui.Button("part-upgrade"),"saved three upgrade slots for every category and separate Shop upgrade action");
   Check(ui.workshopPreview.parts.Length==game.Fleet.Length*7,"saved seven previews per machine");
   foreach(var m in game.Fleet){
    m.RestoreParts(null);m.Owned=true;m.RecoverToDepot();
    Check(Enumerable.Range(0,7).Count(i=>MachineryParts.At(m.definition.loader,i).kind==MachineryPartKind.Tyres)==3,"three tyre types");
    Check(Enumerable.Range(0,7).Count(i=>MachineryParts.At(m.definition.loader,i).kind==MachineryPartKind.Engine)==2,"two engines");
    Check(Enumerable.Range(0,7).Count(i=>MachineryParts.At(m.definition.loader,i).kind==MachineryPartKind.Blade)==2,"two blades/buckets");
    Check(Enumerable.Range(0,7).Count(m.PartOwned)==3&&m.GripMultiplier==1&&m.EngineMultiplier==1&&m.SnowRateMultiplier==1,"only three factory parts owned and default performance unchanged");
    m.RestoreParts(new MachineSave{tyres=2,engine=3});Check(m.Tyres==2&&m.Engine==3&&m.Blade==0&&m.FittedPart(MachineryPartKind.Tyres)==0,"legacy levels migrate to defaults");
    m.RestoreParts(null);
   }
   Check(MachineryParts.Id(game.Fleet[0],2)!=MachineryParts.Id(game.Fleet[1],2),"vehicle-specific identities");
   for(int vehicle=0;vehicle<game.Fleet.Length;vehicle++){
    game.Profile.coins=20000;game.Profile.active=vehicle;var m=game.Active;m.SetActiveMachine(true);game.Open("dashboard");game.Open("garage");ui.Execute("workshop:shop");
    for(int index=0;index<7;index++){
     Check(ui.PartIndex==index,"Shop catalogue order");var part=MachineryParts.At(m.definition.loader,index);int before=game.Profile.coins;
     Check(ui.Label("garage-name").text==part.name&&ui.workshopPreview.parts[vehicle*7+index].activeSelf,"name and matching 3D preview");
     if(!m.PartOwned(index)){
      ui.Execute("upgrade-part");ui.Execute("accept");Check(game.Profile.coins==before&&m.PartLevel(index)==0,"unowned part cannot upgrade");
      ui.Execute("buy-part");ui.Execute("cancel");Check(!m.PartOwned(index)&&game.Profile.coins==before,"cancelled purchase does nothing");
      game.Profile.coins=0;ui.Execute("buy-part");Check(!ui.Button("accept").interactable,"insufficient funds disables confirmation");ui.Execute("accept");Check(!m.PartOwned(index),"direct insufficient-funds acceptance rejected");game.Profile.coins=before;
      ui.Execute("buy-part");ui.Execute("accept");Check(m.PartOwned(index)&&m.FittedPart(part.kind)==index&&game.Profile.coins==before-part.price&&m.PartLevel(index)==0,"purchase fits part without giving upgrades");
      int charged=game.Profile.coins;ui.Execute("accept");Check(game.Profile.coins==charged,"duplicate buy confirmation inert");
     }
     for(int level=0;level<3;level++){before=game.Profile.coins;ui.Execute("upgrade-part");ui.Execute("accept");Check(m.PartLevel(index)==level+1&&game.Profile.coins==before-(200+level*200),"individual upgrade level and cost");}
     before=game.Profile.coins;ui.Execute("upgrade-part");ui.Execute("accept");Check(m.PartLevel(index)==3&&game.Profile.coins==before,"fourth upgrade rejected");
     Check(Mathf.Abs(m.PartMultiplier(part.kind)-part.multiplier*(1+3*part.PerLevel))<.0001f,"fitted part effect uses base and own upgrades");
     ui.Browse(1);
    }
    Check(ui.PartIndex==0,"seven-part wrap");int funds=game.Profile.coins;
    ui.Execute("buy-part");ui.Execute("accept");Check(m.FittedPart(MachineryPartKind.Tyres)==0&&m.Tyres==3&&game.Profile.coins==funds,"refit owned default free with preserved upgrades");
    m.RestoreParts(null);m.SetPartOwned(2);m.FitPart(2);m.UpgradePart(2);m.FitPart(0);m.UpgradePart(0);m.UpgradePart(0);m.FitPart(2);Check(m.Tyres==1,"different tyres keep independent levels");ui.Execute("upgrade-part");ui.Execute("accept");Check(m.PartLevel(0)==3&&m.Tyres==1&&m.FittedPart(MachineryPartKind.Tyres)==2,"upgrading an owned unequipped part leaves fitted tyre and grip unchanged");
    m.SetPartOwned(4);m.FitPart(4);m.UpgradePart(4);m.UpgradePart(4);m.SetPartOwned(6);m.FitPart(6);for(int level=0;level<3;level++)m.UpgradePart(6);
    var save=new MachineSave{id=m.definition.id,tyres=m.Tyres,engine=m.Engine,parts=m.SaveParts(),fittedParts=m.SaveFittedParts()};var profile=SaveStore.Decode(JsonUtility.ToJson(new ProfileSave{machines=new[]{save}}));m.RestoreParts(profile.machines[0]);
    Check(m.FittedPart(MachineryPartKind.Tyres)==2&&m.Tyres==1&&m.PartLevel(0)==3&&m.PartOwned(2)&&m.Engine==2&&m.Blade==3&&m.FittedPart(MachineryPartKind.Engine)==4&&m.FittedPart(MachineryPartKind.Blade)==6,"JSON roundtrip restores ownership, selection and independent levels");
    Check(!m.FitPart(3)&&m.FittedPart(MachineryPartKind.Tyres)==2,"unowned fitting refused");
    var other=game.Fleet[1-vehicle];var foreign=MachineryParts.Normalize(other,save.parts);Check(!foreign[2].owned&&foreign[0].level==0,"other vehicle cannot inherit purchased parts");
    game.Open("dashboard");game.Open("garage");ui.Execute("workshop:shop");ui.Browse(2);ui.Execute("upgrade-part");ui.Browse(1);Check(ui.PartIndex==2,"confirmation locks browsing");ui.Execute("cancel");
    m.transform.position=Vector3.zero;funds=game.Profile.coins;game.UpgradePart(2);game.BuyOrFitPart(3);Check(game.Profile.coins==funds&&!m.PartOwned(3)&&m.Tyres==1,"off-depot mutations rejected");m.RecoverToDepot();
    m.Body.linearVelocity=Vector3.forward*2;game.BuyOrFitPart(3);Check(!m.PartOwned(3),"moving service rejected");m.ResetMotion();
    m.Owned=false;game.BuyOrFitPart(3);Check(!m.PartOwned(3),"unowned machine cannot buy parts");m.Owned=true;
    m.RestoreParts(new MachineSave{parts=new[]{new MachineryPartSave{id=MachineryParts.Id(m,0),owned=false,level=99},new MachineryPartSave{id=MachineryParts.Id(m,2),owned=false,level=3}},fittedParts=new[]{MachineryParts.Id(m,2)}});
    Check(m.PartOwned(0)&&m.Tyres==3&&!m.PartOwned(2)&&m.FittedPart(MachineryPartKind.Tyres)==0&&m.PartLevel(2)==0,"invalid saved levels and unowned fit normalized");
    m.RestoreParts(null);game.Open("dashboard");
   }
   game.Profile.coins=20000;game.Profile.active=0;game.Notice="Choose equipment in Shop, or upgrade the fitted parts in Garage.";game.Open("garage");ProductionMenuChecks.Capture(game.HUD,"parts-garage",1280,720);ui.Execute("workshop:shop");ui.Browse(6);ProductionMenuChecks.Capture(game.HUD,"parts-blade",1280,720);ui.Browse(-2);ProductionMenuChecks.Capture(game.HUD,"parts-engine",1280,720);ui.Browse(-1);ProductionMenuChecks.Capture(game.HUD,"parts-tyres",1280,720);
   Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/Checks.txt","PASS: "+checks+" native checks: catalogue counts, legacy migration, JSON loadouts, per-part levels/caps/effects, independent vehicle ownership, cancel/insufficient/duplicate transactions, refitting and location/motion/ownership gates. Fixture only; no real saves or Play entry.");
  }
 }
}
