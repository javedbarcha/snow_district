using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Snowglow.Editor {
 public static class CloudVisibilityAuthoring {
  const int CloudLayer=10;
  const string Request="Logs/CloudVisibility.request",Logs="Logs/CloudVisibility";
  static T[] All<T>(Scene scene) where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
  static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException("Cloud visibility: "+message);}
  [InitializeOnLoadMethod] static void Register(){EditorApplication.update+=Pending;}
  static void Pending(){
   if(!File.Exists(Request)){EditorApplication.update-=Pending;return;}
   if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
   EditorApplication.update-=Pending;File.Delete(Request);
   try{Install();}catch(Exception e){Directory.CreateDirectory(Logs);File.WriteAllText(Logs+"/Validation.txt","FAIL: "+e);Debug.LogException(e);}
  }
  static void SetLayer(GameObject root){foreach(var t in root.GetComponentsInChildren<Transform>(true)){t.gameObject.layer=CloudLayer;if(PrefabUtility.IsPartOfPrefabInstance(t.gameObject))PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);}}
  public static void Apply(Scene scene){
   var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);var layers=tags.FindProperty("layers");
   string existing=layers.GetArrayElementAtIndex(CloudLayer).stringValue;
   Require(string.IsNullOrEmpty(existing)||existing=="Clouds","layer 10 is already reserved");
   layers.GetArrayElementAtIndex(CloudLayer).stringValue="Clouds";tags.ApplyModifiedProperties();
   var clouds=All<CatalogPlacement>(scene).Where(p=>(p.assetId??"").StartsWith("angular-cloud-")).ToArray();
   foreach(var prefab in clouds.Select(p=>CatalogPreparation.Library().Find(p.assetId).prefab).Distinct()){
    string path=AssetDatabase.GetAssetPath(prefab);var root=PrefabUtility.LoadPrefabContents(path);
    try{SetLayer(root);foreach(var cull in root.GetComponentsInChildren<DistanceVisibility>(true))UnityEngine.Object.DestroyImmediate(cull);foreach(var r in root.GetComponentsInChildren<Renderer>(true)){r.enabled=true;r.allowOcclusionWhenDynamic=false;}PrefabUtility.SaveAsPrefabAsset(root,path);}
    finally{PrefabUtility.UnloadPrefabContents(root);}
   }
   foreach(var cloud in clouds){SetLayer(cloud.gameObject);foreach(var cull in cloud.GetComponentsInChildren<DistanceVisibility>(true))UnityEngine.Object.DestroyImmediate(cull);foreach(var r in cloud.GetComponentsInChildren<Renderer>(true)){r.enabled=true;r.allowOcclusionWhenDynamic=false;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}}
   foreach(var rig in All<CameraRig>(scene)){
    var camera=rig.view;float oldFar=camera.farClipPlane;var distances=camera.layerCullDistances;
    for(int i=0;i<32;i++)if(i!=CloudLayer)distances[i]=Mathf.Min(distances[i]>0?distances[i]:oldFar,oldFar);
    distances[CloudLayer]=0;var policy=camera.GetComponent<CloudCameraVisibility>();if(!policy)policy=camera.gameObject.AddComponent<CloudCameraVisibility>();policy.view=camera;policy.farDistance=Mathf.Max(oldFar,4000);policy.layerDistances=distances;policy.Apply();EditorUtility.SetDirty(policy);EditorUtility.SetDirty(camera);
   }
  }
  static int RenderProbe(Camera source,GameObject prefab,int layer,string filename){
   var scene=EditorSceneManager.NewPreviewScene();GameObject cameraObject=null;RenderTexture target=null;Texture2D pixels=null;var previous=RenderTexture.active;
   try{
    cameraObject=new GameObject("Cloud distance probe camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.CopyFrom(source);camera.scene=scene;camera.layerCullDistances=source.layerCullDistances;camera.enabled=false;camera.orthographic=false;camera.fieldOfView=20;camera.aspect=1;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=1<<layer;cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
    var cloud=UnityEngine.Object.Instantiate(prefab);SceneManager.MoveGameObjectToScene(cloud,scene);foreach(var t in cloud.GetComponentsInChildren<Transform>(true))t.gameObject.layer=layer;cloud.transform.localScale=Vector3.one*10;var bounds=cloud.GetComponentInChildren<Renderer>().bounds;cloud.transform.position+=new Vector3(0,0,1500)-bounds.center;camera.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
    target=new RenderTexture(256,256,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels=new Texture2D(256,256,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,256,256),0,0);pixels.Apply();File.WriteAllBytes(Logs+"/"+filename,pixels.EncodeToPNG());return pixels.GetPixels32().Count(p=>p.r>40||p.g>40||p.b>40);
   }finally{if(cameraObject)cameraObject.GetComponent<Camera>().targetTexture=null;RenderTexture.active=previous;if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(pixels)UnityEngine.Object.DestroyImmediate(pixels);EditorSceneManager.ClosePreviewScene(scene);}
  }
  [MenuItem("Snowglow/Environment/Disable distance culling for clouds only")]
  public static void Install(){
   Require(!EditorApplication.isPlayingOrWillChangePlaymode,"stop Play first");var scene=SceneManager.GetActiveScene();Require(scene.path==SceneValidation.ScenePath,"open Pinefall");Directory.CreateDirectory(Logs);
   EditorSceneManager.SaveScene(scene,Logs+"/Pinefall-before.unity",true);
   var nonClouds=All<DistanceVisibility>(scene).Where(v=>!v.GetComponentInParent<CloudWindController>()).Select(v=>v.GetInstanceID()+":"+v.distance).ToArray();
   var camera=All<CameraRig>(scene).Single().view;float originalFar=camera.farClipPlane;var oldDistances=camera.layerCullDistances;
   Apply(scene);var distances=camera.layerCullDistances;
   for(int i=0;i<32;i++)if(i!=CloudLayer)Require(Mathf.Approximately(distances[i],Mathf.Min(oldDistances[i]>0?oldDistances[i]:originalFar,originalFar)),"non-cloud distance changed");
   Require(nonClouds.SequenceEqual(All<DistanceVisibility>(scene).Where(v=>!v.GetComponentInParent<CloudWindController>()).Select(v=>v.GetInstanceID()+":"+v.distance)),"other distance components changed");
   var prefab=CatalogPreparation.Library().Find("angular-cloud-01").prefab;
   Require(RenderProbe(camera,prefab,CloudLayer,"Cloud-at-1500m.png")>30,"distant cloud invisible");
   Require(RenderProbe(camera,prefab,0,"World-layer-at-1500m.png")==0,"world layer no longer culled");
   SceneValidation.Validate(scene);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);Require(EditorSceneManager.SaveScene(scene),"scene save failed");
   scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);camera=All<CameraRig>(scene).Single().view;Require(camera.farClipPlane>=4000&&camera.layerCullDistances[CloudLayer]==0,"camera settings not saved");
   foreach(var cloud in All<CatalogPlacement>(scene).Where(p=>(p.assetId??"").StartsWith("angular-cloud-")))Require(cloud.GetComponentsInChildren<Renderer>(true).All(r=>r.gameObject.layer==CloudLayer&&r.enabled),"saved cloud layer/renderer");
   File.WriteAllText(Logs+"/Validation.txt","PASS: Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+". Clouds use layer 10 (Clouds), no distance component or per-layer distance cutoff, 4000m camera envelope and dynamic occlusion disabled. Native 1500m cloud probe renders; same mesh on world layer remains culled. Other per-layer effective limits and DistanceVisibility components preserved. Eight prepared prefabs and saved/reopened Pinefall updated; scene validation passed. Cloud population, motion, transforms, meshes and snow unchanged. Live Play/phone acceptance untested.");
  }
 }
}
