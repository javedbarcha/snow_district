using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Snowglow.Editor {
 public static class ForestCardsSceneReview {
  const string Request="Logs/ForestCardsSceneReview.request",Logs="Logs/ForestAlphaCards";
  static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
  [InitializeOnLoadMethod]static void Register(){EditorApplication.update+=Pending;}
  static void Pending(){if(!File.Exists(Request)){EditorApplication.update-=Pending;return;}if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;EditorApplication.update-=Pending;File.Delete(Request);try{Run();}catch(Exception e){Directory.CreateDirectory(Logs);File.WriteAllText(Logs+"/SceneReview.txt","FAIL: "+e);Debug.LogException(e);}}
  static Color32[] Capture(Camera camera,string file){var rt=new RenderTexture(1280,720,24);var previous=RenderTexture.active;Texture2D image=null;try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Logs+"/"+file,image.EncodeToPNG());return image.GetPixels32();}finally{camera.targetTexture=null;RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);if(image)UnityEngine.Object.DestroyImmediate(image);}}
  [MenuItem("Snowglow/Environment/Review distant forest cards without saving")]
  public static void Run(){
   var scene=SceneManager.GetActiveScene();if(scene.path!=SceneValidation.ScenePath||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Open Pinefall with Play stopped");Directory.CreateDirectory(Logs);
   string scenePath=scene.path;var hash=Hash128.Compute(File.ReadAllText(scenePath));bool dirty=scene.isDirty,async=ShaderUtil.allowAsyncCompilation;
   var forest=All<DistanceVisibility>(scene).Where(v=>v.distantParts.Length>0).ToArray();var states=forest.SelectMany(v=>v.nearParts.Concat(v.distantParts)).Distinct().ToDictionary(r=>r,r=>r.enabled);GameObject cameraObject=null;
   try{
    ShaderUtil.allowAsyncCompilation=false;var source=All<CameraRig>(scene).Single().view;cameraObject=new GameObject("Disposable forest gameplay review"){hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.CopyFrom(source);camera.scene=scene;camera.enabled=false;camera.layerCullDistances=source.layerCullDistances;camera.orthographic=false;camera.fieldOfView=60;camera.aspect=16f/9;cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=source.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing;
    int strongest=0,cardPopulation=0;
    for(int heading=0;heading<4;heading++){
     var direction=Quaternion.Euler(0,heading*90,0)*Vector3.forward;var position=-direction*80+Vector3.up*45;camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(direction+Vector3.down*.06f));int cards=0;
     foreach(var v in forest){float distance=Vector3.Distance(v.transform.position,position);bool near=distance<v.distance;foreach(var r in v.nearParts)r.enabled=near;foreach(var r in v.distantParts)r.enabled=!near&&distance<v.distantDistance;if(!near&&distance<v.distantDistance)cards++;}
     var visible=Capture(camera,"Forest-"+heading+"-cards.png");foreach(var v in forest)foreach(var r in v.distantParts)r.enabled=false;var hidden=Capture(camera,"Forest-"+heading+"-without-cards.png");int changed=0;for(int i=0;i<visible.Length;i++)if(Math.Abs(visible[i].r-hidden[i].r)+Math.Abs(visible[i].g-hidden[i].g)+Math.Abs(visible[i].b-hidden[i].b)>12)changed++;strongest=Mathf.Max(strongest,changed);cardPopulation=Mathf.Max(cardPopulation,cards);
    }
    if(strongest<30||cardPopulation==0)throw new InvalidOperationException("No visible distant-card contribution in gameplay view: "+strongest);
    File.WriteAllText(Logs+"/SceneReview.txt","PASS: Four full-scene gameplay-camera headings, existing fog/post-processing and layer distances. Strongest card-on/off contrast: "+strongest+" pixels; up to "+cardPopulation+" card roots selected by camera distance. Original scene bytes, renderer states and dirty state preserved; no Play entry or profile writes. Static views do not establish live traversal or device performance.");
   }finally{foreach(var state in states)state.Key.enabled=state.Value;if(cameraObject)UnityEngine.Object.DestroyImmediate(cameraObject);ShaderUtil.allowAsyncCompilation=async;if(!dirty&&scene.isDirty)EditorSceneManager.OpenScene(scenePath);if(hash!=Hash128.Compute(File.ReadAllText(scenePath)))throw new InvalidOperationException("Saved scene changed during read-only review");}
  }
 }
}
