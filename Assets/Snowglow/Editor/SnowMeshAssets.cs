using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Snowglow.Editor {
 public static class SnowMeshAssets {
  const string Source="Assets/Snowglow/Generated/Snow50cm_20261026_085523_745.asset";
  const string Request="Logs/SplitSnowAssets.request";
  public const long FileLimit=90000000; // Below both decimal 100 MB and GitHub's 100 MiB.
  const int ChunksPerPart=8;

  public static List<string> SaveChunks(SnowField snow,SnowSeed seed,bool checkSeed=true){
   string seedPath=AssetDatabase.GetAssetPath(seed);
   if(string.IsNullOrEmpty(seedPath))throw new InvalidOperationException("Save the snow seed before its mesh parts.");
   string parent=Path.GetDirectoryName(seedPath).Replace('\\','/');
   string folder=AssetDatabase.GenerateUniqueAssetPath(parent+"/"+Path.GetFileNameWithoutExtension(seedPath)+"_Meshes");
   AssetDatabase.CreateFolder(parent,Path.GetFileName(folder));
   var files=new List<string>();
   for(int start=0;start<snow.transform.childCount;start+=ChunksPerPart){
    string path=folder+"/Part_"+(start/ChunksPerPart).ToString("D3")+".asset";bool created=false;
    for(int n=start;n<Math.Min(start+ChunksPerPart,snow.transform.childCount);n++){
     var child=snow.transform.GetChild(n);var mf=child.GetComponent<MeshFilter>();var mc=child.GetComponent<MeshCollider>();
     foreach(var mesh in new[]{mf?mf.sharedMesh:null,mc?mc.sharedMesh:null}){
      if(!mesh)continue;
      if(AssetDatabase.Contains(mesh))throw new InvalidOperationException("Mesh already belongs to an asset: "+mesh.name);
      if(!created){AssetDatabase.CreateAsset(mesh,path);created=true;files.Add(path);}else AssetDatabase.AddObjectToAsset(mesh,path);
     }
    }
   }
   EditorUtility.SetDirty(seed);AssetDatabase.SaveAssets();
   if(checkSeed)AssertSmall(seedPath);foreach(string path in files)AssertSmall(path);
   return files;
  }
  static void AssertSmall(string path){if(new FileInfo(path).Length>=FileLimit)throw new InvalidOperationException("Snow asset exceeds the 90 MB safety budget: "+path);}

  [InitializeOnLoadMethod]static void OnReload(){EditorApplication.update+=Pending;}
  static void Pending(){
   if(Application.isBatchMode||!File.Exists(Request)){EditorApplication.update-=Pending;return;}
   if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;
   EditorApplication.update-=Pending;
   if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty){Debug.Log("Snowglow: save the scene, then choose Snowglow > Split oversized snow asset.");return;}
   SplitExisting();
  }
  [MenuItem("Snowglow/Split oversized snow asset")]
  public static void SplitExisting(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before splitting authored assets.");
   if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   var scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath,OpenSceneMode.Single);
   SnowField snow=null;foreach(var root in scene.GetRootGameObjects()){snow=root.GetComponentInChildren<SnowField>(true);if(snow)break;}
   if(!snow||!snow.seed||AssetDatabase.GetAssetPath(snow.seed)!=Source)throw new InvalidOperationException("Scene does not reference the expected oversized snow seed.");
   if(new FileInfo(Source).Length<FileLimit){SceneValidation.Run();if(File.Exists(Request))File.Delete(Request);return;}
   float volume=snow.seed.grid.Total();long originalBytes=new FileInfo(Source).Length;
   var oldMeshes=new List<Mesh>();foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(Source))if(asset is Mesh mesh)oldMeshes.Add(mesh);
   // Clone only serialized mesh data; preserve all scene transforms and volume data.
   foreach(Transform child in snow.transform){
    var mf=child.GetComponent<MeshFilter>();if(mf&&mf.sharedMesh)mf.sharedMesh=UnityEngine.Object.Instantiate(mf.sharedMesh);
    var mc=child.GetComponent<MeshCollider>();if(mc&&mc.sharedMesh)mc.sharedMesh=UnityEngine.Object.Instantiate(mc.sharedMesh);
   }
   // Save parts first, then the scene's new GUID/fileID references. Remove the old
   // embedded meshes only after the scene no longer points to them.
   var files=SaveChunksForSplit(snow);
   EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save split-mesh scene references.");
   foreach(var mesh in oldMeshes)UnityEngine.Object.DestroyImmediate(mesh,true);
   EditorUtility.SetDirty(snow.seed);AssetDatabase.SaveAssets();AssertSmall(Source);
   SceneValidation.Run();
   var reloaded=AssetDatabase.LoadAssetAtPath<SnowSeed>(Source);
   if(Math.Abs(reloaded.grid.Total()-volume)>.001f)throw new InvalidOperationException("Snow volume changed during asset split.");
   Directory.CreateDirectory("Logs");
   using(var report=new StreamWriter("Logs/SnowAssetSplit.txt")){
    report.WriteLine("PASS: split native Unity snow assets; scene reopened and validated.");report.WriteLine("Original bytes: "+originalBytes);report.WriteLine(Source+": "+new FileInfo(Source).Length+" bytes");
    foreach(string file in files)report.WriteLine(file+": "+new FileInfo(file).Length+" bytes");report.WriteLine("Snow volume preserved: "+volume+" cubic metres.");
   }
   if(File.Exists(Request))File.Delete(Request);
   Debug.Log("Snowglow: snow seed and "+files.Count+" mesh parts saved below 90 MB each. Scene references and volume validated.");
  }
  // The existing seed is still large until its embedded meshes have been removed.
  static List<string> SaveChunksForSplit(SnowField snow){return SaveChunks(snow,snow.seed,false);}
 }
}
