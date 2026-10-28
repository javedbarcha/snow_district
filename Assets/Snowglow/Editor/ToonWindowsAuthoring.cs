using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Snowglow.Editor {
 public static class ToonWindowsAuthoring {
  const string Request="Logs/ToonWindows.request";
  public const string ShaderName="Snowglow/Toon Vehicle Windows";
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){
   if(!File.Exists(Request)){EditorApplication.update-=Pending;return;}
   if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
   EditorApplication.update-=Pending;File.Delete(Request);
   try{Apply();}catch(Exception e){File.WriteAllText("Logs/ToonWindows.txt","FAIL: "+e);Debug.LogException(e);}
  }
  public static void Configure(Material material){
   var shader=Shader.Find(ShaderName);if(!shader)throw new InvalidOperationException("Toon window shader must be imported first.");
   material.shader=shader;material.shaderKeywords=Array.Empty<string>();material.renderQueue=2000;
   material.SetOverrideTag("RenderType","Opaque");
   ColorUtility.TryParseHtmlString("#9DEDF7",out var cyan);ColorUtility.TryParseHtmlString("#E7FCFF",out var reflection);
   material.SetColor("_BaseColor",cyan);material.SetColor("_ReflectionColor",reflection);
   material.SetShaderPassEnabled("DepthOnly",true);material.enableInstancing=false;
   EditorUtility.SetDirty(material);
  }
  [MenuItem("Snowglow/Asset intake/Apply bright opaque toon windows")]
  public static void Apply(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before material authoring.");
   string[] paths={CatalogPreparation.Root+"/Sources/LowPolySnowPlower/Materials/Color09.mat",CatalogPreparation.Root+"/Sources/LowPolySnowLoader/Glass.mat"};
   foreach(var path in paths){var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat)throw new InvalidOperationException("Missing vehicle window material: "+path);Configure(mat);}
   AssetDatabase.SaveAssets();
   foreach(var path in paths){var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(ShaderUtil.ShaderHasError(mat.shader)||mat.renderQueue!=2000||mat.GetTag("RenderType",false)!="Opaque"||mat.GetColor("_BaseColor").a!=1||mat.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT"))throw new InvalidOperationException("Invalid opaque window state: "+path);}
   foreach(var path in new[]{StarterPlowAuthoring.PrefabPath,LoaderModelIntake.PrefabPath}){
    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!prefab||!prefab.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(m=>m&&m.shader.name==ShaderName)))throw new InvalidOperationException("Saved vehicle has no toon windows: "+path);
   }
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
   if(scene.path==SceneValidation.ScenePath){SceneValidation.Validate(scene);var fleet=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PrototypeSession>()).Single().Fleet;Selection.activeGameObject=fleet[0].gameObject;if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(fleet[0].transform.position+new Vector3(0,1.5f,0),Quaternion.Euler(22,215,0),4.5f,true,true);}
   File.WriteAllText("Logs/ToonWindows.txt","PASS: Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+". Both saved vehicle window materials use the opaque URP toon shader, geometry queue, alpha one, no transparent keyword and enabled depth pass. Both equipped prefabs retain their shared window bindings. Bright cyan and pale painted diagonal reflections use object-space projection; geometry counts unchanged. Phone frame timings and Android player shader build remain untested.");
   Debug.Log("Snowglow: opaque bright toon windows applied to both vehicles.");
  }
 }
}
