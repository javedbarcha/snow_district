using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Snowglow.Editor {
 public static class CareerSnowChecks {
  const string Folder="Logs/CareerSnow",Request="Logs/CareerSnow.request";
  static int checks;
  static void Check(bool value,string reason){checks++;if(!value)throw new InvalidOperationException("Career snow: "+reason);}
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){
   if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
   File.Delete(Request);Directory.CreateDirectory(Folder);
   try{
    var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=SceneValidation.ScenePath)throw new InvalidOperationException("Open Pinefall");
    var game=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<PrototypeSession>(true)).Single();ProductionMenuChecks.Run(game.HUD);SceneValidation.Validate(scene);MechanicsChecks.Run();RecoveryPhysicsChecks.Run();
    File.WriteAllText(Folder+"/Validation.txt","PASS: Native Career snow persistence/replay/migration/load conservation checks, existing UI/flow, scene/snow and recovery physics checks. No scene save, Play entry or player-save writes. Android packaging/device acceptance remains separate.");
   }catch(Exception e){File.WriteAllText(Folder+"/Validation.txt","FAIL: "+e);Debug.LogException(e);}
  }
  static int Cell(SnowVolumeGrid grid,float x,float z)=>grid.Index(x,z);
  static void ClearTarget(PrototypeSession game){
   var grid=game.Snow.Grid;int keep=0;
   for(int i=0;i<grid.depth.Length;i++)if(game.Snow.IsTarget(grid.X(i),grid.Z(i))){if(++keep%20==0)continue;grid.disposed+=grid.depth[i]*SnowVolumeGrid.Area;grid.depth[i]=grid.credit[i]=0;}
   game.Snow.Recount();Check(game.Snow.Progress>=.9f,"fixture completes only selected venue");game.Finish();
  }
  static float[] Region(SnowVolumeGrid grid,int kind){var bounds=SnowField.ContractBounds(kind);return Enumerable.Range(0,grid.depth.Length).Where(i=>CareerSnowState.Inside(bounds,grid.X(i),grid.Z(i))).Select(i=>grid.depth[i]).ToArray();}
  static void Ledger(PrototypeSession game,string label){Check(Mathf.Abs(game.Snow.Grid.initialVolume-game.Snow.Grid.Total()-game.Snow.Grid.disposed-game.Fleet.Sum(m=>m.Tool.Load))<.02f,label);}
  public static void Run(PrototypeSession game){
   checks=0;game.Profile=new ProfileSave();foreach(var machine in game.Fleet){machine.Tool.Load=0;machine.Owned=true;}game.StartJob(0);
   var seed=game.Snow.seed.grid;int outside=Cell(game.Snow.Grid,-65,-25);float removed=game.Snow.Grid.Take(outside,.04f,out float unused);game.Snow.Grid.disposed+=removed;float outsideDepth=game.Snow.Grid.depth[outside];
   ClearTarget(game);var first=Region(game.Snow.Grid,0);Check(first.Any(d=>d>.07f)&&first.Any(d=>d==0),"checkpoint retains exact partial residuals");
   Check(CareerSnowState.Find(game.Profile,"pinefall")!=null,"completion creates checkpoint");Ledger(game,"completion conserves current job snow");
   game.StartJob(1);Check(first.SequenceEqual(Region(game.Snow.Grid,0)),"next job preserves previous clearance exactly");Check(game.Snow.Grid.depth[outside]==outsideDepth,"town clearance outside venues survives");Check(game.Snow.Progress<.9f,"next venue has real work");Ledger(game,"next job establishes valid baseline");
   game.Profile.snow=game.Snow.Grid;string json=JsonUtility.ToJson(game.Profile);game.Profile=SaveStore.Decode(json);game.Snow.ReplaceGrid(game.StartupSnow());
   Check(game.Profile.inJob&&game.Profile.job==1&&first.SequenceEqual(Region(game.Snow.Grid,0)),"active Career resume retains town");Check(game.Snow.Grid.depth[outside]==outsideDepth,"saved active snow survives startup");Ledger(game,"resumed active ledger");
   ClearTarget(game);var second=Region(game.Snow.Grid,1);var committed=CareerSnowState.Find(game.Profile,"pinefall");string checkpoint=JsonUtility.ToJson(committed);
   game.Profile=SaveStore.Decode(JsonUtility.ToJson(game.Profile));game.Snow.ReplaceGrid(game.StartupSnow());Check(first.SequenceEqual(Region(game.Snow.Grid,0))&&second.SequenceEqual(Region(game.Snow.Grid,1)),"completed town survives idle save/reload");
   game.StartJob(0,false,true);Check(Region(game.Snow.Grid,0).SequenceEqual(Region(seed,0)),"Quick Job starts fresh snow");ClearTarget(game);Check(JsonUtility.ToJson(CareerSnowState.Find(game.Profile,"pinefall"))==checkpoint,"Quick completion cannot overwrite Career checkpoint");
   game.StartJob(2);Check(first.SequenceEqual(Region(game.Snow.Grid,0))&&second.SequenceEqual(Region(game.Snow.Grid,1)),"Career restores cleared town after Quick Job");game.Abandon();
   int coins=game.Profile.coins,xp=game.Profile.xp;game.StartJob(0);Check(Region(game.Snow.Grid,0).SequenceEqual(Region(seed,0)),"replay replenishes selected venue");Check(second.SequenceEqual(Region(game.Snow.Grid,1))&&game.Snow.Grid.depth[outside]==outsideDepth,"replay preserves other venue and town roads");Check(game.Snow.Progress<.9f,"replay cannot pay instantly");game.Finish();Check(game.Profile.inJob&&game.Profile.coins==coins&&game.Profile.xp==xp,"fresh replay grants no premature rewards");game.Abandon();
   Check(first.SequenceEqual(Region(game.Snow.Grid,0))&&second.SequenceEqual(Region(game.Snow.Grid,1)),"abandoning replay restores last completed town");
   game.StartJob(0,true);ClearTarget(game);Check(JsonUtility.ToJson(CareerSnowState.Find(game.Profile,"pinefall"))==checkpoint,"Free Work leaves Career checkpoint unchanged");game.StartJob(2);Check(first.SequenceEqual(Region(game.Snow.Grid,0)),"Career survives Free Work");game.Abandon();
   // Held snow is deposited at its old mouth location when the town is restored,
   // with any undepositable remainder retained in that vehicle's bucket.
   game.Profile=new ProfileSave();game.StartJob(0);int loadCell=Cell(game.Snow.Grid,-65,0);float load=game.Snow.Grid.Take(loadCell,.05f,out unused);game.Fleet[1].Tool.Load=load;
   ClearTarget(game);float ground=game.Snow.Grid.Total();Check(game.Fleet[1].Tool.Load==load,"completion does not alter current vehicle load");game.StartJob(1);
   Check(Mathf.Abs(game.Snow.Grid.Total()+game.Fleet.Sum(m=>m.Tool.Load)-ground-load)<.02f,"transition conserves held snow");Ledger(game,"held snow transition ledger");game.Abandon();
   var full=CareerSnowState.FromSeed(seed);for(int i=0;i<full.depth.Length;i++)full.depth[i]=2.5f;
   var state=CareerSnowState.Capture("pinefall",full,seed,Array.Empty<MachineController>());state.loads=new[]{new CareerSnowLoad{vehicle=game.Fleet[1].definition.id,position=Vector3.zero,volume=.4f}};
   var restored=state.Restore(seed,out var remaining);Check(remaining.TryGetValue(game.Fleet[1].definition.id,out float retained)&&Mathf.Abs(retained-.4f)<.0001f,"full world retains undepositable load");Check(Mathf.Abs(restored.initialVolume-restored.Total()-.4f)<.02f,"full world retained load ledger");
   game.Profile=new ProfileSave{careerCompletedJobs=new[]{CareerProgression.Key("pinefall",0)},coins=927,xp=180};game.Snow.ReplaceGrid(game.StartupSnow());Check(Region(game.Snow.Grid,0).All(d=>d==0)&&Region(game.Snow.Grid,1).SequenceEqual(Region(seed,1)),"old completed profile migrates only completed venue");Check(game.Profile.coins==927&&game.Profile.xp==180,"migration grants no rewards");
   game.Profile=new ProfileSave{inJob=true,job=1,careerCompletedJobs=new[]{CareerProgression.Key("pinefall",0)},snow=CareerSnowState.FromSeed(seed)};game.Profile.snow.initialVolume=game.Profile.snow.Total();int active=Cell(game.Profile.snow,90,20);game.Profile.snow.depth[active]=.17f;game.Profile.snow.initialVolume=game.Profile.snow.Total();game.Snow.ReplaceGrid(game.StartupSnow());
   Check(Region(game.Snow.Grid,0).All(d=>d==0)&&game.Snow.Grid.depth[active]==.17f&&game.Profile.inJob&&game.Profile.job==1,"old active Career migration preserves pending job work");
   Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/Checks.txt","PASS: "+checks+" Career snow checks: exact completed venue/town continuity, active and idle save reload, selected replay only, abandoned replay rollback, Quick/Free isolation, older-save migration without rewards, held snow conservation and blocked-drop remainder. Fixture-only, no real save writes.");
  }
 }
}
