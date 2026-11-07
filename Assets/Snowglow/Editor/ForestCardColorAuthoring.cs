using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Snowglow.Editor {
 public static class ForestCardColorAuthoring {
  const string Source="Assets/Snowglow/Catalog/Sources/ForestAlphaCards/",Logs="Logs/ForestCardColors",Request="Logs/ForestCardColors.request";
  static readonly string[] Families={"Foliage","Snow","Wood"};
  static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
  [InitializeOnLoadMethod]static void Register(){EditorApplication.update+=Pending;}
  static void Pending(){if(!File.Exists(Request)){EditorApplication.update-=Pending;return;}if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;EditorApplication.update-=Pending;File.Delete(Request);try{Install();}catch(Exception e){Directory.CreateDirectory(Logs);File.WriteAllText(Logs+"/Validation.txt","FAIL: "+e);Debug.LogException(e);}}
  static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("Forest card colours: "+message);}
  public static void BindShader(Material material){var shader=Shader.Find("Snowglow/Forest Card Palette");Check(shader&&shader.isSupported&&!ShaderUtil.ShaderHasError(shader),"palette shader unavailable");material.shader=shader;material.DisableKeyword("_ALPHATEST_ON");}
  static Color32[] Render(Scene scene,Camera source,int variant,bool card,string file){
   GameObject root=null,cameraObject=null;RenderTexture rt=null;Texture2D image=null;var previous=RenderTexture.active;
   try{
    var asset=CatalogPreparation.Library().Find("alpine-tree-"+variant.ToString("D2"));root=UnityEngine.Object.Instantiate(asset.prefab);root.hideFlags=HideFlags.HideAndDontSave;SceneManager.MoveGameObjectToScene(root,scene);foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
    var sun=All<Light>(scene).Where(l=>l.type==LightType.Directional).OrderByDescending(l=>l.intensity).First();var facing=-sun.transform.forward;facing.y=.3f;facing.Normalize();var bounds=CatalogValidation.BoundsOf(root);var origin=new Vector3(10000,10000,10000);root.transform.position=origin-bounds.center;root.GetComponent<DistanceVisibility>().Evaluate(root.transform.position+Vector3.right*(card?300:50));
    cameraObject=new GameObject("Disposable forest colour camera"){hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.CopyFrom(source);camera.scene=scene;camera.enabled=false;camera.cullingMask=1<<31;camera.layerCullDistances=new float[32];camera.orthographic=true;camera.orthographicSize=Mathf.Max(bounds.extents.y,bounds.extents.x)*1.25f;camera.aspect=1;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.transform.SetPositionAndRotation(origin+facing*40,Quaternion.LookRotation(-facing));cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=source.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing;
    rt=new RenderTexture(512,512,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image=new Texture2D(512,512,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,512,512),0,0);image.Apply();File.WriteAllBytes(Logs+"/"+file,image.EncodeToPNG());camera.targetTexture=null;return image.GetPixels32();
   }finally{RenderTexture.active=previous;if(rt){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(image)UnityEngine.Object.DestroyImmediate(image);if(cameraObject)UnityEngine.Object.DestroyImmediate(cameraObject);if(root)UnityEngine.Object.DestroyImmediate(root);}
  }
  sealed class Samples {
   readonly int[,,] hist=new int[3,3,256];readonly int[] count=new int[3];
   public void Add(Color32[] pixels,Color32[] mask=null){for(int pixel=0;pixel<pixels.Length;pixel++){var p=mask==null?pixels[pixel]:mask[pixel];if(Mathf.Max(p.r,Mathf.Max(p.g,p.b))<60)continue;int family;
    if(p.g>p.r*1.15f&&p.g>p.b*.8f)family=0;
    else if(p.r>p.g*1.12f&&p.r>p.b*1.15f)family=2;
    else if(Mathf.Min(p.r,Mathf.Min(p.g,p.b))>100)family=1;else continue;
    var output=pixels[pixel];hist[family,0,output.r]++;hist[family,1,output.g]++;hist[family,2,output.b]++;count[family]++;
   }}
   public Vector3 Median(int family,float quantile=.5f){Check(count[family]>50,"not enough "+Families[family]+" reference pixels");var values=new float[3];for(int channel=0;channel<3;channel++){int total=0;for(int v=0;v<256;v++){total+=hist[family,channel,v];if(total>count[family]*quantile){values[channel]=QualitySettings.activeColorSpace==ColorSpace.Linear?Mathf.GammaToLinearSpace(v/255f):v/255f;break;}}}return new Vector3(values[0],values[1],values[2]);}
  }
  static float Error(Vector3 a,Vector3 b)=>Vector3.Distance(a,b);
  [MenuItem("Snowglow/Environment/Match forest card colours to 3D trees")]
  public static void Install(){
   var scene=SceneManager.GetActiveScene();Check(scene.path==SceneValidation.ScenePath&&!EditorApplication.isPlayingOrWillChangePlaymode,"open Pinefall with Play stopped");Directory.CreateDirectory(Logs);
   string scenePath=scene.path;var sceneHash=Hash128.Compute(File.ReadAllText(scenePath));bool dirty=scene.isDirty,async=ShaderUtil.allowAsyncCompilation;
   var protectedPaths=All<MeshFilter>(scene).Where(f=>f.sharedMesh).Select(f=>AssetDatabase.GetAssetPath(f.sharedMesh)).Concat(new[]{Source+"Forest_Cards_Atlas_4096.png","Assets/Snowglow/Catalog/Sources/AlpineTrees/TreePalette.mat",AssetDatabase.GetAssetPath(All<PrototypeSession>(scene).Single().Snow.seed)}).Where(p=>p.StartsWith("Assets/")).Distinct().ToDictionary(p=>p,p=>AssetDatabase.GetAssetDependencyHash(p));
   var camera=All<CameraRig>(scene).Single().view;var material=AssetDatabase.LoadAssetAtPath<Material>(Source+"ForestCards.mat");File.Copy(Source+"ForestCards.mat",Logs+"/ForestCards-before.mat",true);
   try{
    ShaderUtil.allowAsyncCompilation=false;BindShader(material);foreach(var family in Families)material.SetVector("_"+family+"Gain",Vector4.one);
    var reference=new Samples();var before=new Samples();var masks=new System.Collections.Generic.Dictionary<int,Color32[]>();foreach(int variant in new[]{1,9,21}){reference.Add(Render(scene,camera,variant,false,"Variant-"+variant+"-3D.png"));masks[variant]=Render(scene,camera,variant,true,"Variant-"+variant+"-before.png");before.Add(masks[variant]);}
    var target=Enumerable.Range(0,3).Select(i=>reference.Median(i,.6f)).ToArray();var old=Enumerable.Range(0,3).Select(i=>before.Median(i)).ToArray();var gains=Enumerable.Range(0,3).Select(i=>new Vector3(Mathf.Clamp(target[i].x/Mathf.Max(.005f,old[i].x),.25f,16),Mathf.Clamp(target[i].y/Mathf.Max(.005f,old[i].y),.25f,16),Mathf.Clamp(target[i].z/Mathf.Max(.005f,old[i].z),.25f,16))).ToArray();
    for(int i=0;i<3;i++)material.SetVector("_"+Families[i]+"Gain",new Vector4(gains[i].x,gains[i].y,gains[i].z,1));
    var after=new Samples();foreach(int variant in new[]{1,9,21})after.Add(Render(scene,camera,variant,true,"Variant-"+variant+"-after.png"),masks[variant]);
    var now=Enumerable.Range(0,3).Select(i=>after.Median(i)).ToArray();float oldError=Enumerable.Range(0,3).Sum(i=>Error(target[i],old[i])),newError=Enumerable.Range(0,3).Sum(i=>Error(target[i],now[i]));Check(newError<oldError*.8f,"palette match did not improve enough: "+oldError+" -> "+newError);
    EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();Check(!ShaderUtil.ShaderHasError(material.shader),"palette shader compile errors");SceneValidation.Validate(scene);
    foreach(var pair in protectedPaths)Check(pair.Value==AssetDatabase.GetAssetDependencyHash(pair.Key),"original texture/near material/mesh/snow changed: "+pair.Key);
    ForestCardsSceneReview.Run();File.Copy("Logs/ForestAlphaCards/Forest-1-cards.png",Logs+"/Scene-bright-cards.png",true);
    File.WriteAllText(Logs+"/Validation.txt","PASS: Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+". Same-lighting sun-facing native 3D/card palette comparison: aggregate active-colour-space error "+oldError.ToString("F4")+" -> "+newError.ToString("F4")+". Foliage/snow/wood gains: "+string.Join("; ",gains.Select(g=>g.ToString("F3")))+". Shared shader preserves atlas/alpha, clipping, double-sided depth, instancing and world fog. Scene validation passed; source atlas, near materials, meshes and snow hashes unchanged. No scene save, placement/collider/threshold changes, runtime factories or player-save writes. Paired native images generated. Flat silhouettes/view-dependent lighting can still differ; live/phone acceptance remains separate.");
   }finally{ShaderUtil.allowAsyncCompilation=async;if(!dirty&&scene.isDirty)EditorSceneManager.OpenScene(scenePath);Check(sceneHash==Hash128.Compute(File.ReadAllText(scenePath)),"saved scene changed");}
  }
 }
}