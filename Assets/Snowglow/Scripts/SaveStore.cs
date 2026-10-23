using System;
using System.IO;
using UnityEngine;
namespace Snowglow {
 [Serializable]public sealed class MachineSave {
  public string id;public bool owned;public float fuel,condition,load,lift,tilt,angle;public bool auto=true,working=true;
  public int tyres,engine;public Vector3 position;public Quaternion rotation;
 }
 [Serializable]public sealed class ProfileSave {
  public int schema=2,coins=1800,xp,job,active;public bool freeWork,inJob;public float remaining=720,earned,chain,chainTime;
  public string transaction="",lastReward="",lastDummy="";public bool muted,assists=true,debug;public int quality=1;
  public MachineSave[] machines;public SnowVolumeGrid snow;
 }
 public static class SaveStore {
  public static string PathName=>Path.Combine(Application.persistentDataPath,"snowglow-v2.json");
  public static ProfileSave Load(){foreach(string path in new[]{PathName,PathName+".bak"})try{if(File.Exists(path)){var p=JsonUtility.FromJson<ProfileSave>(File.ReadAllText(path));if(p!=null&&p.schema==2)return p;}}catch(Exception e){Debug.LogWarning("Save recovery: "+e.Message);}return new ProfileSave();}
  public static bool Write(ProfileSave p){try{
   string tmp=PathName+".tmp";File.WriteAllText(tmp,JsonUtility.ToJson(p));
   if(File.Exists(PathName))File.Replace(tmp,PathName,PathName+".bak");else File.Move(tmp,PathName);return true;
  }catch(Exception e){Debug.LogWarning("Save failed: "+e.Message);return false;}}
 }
}
