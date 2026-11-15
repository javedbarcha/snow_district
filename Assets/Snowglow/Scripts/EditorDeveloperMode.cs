using UnityEngine;
namespace Snowglow {
 // No developer state or unlock bypass is available in player builds.
 public static class EditorDeveloperMode {
#if UNITY_EDITOR
  public static string PreferenceKey=>"Snowglow.DeveloperMode."+Application.dataPath;
  public const string ProfileKey="Snowglow.DeveloperMode.Profile";
  public static bool Enabled {get=>UnityEditor.EditorPrefs.GetBool(PreferenceKey,false);set=>UnityEditor.EditorPrefs.SetBool(PreferenceKey,value);}
  public static bool Active=>Enabled&&!UnityEditor.SessionState.GetBool("Snowglow.LevelChecks.Active",false)&&!UnityEditor.SessionState.GetBool("Snowglow.CollisionPlay.Active",false);
  public static ProfileSave Load(){
   var json=UnityEditor.SessionState.GetString(ProfileKey,"");
   var p=string.IsNullOrEmpty(json)?new ProfileSave{coins=1000000}:SaveStore.Decode(json);
   if(p==null)p=new ProfileSave{coins=1000000};p.developerUnlocked=true;return p;
  }
  public static void Store(ProfileSave profile)=>UnityEditor.SessionState.SetString(ProfileKey,JsonUtility.ToJson(profile));
#else
  public static bool Active=>false;
#endif
 }
}
