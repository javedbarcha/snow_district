using UnityEditor;
namespace Snowglow.Editor {
 [InitializeOnLoad] public static class DeveloperModeMenu {
  const string Path="Snowglow/Developer/Unlock everything (Editor only)";
  static DeveloperModeMenu(){EditorApplication.playModeStateChanged+=state=>{
   if(state==PlayModeStateChange.ExitingEditMode)SessionState.EraseString(EditorDeveloperMode.ProfileKey);
  };}
  [MenuItem(Path)] static void Toggle(){EditorDeveloperMode.Enabled=!EditorDeveloperMode.Enabled;Menu.SetChecked(Path,EditorDeveloperMode.Enabled);}
  [MenuItem(Path,true)] static bool Validate(){Menu.SetChecked(Path,EditorDeveloperMode.Enabled);return !EditorApplication.isPlayingOrWillChangePlaymode;}
 }
}
