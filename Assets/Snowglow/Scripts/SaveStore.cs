using System;
using System.IO;
using UnityEngine;
namespace Snowglow {
 [Serializable]public sealed class MachineSave {
  public string id;public bool owned;public float fuel,condition,load,lift,tilt,angle;public bool auto=true,working=true;
  public int tyres,engine;public MachineryPartSave[] parts;public string[] fittedParts;public Vector3 position;public Quaternion rotation;
 }
 [Serializable]public sealed class ProfileSave {
  [NonSerialized]public bool developerUnlocked;
  public int schema=2,coins=1800,xp,job,active;public bool freeWork,inJob;public float remaining=720,earned,chain,chainTime;
  public string transaction="",lastReward="",lastDummy="";public bool muted,assists=true,debug;public int quality=1;
  public MachineSave[] machines;public SnowVolumeGrid snow;
  public CareerSnowState[] careerSnow=Array.Empty<CareerSnowState>();
  public int careerVersion=1,legacyUnlockedJobs;public string[] careerCompletedJobs=new string[0];
  public string jobTownId=CareerProgression.Pinefall;public bool quickPlay,pendingTownStart;
 }
 public static class SaveStore {
  public static string PathName=>Path.Combine(Application.persistentDataPath,"snowglow-v2.json");
  public static ProfileSave Decode(string json){var p=JsonUtility.FromJson<ProfileSave>(json);if(p==null||p.schema!=2)return null;CareerProgression.Normalize(p,!json.Contains("\"careerVersion\""));return p;}
  public static ProfileSave Load(){
#if UNITY_EDITOR
   if(UnityEditor.SessionState.GetBool("Snowglow.LevelChecks.Active",false))return Decode(UnityEditor.SessionState.GetString("Snowglow.LevelChecks.Profile","{}"))??new ProfileSave();
   if(EditorDeveloperMode.Active)return EditorDeveloperMode.Load();
#endif
   foreach(string path in new[]{PathName,PathName+".bak"})try{if(File.Exists(path)){var p=Decode(File.ReadAllText(path));if(p!=null)return p;}}catch(Exception e){Debug.LogWarning("Save recovery: "+e.Message);}return new ProfileSave();}
  public static bool Write(ProfileSave p){try{
#if UNITY_EDITOR
   if(p.developerUnlocked||EditorDeveloperMode.Active){EditorDeveloperMode.Store(p);return true;}
#endif
   string tmp=PathName+".tmp";File.WriteAllText(tmp,JsonUtility.ToJson(p));
   if(File.Exists(PathName))File.Replace(tmp,PathName,PathName+".bak");else File.Move(tmp,PathName);return true;
  }catch(Exception e){Debug.LogWarning("Save failed: "+e.Message);return false;}}
 }
}
