using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Snowglow.Editor {
 public static class CloudFogAuthoring {
  const string Request="Logs/CloudFog.request",Logs="Logs/CloudFog";
  const string MaterialPath="Assets/Snowglow/Catalog/Sources/AngularClouds/CloudPalette.mat";
  static T[] All<T>(Scene scene)where T:Component=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
  [InitializeOnLoadMethod]static void Register(){EditorApplication.update+=Pending;}
  static void Pending(){
   if(!File.Exists(Request)){EditorApplication.update-=Pending;return;}
   if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
   EditorApplication.update-=Pending;File.Delete(Request);
   try{Install();}catch(Exception e){Directory.CreateDirectory(Logs);File.WriteAllText(Logs+"/Validation.txt","FAIL: "+e);Debug.LogException(e);}
  }
  static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("Cloud fog: "+message);}
  public static Shader ShaderAsset(){var shader=Shader.Find("Snowglow/Cloud Sky Unlit");Check(shader&&shader.isSupported&&!ShaderUtil.ShaderHasError(shader),"cloud shader unavailable or failed compilation");return shader;}
  static Color32[] GameFrame(Scene scene,Camera source,bool clouds,string filename){
   GameObject go=null;RenderTexture rt=null;Texture2D image=null;var previous=RenderTexture.active;
   try{
    var target=All<CloudWindController>(scene).Single().slots.Where(s=>s.initialCloud.gameObject.activeSelf).Select(s=>s.initialCloud.GetComponentInChildren<Renderer>()).Where(r=>Vector2.Distance(new Vector2(r.bounds.center.x,r.bounds.center.z),new Vector2(source.transform.position.x,source.transform.position.z))>600).OrderByDescending(r=>r.bounds.size.magnitude/Vector3.Distance(r.bounds.center,source.transform.position)).First();
    go=new GameObject("Disposable gameplay cloud verification"){hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(go,scene);var camera=go.AddComponent<Camera>();camera.CopyFrom(source);camera.scene=scene;camera.enabled=false;camera.layerCullDistances=source.layerCullDistances;camera.orthographic=false;camera.fieldOfView=60;camera.aspect=16f/9;camera.cullingMask=clouds?source.cullingMask:source.cullingMask&~(1<<10);camera.transform.SetPositionAndRotation(source.transform.position,Quaternion.LookRotation(target.bounds.center-source.transform.position));var data=go.AddComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=source.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing;
    rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Logs+"/"+filename,image.EncodeToPNG());camera.targetTexture=null;return image.GetPixels32();
   }finally{RenderTexture.active=previous;if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(image)UnityEngine.Object.DestroyImmediate(image);if(go)UnityEngine.Object.DestroyImmediate(go);}
  }
  static int Probe(Camera source,GameObject prefab,float distance,string file){
   var scene=SceneManager.GetActiveScene();var previous=RenderTexture.active;RenderTexture rt=null;Texture2D image=null;GameObject go=null,cloud=null;var origin=new Vector3(10000,10000,10000);
   try{
    go=new GameObject("Disposable fog contrast camera"){hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(go,scene);var camera=go.AddComponent<Camera>();camera.CopyFrom(source);camera.scene=scene;camera.layerCullDistances=source.layerCullDistances;camera.enabled=false;camera.orthographic=false;camera.fieldOfView=20;camera.aspect=1;camera.cullingMask=1<<10;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=RenderSettings.fogColor;go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
    cloud=UnityEngine.Object.Instantiate(prefab);cloud.hideFlags=HideFlags.HideAndDontSave;SceneManager.MoveGameObjectToScene(cloud,scene);cloud.transform.localScale=Vector3.one*20;var bounds=cloud.GetComponentInChildren<Renderer>().bounds;cloud.transform.position+=origin+Vector3.forward*distance-bounds.center;camera.transform.SetPositionAndRotation(origin,Quaternion.identity);
    rt=new RenderTexture(512,512,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image=new Texture2D(512,512,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,512,512),0,0);image.Apply();File.WriteAllBytes(Logs+"/"+file,image.EncodeToPNG());var pixels=image.GetPixels32();var bg=pixels[0];int contrast=pixels.Count(p=>Math.Abs(p.r-bg.r)+Math.Abs(p.g-bg.g)+Math.Abs(p.b-bg.b)>24);camera.targetTexture=null;return contrast;
   }finally{RenderTexture.active=previous;if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(image)UnityEngine.Object.DestroyImmediate(image);if(cloud)UnityEngine.Object.DestroyImmediate(cloud);if(go)UnityEngine.Object.DestroyImmediate(go);}
  }
  [MenuItem("Snowglow/Environment/Keep cloud palette visible through world fog")]
  public static void Install(){
   Check(!EditorApplication.isPlayingOrWillChangePlaymode,"stop Play first");var scene=SceneManager.GetActiveScene();Check(scene.path==SceneValidation.ScenePath,"open Pinefall");Directory.CreateDirectory(Logs);
   var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);File.Copy(MaterialPath,Logs+"/CloudPalette-before.mat",true);
   var camera=All<CameraRig>(scene).Single().view;var prefab=CatalogPreparation.Library().Find("angular-cloud-01").prefab;
   bool fog=RenderSettings.fog;var color=RenderSettings.fogColor;float start=RenderSettings.fogStartDistance,end=RenderSettings.fogEndDistance;var mode=RenderSettings.fogMode;
   var transforms=All<CloudWindController>(scene).Single().slots.Select(s=>s.initialCloud.localToWorldMatrix.ToString("R")+s.initialCloud.gameObject.activeSelf).ToArray();
   // Compare the previous URP shader against the same fog-coloured sky, not black.
   material.shader=Shader.Find("Universal Render Pipeline/Unlit");int before=Probe(camera,prefab,1200,"Before-fog-sky.png");
   material.shader=ShaderAsset();EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
   int after=Probe(camera,prefab,1200,"After-fog-sky.png");Check(after>100&&after>before+100,"cloud has no visible contrast against fog sky");
   int far=Probe(camera,prefab,1500,"After-1500m.png");Check(far>100,"far cloud disappeared");
   var visible=GameFrame(scene,camera,true,"Gameplay-clouds.png");var hidden=GameFrame(scene,camera,false,"Gameplay-without-clouds.png");int gameplayContrast=0;for(int i=0;i<visible.Length;i++)if(Math.Abs(visible[i].r-hidden[i].r)+Math.Abs(visible[i].g-hidden[i].g)+Math.Abs(visible[i].b-hidden[i].b)>24)gameplayContrast++;Check(gameplayContrast>100,"cloud not visible through gameplay rendering and world fog");
   Check(RenderSettings.fog==fog&&RenderSettings.fogColor==color&&RenderSettings.fogStartDistance==start&&RenderSettings.fogEndDistance==end&&RenderSettings.fogMode==mode,"world fog changed");
   Check(transforms.SequenceEqual(All<CloudWindController>(scene).Single().slots.Select(s=>s.initialCloud.localToWorldMatrix.ToString("R")+s.initialCloud.gameObject.activeSelf)),"cloud distribution changed");
   SceneValidation.Validate(scene);Check(AssetDatabase.LoadAssetAtPath<Material>(MaterialPath).shader==ShaderAsset(),"shader not retained");
   File.WriteAllText(Logs+"/Validation.txt","PASS: Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+". Original URP cloud shader against fog sky: "+before+" contrast pixels; cloud-only fog-free shader: "+after+" at 1200m, "+far+" at 1500m. Native palette contrast renders use gameplay camera limits and fog-coloured sky. Shared material GUID/palette texture retained; world fog, cloud transforms/active states, population and wind unchanged. Scene validation passed. Shader supported without compile errors. No Play entry or player-save writes; live game/phone acceptance remains separate.");
  }
 }
}
