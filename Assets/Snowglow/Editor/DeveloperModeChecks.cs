using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
namespace Snowglow.Editor {
 [InitializeOnLoad] public static class DeveloperModeChecks {
  const string Key="Snowglow.DeveloperChecks",Folder="Logs/DeveloperMode";
  static DeveloperModeChecks(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=state=>{
   if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Key,false))Finish();
  };}
  static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
  static string Hash(string path){if(!File.Exists(path))return "missing";using(var hash=SHA256.Create())return Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(path)));}
  static void Finish(){
   try{Check(Hash(SaveStore.PathName)==SessionState.GetString(Key+".Save",""),"real save changed");Check(Hash(SaveStore.PathName+".bak")==SessionState.GetString(Key+".Backup",""),"backup changed");
    File.AppendAllText(Folder+"/Checks.txt","\nReal save and backup SHA256 unchanged after Play exit.");
   }catch(Exception e){File.AppendAllText(Folder+"/Checks.txt","\nFAIL: "+e);Debug.LogException(e);}
   finally{EditorDeveloperMode.Enabled=SessionState.GetBool(Key+".Prior",false);SessionState.EraseString(EditorDeveloperMode.ProfileKey);SessionState.SetBool(Key,false);}
  }
  static void Tick(){try{
   if(File.Exists(Folder+".request")&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&!EditorApplication.isUpdating){
    File.Delete(Folder+".request");Directory.CreateDirectory(Folder);
    Check(!UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,"save scene before checks");
    SessionState.SetBool(Key+".Prior",EditorDeveloperMode.Enabled);SessionState.SetString(Key+".Save",Hash(SaveStore.PathName));SessionState.SetString(Key+".Backup",Hash(SaveStore.PathName+".bak"));
    SessionState.SetBool(Key,true);SessionState.SetBool(Key+".Ran",false);SessionState.SetFloat(Key+".Start",(float)EditorApplication.timeSinceStartup);
    EditorDeveloperMode.Enabled=true;EditorApplication.isPlaying=true;return;
   }
   if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
   Check(EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+".Start",0)<180,"Play checks timed out");
   var game=PrototypeSession.Instance;if(!game||game.Profile==null||!game.Active.Body||SessionState.GetBool(Key+".Ran",false))return;
   SessionState.SetBool(Key+".Ran",true);
   Check(game.Profile.developerUnlocked&&game.Profile.coins==1000000,"isolated funded developer profile");
   Check(game.Profile.careerCompletedJobs.Length==0,"unlock must not complete jobs or clear Career snow");
   Check(game.Fleet.All(m=>m.Owned),"all vehicles owned");
   foreach(var machine in game.Fleet)for(int i=0;i<MachineryParts.Count;i++)Check(machine.PartOwned(i)&&machine.PartLevel(i)==0,"all parts owned with stock upgrade levels");
   for(int r=0;r<CareerProgression.TownIds.Length;r++){Check(CareerProgression.RegionUnlocked(game.Profile,r),"region unlocked");for(int t=0;t<CareerProgression.TownIds[r].Length;t++)Check(CareerProgression.TownUnlocked(game.Profile,r,t),"town browse unlocked");}
   Check(CareerProgression.QuickJobs(game.Profile,new System.Random(1)).Length==6,"all six built town jobs available");
   Check(!CareerProgression.Available("svalbard")&&!CareerProgression.JobUnlocked(game.Profile,"svalbard",0),"unbuilt scene stays unavailable");
   var normal=new ProfileSave();Check(!CareerProgression.TownIsUnlocked(normal,"alps")&&!CareerProgression.JobUnlocked(normal,"pinefall",1),"normal progression preserved");
   game.Active.UpgradePart(0);game.Save();var restored=SaveStore.Load();Check(restored.developerUnlocked&&restored.machines[0].parts.Any(p=>p.level==1),"test saves and upgrades survive session load");
   Check(!JsonUtility.ToJson(restored).Contains("developerUnlocked"),"unlock flag never serialized");
   EditorDeveloperMode.Enabled=false;Check(!SaveStore.Load().developerUnlocked,"toggle off loads real profile");
   // Even an old developer profile cannot accidentally write to the real file.
   Check(SaveStore.Write(restored),"marked developer profile write remains isolated after disabling");EditorDeveloperMode.Enabled=true;
   File.WriteAllText(Folder+"/Checks.txt","PASS: actual Play developer startup, all vehicle/part ownership, adjustable upgrades, region/town access, six built jobs, unbuilt scene guard, normal progression, isolated save roundtrip, toggle-off restore and defensive write routing.");EditorApplication.isPlaying=false;
  }catch(Exception e){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/Checks.txt","FAIL: "+e);Debug.LogException(e);if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;else if(SessionState.GetBool(Key,false))Finish();}}
 }
}
