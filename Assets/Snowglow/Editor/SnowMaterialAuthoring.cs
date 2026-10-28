using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Snowglow.Editor {
 public static class SnowMaterialAuthoring {
  const string Request="Logs/SoftSnow.request";
  public const string MeshShader="Snowglow/Soft Toon Snow",TerrainShader="Universal Render Pipeline/Terrain/Lit";
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){
   if(!File.Exists(Request)){EditorApplication.update-=Pending;return;}
   if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
   EditorApplication.update-=Pending;File.Delete(Request);
   try{Apply();}catch(Exception e){File.WriteAllText("Logs/SoftSnowMaterial.txt","FAIL: "+e);Debug.LogException(e);}
  }
  public static void Configure(Material material,bool terrain){
   var shader=Shader.Find(terrain?TerrainShader:MeshShader);if(!shader)throw new InvalidOperationException("Soft snow shader not imported.");
   material.shader=shader;material.shaderKeywords=Array.Empty<string>();material.renderQueue=terrain?1900:2000;material.SetOverrideTag("RenderType","Opaque");
   var snow=Color.white;ColorUtility.TryParseHtmlString("#BFE7EE",out var shade);
   material.SetColor("_BaseColor",terrain?Color.white:snow);if(!terrain)material.SetColor("_SnowShadowColor",shade);material.enableInstancing=terrain;
   material.SetShaderPassEnabled("DepthOnly",true);material.SetShaderPassEnabled("ShadowCaster",true);
   if(terrain){material.SetFloat("_EnableInstancedPerPixelNormal",0);}
   EditorUtility.SetDirty(material);
  }
  public static void Configure(TerrainLayer layer){
   var snow=Color.white;
   layer.diffuseRemapMin=Vector4.zero;layer.diffuseRemapMax=(Vector4)snow.linear;
   layer.metallic=0;layer.smoothness=0;EditorUtility.SetDirty(layer);
  }
  [MenuItem("Snowglow/Art/Apply soft matte toon snow")]
  public static void Apply(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before authoring snow materials.");
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=SceneValidation.ScenePath)throw new InvalidOperationException("Open saved Pinefall first.");
   var session=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PrototypeSession>()).Single();var field=session.Snow;var palette=field.palette;
   string seedPath=AssetDatabase.GetAssetPath(field.seed);var seedHash=AssetDatabase.GetAssetDependencyHash(seedPath);
   string meshGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(palette.snow)),terrainGuid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(palette.terrain));
   var terrain=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Terrain>()).Single();bool instanced=terrain.drawInstanced;
   if(terrain.terrainData.terrainLayers.Length!=1)throw new InvalidOperationException("Review the snow material before applying to mixed terrain layers.");
   Configure(palette.snow,false);Configure(palette.terrain,true);Configure(terrain.terrainData.terrainLayers[0]);AssetDatabase.SaveAssets();terrain.Flush();
   foreach(var mat in new[]{palette.snow,palette.terrain})if(ShaderUtil.ShaderHasError(mat.shader)||mat.GetTag("RenderType",false)!="Opaque"||mat.GetColor("_BaseColor").a!=1)throw new InvalidOperationException("Snow shader/material validation failed: "+mat.name);
   if(field.GetComponentsInChildren<MeshRenderer>().Any(r=>r.sharedMaterial!=palette.snow)||terrain.materialTemplate!=palette.terrain||terrain.drawInstanced!=instanced)throw new InvalidOperationException("Snow bindings or terrain instancing changed.");
   if(seedHash!=AssetDatabase.GetAssetDependencyHash(seedPath)||meshGuid!=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(palette.snow))||terrainGuid!=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(palette.terrain)))throw new InvalidOperationException("Snow seed or material identity changed.");
   SceneValidation.Validate(scene);
   File.WriteAllText("Logs/SoftSnowMaterial.txt","PASS: Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+". Reference snow palette: pure white upward mesh faces and pale cyan sides. Mesh shader imported without reported errors; terrain uses supported URP Terrain/Lit with a white matte snow layer. Shared snow/terrain material GUIDs, snow seed hash, chunk bindings and terrain instancing preserved; scene validation passed. No snow geometry, physics, volume or scene layout changed. Android player shader compilation, device appearance and frame timings remain untested.");
   if(SceneView.lastActiveSceneView)SceneView.lastActiveSceneView.LookAt(new Vector3(-54,6,-65),Quaternion.Euler(32,45,0),32,true,true);
   Debug.Log("Snowglow soft matte toon snow applied; see Logs/SoftSnowMaterial.txt");
  }
 }
}
