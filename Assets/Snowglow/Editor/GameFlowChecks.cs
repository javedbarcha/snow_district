using System;
using System.IO;
using System.Linq;
using UnityEngine;
namespace Snowglow.Editor {
 // Runs in ProductionMenuChecks' disposable scene with session ready=false.
 public static class GameFlowChecks {
  static int checks;
  static void Check(bool ok,string message){checks++;if(!ok)throw new InvalidOperationException("Game flow: "+message);}
  static void Finish(PrototypeSession game){Array.Clear(game.Snow.Grid.depth,0,game.Snow.Grid.depth.Length);game.Snow.Recount();Check(game.Snow.Progress>=.9f,"Completion fixture clears the target");game.Finish();}
  public static void Run(PrototypeSession game,ProductionMenu ui){
   checks=0;game.Profile=new ProfileSave();foreach(var m in game.Fleet){m.Owned=true;m.Condition=80;m.Fuel=m.definition.fuelCapacity*.5f;}game.Open("dashboard");ui.Execute("career");ui.Execute("region:0");ui.Execute("town:0");
   Check(CareerProgression.JobUnlocked(game.Profile,"pinefall",0)&&!CareerProgression.JobUnlocked(game.Profile,"pinefall",1),"New career starts with one job");
   Check(ui.Button("job-0").interactable&&!ui.Button("job-1").interactable&&!ui.Button("job-2").interactable,"Career lock presentation");
   ProductionMenuChecks.Capture(game.HUD,"flow-career-new",1280,720);
   string transaction=game.Profile.transaction;game.StartJob(1);Check(!game.Profile.inJob&&game.Profile.transaction==transaction,"Runtime rejects locked jobs");
   ui.Execute("job:0");Check(game.IsRunning&&game.Profile.inJob&&!game.Profile.quickPlay&&game.Profile.active==0,"Career starts directly with Garage selection");
   Finish(game);Check(game.Panel=="results"&&CareerProgression.Completed(game.Profile,"pinefall",0)&&CareerProgression.JobUnlocked(game.Profile,"pinefall",1),"Career completion unlocks next job");
   int coins=game.Profile.coins,xp=game.Profile.xp;game.Finish();Check(game.Profile.coins==coins&&game.Profile.xp==xp,"Duplicate completion grants nothing");
   ui.Execute("replay");Check(game.IsRunning&&!game.Profile.quickPlay&&game.Profile.job==0,"Career replay keeps mode");Finish(game);Check(game.Profile.careerCompletedJobs.Length==1,"Career replay does not duplicate completion");
   ui.Execute("next-job");Check(game.Panel=="contracts"&&!ui.QuickJob,"Career results returns to next jobs");
   ui.Execute("quick");Check(ui.QuickJob&&ui.JobAtSlot(0).HasValue&&ui.JobAtSlot(1).HasValue&&!ui.JobAtSlot(2).HasValue,"Quick Play contains only two unlocked jobs");
   bool swapped=false;for(int seed=0;seed<100;seed++){var pool=CareerProgression.QuickJobs(game.Profile,new System.Random(seed));Check(pool.Length==2&&pool.All(j=>j.town=="pinefall"&&j.kind<2)&&pool.Select(j=>j.kind).Distinct().Count()==2,"Shuffle is unlocked-only and duplicate-free");if(pool[0].kind==1)swapped=true;}Check(swapped,"Shuffle varies order");
   ProductionMenuChecks.Capture(game.HUD,"flow-quick-unlocked",1280,720);
   int slot=ui.JobAtSlot(0).Value.kind==1?0:1;ui.Execute("job:"+slot);Check(game.Profile.quickPlay&&game.Profile.job==1&&game.IsRunning,"Quick Play starts shuffled selection");Finish(game);
   Check(game.Profile.coins>coins&&game.Profile.xp>xp&&!CareerProgression.Completed(game.Profile,"pinefall",1)&&!CareerProgression.JobUnlocked(game.Profile,"pinefall",2),"Quick Play rewards without Career unlocks");
   ui.Execute("replay");Check(game.IsRunning&&game.Profile.quickPlay&&game.Profile.job==1,"Quick replay keeps mode and job");
   string active=game.Profile.transaction;coins=game.Profile.coins;ui.Execute("career");ui.Execute("region:0");ui.Execute("town:0");ui.Execute("job:1");
   Check(ui.PendingConfirmation=="replace-job"&&game.Profile.transaction==active&&game.Profile.quickPlay,"Switching mode protects active job");ui.Execute("cancel");Check(game.Profile.transaction==active&&game.Profile.inJob,"Closing conflict retains job");ui.Execute("job:1");ui.Execute("dialog-secondary");Check(game.IsRunning&&game.Profile.transaction==active,"Conflict Resume returns to old job");
   ui.Execute("career");ui.Execute("region:0");ui.Execute("town:0");ui.Execute("job:1");ui.Execute("accept");Check(game.IsRunning&&!game.Profile.quickPlay&&game.Profile.transaction!=active&&game.Profile.coins==coins,"Confirmed replacement starts Career without old reward");Finish(game);
   Check(CareerProgression.JobUnlocked(game.Profile,"pinefall",2),"Career unlocks Main Street");ui.Execute("next-job");ui.Execute("job:2");Finish(game);ui.Execute("next-job");
   Check(game.Panel=="towns"&&CareerProgression.TownComplete(game.Profile,"pinefall")&&CareerProgression.TownUnlocked(game.Profile,0,1),"Completed town unlocks next town");
   Check(ui.Label("town-1").text.Contains("UNLOCKED / COMING SOON")&&!ui.Button("town-1").interactable,"Unbuilt unlocked town is never playable");ui.Execute("town:1");Check(game.Panel=="towns","Planned destination cannot start");game.StartJob(0,false,false,"alps");Check(!game.Profile.inJob,"Runtime rejects unavailable town");
   ProductionMenuChecks.Capture(game.HUD,"flow-next-town",1280,720);
   var roundtrip=SaveStore.Decode(JsonUtility.ToJson(game.Profile));Check(roundtrip.careerCompletedJobs.Length==3&&CareerProgression.TownComplete(roundtrip,"pinefall"),"Career progression roundtrip");
   var legacy=SaveStore.Decode("{\"schema\":2,\"coins\":927,\"xp\":123,\"inJob\":true,\"job\":2,\"remaining\":456,\"transaction\":\"keep-me\",\"machines\":[{\"id\":\"starter\",\"fuel\":17,\"condition\":61,\"load\":0.4,\"owned\":true}]}");
   Check(legacy.coins==927&&legacy.xp==123&&legacy.inJob&&legacy.job==2&&legacy.remaining==456&&legacy.transaction=="keep-me"&&legacy.machines[0].fuel==17&&legacy.machines[0].load==.4f,"Legacy migration preserves active job, economy and machine state");Check(legacy.legacyUnlockedJobs==3&&legacy.careerCompletedJobs.Length==0,"Legacy access preserved without invented completions");
   Check(SaveStore.Decode(JsonUtility.ToJson(new ProfileSave())).legacyUnlockedJobs==0,"New saves do not receive legacy unlocks");
   var oldSnow=new ProfileSave{inJob=true,quickPlay=true,snow=new SnowVolumeGrid()};oldSnow.snow.depth[9]=.31f;oldSnow.snow.credit[9]=.17f;oldSnow.snow.disposed=4;oldSnow.snow.initialVolume=8;
   var restoredSnow=SaveStore.Decode(JsonUtility.ToJson(oldSnow).Replace("\"careerVersion\":1,",""));Check(restoredSnow.snow.depth[9]==.31f&&restoredSnow.snow.credit[9]==.17f&&restoredSnow.snow.disposed==4&&restoredSnow.snow.initialVolume==8&&restoredSnow.quickPlay,"Legacy progression migration retains snow ledger and mode");
   game.Profile=new ProfileSave();game.StartJob(0,false,true);active=game.Profile.transaction;game.Snow.Grid.depth[0]=.37f;float snow=game.Snow.Grid.Total();game.Profile=SaveStore.Decode(JsonUtility.ToJson(game.Profile));game.Open("dashboard");ui.Execute("continue");Check(game.IsRunning&&game.Profile.quickPlay&&game.Profile.transaction==active&&game.Snow.Grid.Total()==snow,"Saved Quick Play mode resumes without resetting snow");
   game.Open("pause");ui.Navigate("garage");ui.Execute("workshop:shop");ui.Browse(1);int part=ui.PartIndex;ui.Navigate("settings");ui.Back();Check(game.Panel=="garage"&&ui.ShopTab&&ui.PartIndex==part,"Settings returns to same Garage tab and part");ui.Navigate("store");ui.Back();Check(ui.ShopTab&&ui.PartIndex==part,"Transactions return to same Shop selection");ui.Back();Check(game.Panel=="pause"&&Time.timeScale==0&&game.Profile.transaction==active,"Garage from Pause returns to Pause");
   game.Continue();ui.Navigate("garage");ui.Execute("garage:1");ui.Execute("drive");Check(game.IsRunning&&game.Profile.active==1&&game.Profile.transaction==active,"In-game Garage switches and resumes same job");
   game.Open("dashboard");Check(ui.Button("continue-job").interactable&&game.Profile.transaction==active,"Main Menu preserves shift and Continue");ui.Navigate("garage");ui.Back();Check(game.Panel=="dashboard"&&game.Profile.inJob,"Main-menu Garage returns to Main Menu");ui.Execute("continue");
   game.Active.transform.position=new Vector3(20,6,20);game.Active.Body.position=game.Active.transform.position;ui.Navigate("garage");ui.Execute("garage:0");ui.Execute("drive");Check(game.Panel=="garage"&&game.Profile.active==1&&game.Notice.Contains("depot"),"Rejected switch keeps a usable Garage");ui.Back();Check(game.IsRunning,"Rejected switch can return to game");
   game.Open("pause");coins=game.Profile.coins;xp=game.Profile.xp;game.Active.Tool.Load=.4f;ui.Confirm("abandon");ui.Execute("cancel");Check(game.Profile.inJob&&game.Active.Tool.Load==.4f,"Cancelled abandonment preserves load");ui.Confirm("abandon");ui.Execute("accept");Check(!game.Profile.inJob&&game.Panel=="dashboard"&&game.Profile.coins==coins&&game.Profile.xp==xp&&game.Fleet.All(m=>m.Tool.Load==0),"Abandon resets work but preserves earned wallet/ownership");game.Finish();Check(game.Profile.coins==coins,"Abandoned shift cannot grant reward");
   ui.Navigate("garage");ui.Execute("garage:0");ui.Execute("drive");Check(!game.Profile.inJob&&game.Panel=="dashboard"&&Time.timeScale==0,"No-job Garage selection returns to menu");game.Open("");Check(game.Panel=="contracts"&&Time.timeScale==0,"No-job gameplay entry is guarded");game.Open("machinery");Check(game.Panel=="garage"&&ui.ScreenName=="garage","Legacy machinery route redirects to Garage");
   game.Profile=new ProfileSave();game.StartJob(0,true);game.Profile.earned=10;game.Finish();Check(game.Profile.careerCompletedJobs.Length==0&&game.Profile.xp==0,"Free Work cannot advance Career");
   Directory.CreateDirectory("Logs/GameFlow");File.WriteAllText("Logs/GameFlow/Validation.txt","PASS: "+checks+" actual-session and progression assertions. Career locks/completion/town chain, Quick Play shuffle/rewards without unlocks, replay/next paths, conflict resume/cancel/replace, planned-content guards, versioned legacy/new-save roundtrip, mode restoration, Garage origin/context/tab selection, paused menus, depot rejection, no-job gameplay prevention, abandonment and reward idempotency. Completion fixture clears transient snow to test Finish; this is not a driving test. No player-save writes. Android package and phone traversal/performance remain untested.");
  }
 }
}
