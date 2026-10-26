using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Snowglow.Editor {
 public static class SnowResolutionAuthoring {
  const string Request="Logs/ResizeSnow.request";
  [InitializeOnLoadMethod]static void OnReload(){EditorApplication.update+=Pending;}
  static void Pending(){
   if(Application.isBatchMode||!File.Exists(Request)){EditorApplication.update-=Pending;return;}
   if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)return;
   EditorApplication.update-=Pending;
   if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty){Debug.Log("Snowglow: save the scene, then choose Snowglow > Rebuild snow at 50 cm resolution.");return;}
   Rebuild();
  }
  [MenuItem("Snowglow/Rebuild snow at 50 cm resolution")]
  public static void Rebuild(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before rebuilding saved snow.");
   if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   var scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath,OpenSceneMode.Single);
   SnowField snow=null;foreach(var root in scene.GetRootGameObjects()){snow=root.GetComponentInChildren<SnowField>(true);if(snow)break;}
   if(!snow||!snow.seed)throw new InvalidOperationException("Saved snow field/seed missing.");
   double before=0;foreach(float d in snow.seed.grid.depth)before+=d;
   before*=snow.seed.grid.depth.Length==65536?1:SnowVolumeGrid.Area;
   var target=ScriptableObject.CreateInstance<SnowSeed>();
   string path="Assets/Snowglow/Generated/Snow50cm_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff")+".asset";AssetDatabase.CreateAsset(target,path);
   snow.RebuildResolution(target);
   if(Math.Abs(target.grid.Total()-before)>.01)throw new InvalidOperationException("Snow volume changed during resolution migration.");
   SnowMeshAssets.SaveChunks(snow,target);
   EditorUtility.SetDirty(target);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();
   if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Could not save the higher-resolution snow scene.");
   AssetDatabase.SaveAssets();SceneValidation.Run();
   if(File.Exists(Request))File.Delete(Request);
   Debug.Log("Snowglow: saved 50 cm snow; previous volume "+before+", new volume "+target.grid.Total()+" cubic metres. Vehicles/scenery preserved.");
  }
 }
}
