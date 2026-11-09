using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Snowglow.Editor {
 // Read-only artwork staging. Never saves the scene, profile, materials or assets.
 public static class UIProductionPreviewCapture {
  const string Folder="Logs/UIProductionPreview",Request="Logs/UIProductionPreview.request";
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;try{File.Delete(Request);}catch(IOException){return;}EditorApplication.update-=Pending;try{Run();}catch(Exception e){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/Capture.txt","FAIL: "+e);Debug.LogException(e);}}
  static void Render(Camera camera,string name){var rt=new RenderTexture(1280,720,24);var old=RenderTexture.active;var target=camera.targetTexture;var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(Folder+"/"+name+".png",texture.EncodeToPNG());}finally{camera.targetTexture=target;RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}}
  [MenuItem("Snowglow/Preview/Capture production UI artwork only")]
  public static void Run(){
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=SceneValidation.ScenePath||EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Open Pinefall with Play stopped");Directory.CreateDirectory(Folder);string hash=Hash128.Compute(File.ReadAllBytes(scene.path)).ToString();bool dirty=scene.isDirty;var game=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<PrototypeSession>(true)).Single();var oldScene=scene;var cameraObject=new GameObject("Temporary menu capture camera");var camera=cameraObject.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.Skybox;camera.cullingMask=~(1<<5);camera.fieldOfView=48;camera.nearClipPlane=.1f;camera.farClipPlane=4000;GameObject staged=null;
   try{
    cameraObject.transform.position=new Vector3(66,43,-93);cameraObject.transform.LookAt(new Vector3(0,8,5));Render(camera,"pinefall");
    staged=UnityEngine.Object.Instantiate(game.Fleet.First(m=>!m.definition.loader).gameObject);staged.name="Temporary art tractor";staged.transform.position=new Vector3(0,5.25f,-20);staged.transform.rotation=Quaternion.Euler(0,0,0);staged.GetComponent<Rigidbody>().isKinematic=true;cameraObject.transform.position=staged.transform.position+new Vector3(6,3.4f,8);cameraObject.transform.LookAt(staged.transform.position+new Vector3(0,1.1f,1));Render(camera,"quick-job");UnityEngine.Object.DestroyImmediate(staged);staged=null;
    var garage=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<CatalogPlacement>(true)).FirstOrDefault(p=>p.assetId.IndexOf("garage",StringComparison.OrdinalIgnoreCase)>=0);
    if(garage){var bounds=new Bounds(garage.transform.position,Vector3.zero);foreach(var r in garage.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);cameraObject.transform.position=bounds.center+garage.transform.TransformDirection(new Vector3(19,6,25));cameraObject.transform.LookAt(bounds.center+Vector3.down*1.5f);Render(camera,"garage-card");}
   }finally{if(staged)UnityEngine.Object.DestroyImmediate(staged);UnityEngine.Object.DestroyImmediate(cameraObject);}
   Workshop(game);
   if(Hash128.Compute(File.ReadAllBytes(scene.path)).ToString()!=hash)throw new InvalidOperationException("Saved scene changed");if(scene.isDirty!=dirty&&!dirty){var clean=typeof(EditorSceneManager).GetMethod("ClearSceneDirtiness",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic)??typeof(EditorSceneManager).GetMethod("MarkSceneClean",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);if(clean!=null)clean.Invoke(null,new object[]{scene});}
   File.WriteAllText(Folder+"/Capture.txt","PASS: Native Pinefall/tractor/garage captures and temporary workshop stage with actual game machinery. No scene/profile/assets saved. Saved scene hash retained; temporary objects cleaned up. Workshop is a proposed in-engine art stage, not a new saved gameplay building. Preview only.");
  }
  static void Workshop(PrototypeSession game){
   var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("Temporary workshop stage");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);var materials=new System.Collections.Generic.List<Material>();
   Material Mat(string name,Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Simple Lit")){name=name};m.SetColor("_BaseColor",color);materials.Add(m);return m;}
   var navy=Mat("Workshop blue",new Color(.065f,.13f,.19f));var steel=Mat("Workshop steel",new Color(.28f,.36f,.4f));var yellow=Mat("Workshop yellow",new Color(1,.65f,.035f));var floor=Mat("Workshop concrete",new Color(.32f,.37f,.4f));var lightMat=Mat("Workshop lights",new Color(.9f,.95f,1));
   void Box(string name,Vector3 pos,Vector3 size,Material mat){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(root.transform,false);o.transform.localPosition=pos;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());}
   Box("Concrete floor",new Vector3(0,-.15f,0),new Vector3(20,.3f,19),floor);Box("Rear wall",new Vector3(0,3,7),new Vector3(20,6,.25f),navy);Box("Side wall",new Vector3(-9,3,0),new Vector3(.25f,6,14),navy);
   for(int i=-8;i<=8;i+=4){Box("Steel upright",new Vector3(i,3,6.75f),new Vector3(.24f,6,.3f),steel);Box("Safety yellow base",new Vector3(i,.6f,6.5f),new Vector3(.35f,1.2f,.35f),yellow);}
   Box("Yellow service beam",new Vector3(0,5.3f,6.5f),new Vector3(19,.25f,.4f),yellow);Box("Roller shutter",new Vector3(-2,2.1f,6.7f),new Vector3(5,4.2f,.15f),steel);
   for(int i=0;i<13;i++)Box("Shutter seam",new Vector3(-2,.25f+i*.3f,6.58f),new Vector3(5,.025f,.035f),navy);
   Box("Parts cabinet",new Vector3(5.8f,1,5.8f),new Vector3(2.7f,2,1.2f),yellow);for(int i=0;i<5;i++)Box("Drawer handle",new Vector3(5.8f,.35f+i*.35f,5.15f),new Vector3(1.8f,.07f,.08f),steel);
   Box("Tool board",new Vector3(5.8f,3.1f,6.5f),new Vector3(3.2f,1.8f,.14f),steel);for(int i=0;i<6;i++)Box("Hanging tool",new Vector3(4.6f+i*.48f,3.1f,6.35f),new Vector3(.06f,.9f,.06f),lightMat);
   for(int i=-1;i<=1;i++)Box("Overhead light",new Vector3(i*5,5.8f,1),new Vector3(3,.06f,.25f),lightMat);
   var cameraObj=new GameObject("Workshop capture camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObj,scene);var camera=cameraObj.AddComponent<Camera>();camera.scene=scene;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.07f,.12f,.16f);camera.fieldOfView=42;camera.nearClipPlane=.1f;camera.farClipPlane=100;cameraObj.transform.position=new Vector3(8,3.4f,-10);cameraObj.transform.LookAt(new Vector3(0,1.6f,1));
   var sun=new GameObject("Workshop stage light");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sun,scene);var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=.9f;light.transform.rotation=Quaternion.Euler(40,-30,0);
   try{foreach(bool loader in new[]{false,true}){var source=game.Fleet.First(m=>m.definition.loader==loader);var machine=UnityEngine.Object.Instantiate(source.gameObject);machine.transform.SetParent(root.transform,false);machine.transform.position=new Vector3(0,.1f,0);machine.transform.rotation=Quaternion.Euler(0,170,0);var body=machine.GetComponent<MachineController>();body.Initialize();body.Tool.ResetLowered();typeof(AttachmentController).GetMethod("Update",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(body.Tool,null);Render(camera,loader?"workshop-loader":"workshop-tractor");UnityEngine.Object.DestroyImmediate(machine);}}finally{EditorSceneManager.ClosePreviewScene(scene);foreach(var m in materials)UnityEngine.Object.DestroyImmediate(m);}
  }
 }
}
