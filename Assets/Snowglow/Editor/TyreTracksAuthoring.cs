using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace Snowglow.Editor {
 public static class TyreTracksAuthoring {
  const string Request="Logs/TyreTracks.request",Folder="Assets/Snowglow/Catalog/SceneData/";
  static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException("Tyre tracks: "+message);}
  static T[] All<T>(Scene s) where T:Component=>s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){
   if(!File.Exists(Request)){EditorApplication.update-=Pending;return;}
   if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
   EditorApplication.update-=Pending;File.Delete(Request);
   try{Install();}catch(Exception e){Directory.CreateDirectory("Logs/TyreTracks");File.WriteAllText("Logs/TyreTracks/Validation.txt","FAIL: "+e);Debug.LogException(e);}
  }
  public static void Place(PrototypeSession session){
   var scene=session.gameObject.scene;var tracks=All<SnowTyreTracks>(scene).SingleOrDefault();
   if(!tracks)tracks=new GameObject("Snow tyre impressions | cosmetic").AddComponent<SnowTyreTracks>();
   tracks.snow=session.Snow;tracks.machines=session.Fleet;tracks.wheelWidths=new float[session.Fleet.Length*4];tracks.capacity=2048;
   for(int m=0;m<session.Fleet.Length;m++)for(int w=0;w<4;w++){
    var machine=session.Fleet[m];var wheel=machine.wheels[w];var bounds=new Bounds();bool found=false;
    foreach(var filter in machine.wheelVisuals[w].GetComponentsInChildren<MeshFilter>()){
     if(!filter.sharedMesh)continue;var b=filter.sharedMesh.bounds;
     for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){
      var p=wheel.transform.InverseTransformPoint(filter.transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));
      if(!found){bounds=new Bounds(p,Vector3.zero);found=true;}else bounds.Encapsulate(p);
     }
    }
    tracks.wheelWidths[m*4+w]=Mathf.Clamp(found?bounds.size.x:wheel.radius*.55f,.18f,.85f);
   }
   string path=Folder+"TyreImpressions.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!material){var shader=Shader.Find("Snowglow/Snow tyre impressions");Require(shader,"shader not imported");material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
   var renderer=tracks.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
   EditorUtility.SetDirty(tracks);
  }
  public static void Batch(){EditorSceneManager.OpenScene(SceneValidation.ScenePath);Install();}
  [MenuItem("Snowglow/Install snow tyre impressions and validate")]
  public static void Install(){
   Require(!EditorApplication.isPlayingOrWillChangePlaymode,"stop Play first");var scene=SceneManager.GetActiveScene();
   Require(scene.path==SceneValidation.ScenePath&&!scene.isDirty,"open saved Pinefall first");
   Directory.CreateDirectory("Logs/TyreTracks");if(!File.Exists("Logs/TyreTracks/Pinefall-before.unity"))File.Copy(scene.path,"Logs/TyreTracks/Pinefall-before.unity");
   var session=All<PrototypeSession>(scene).Single();string seed=AssetDatabase.GetAssetPath(session.Snow.seed),hash=AssetDatabase.GetAssetDependencyHash(seed).ToString();
   var poses=All<CatalogPlacement>(scene).Where(p=>!string.IsNullOrEmpty(p.placementId)).ToDictionary(p=>p.placementId,p=>p.assetId+"|"+p.transform.localToWorldMatrix.ToString("R"));
   Place(session);SceneValidation.Validate(scene);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);Require(EditorSceneManager.SaveScene(scene),"scene save failed");
   scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);SceneValidation.Validate(scene);
   var authored=All<SnowTyreTracks>(scene).Single();Require(authored.snow&&authored.machines.Length==2&&authored.wheelWidths.Length==8,"saved bindings missing");
   foreach(var p in All<CatalogPlacement>(scene).Where(p=>!string.IsNullOrEmpty(p.placementId)))Require(poses[p.placementId]==p.assetId+"|"+p.transform.localToWorldMatrix.ToString("R"),"placement changed");
   Require(hash==AssetDatabase.GetAssetDependencyHash(seed).ToString(),"seed changed");
   Checks();MechanicsChecks.Run();Preview(scene,authored);
   EditorSceneManager.OpenScene(SceneValidation.ScenePath);
   File.WriteAllText("Logs/TyreTracks/Validation.txt","PASS: Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+". Saved/reopened one authored tyre-track renderer bound to both machines/eight grounded wheels. 11 cosmetic track checks and all 19 snow-ledger checks passed. Native daytime track preview rendered. Scene placements and snow seed hash unchanged. 2048-stamp ring, 4096 quads / 8192 triangles maximum, mesh refresh <=10Hz, 100s active-play fade. No volume, grip, work-credit or save-schema changes. Real continuous driving, contact/turn appearance and Android/A07 performance remain untested.");
   Debug.Log(File.ReadAllText("Logs/TyreTracks/Validation.txt"));
  }
  static void Checks(){
   var root=new GameObject("Disposable tyre-track acceptance fixture");SnowTyreTracks tracks=null;
   try{
    var snow=root.AddComponent<SnowField>();for(int i=0;i<snow.Grid.depth.Length;i++)snow.Grid.depth[i]=.3f;
    var child=new GameObject("Fixture impressions");child.transform.SetParent(root.transform);tracks=child.AddComponent<SnowTyreTracks>();tracks.snow=snow;tracks.capacity=128;
    tracks.machines=new MachineController[0];tracks.Clear();
    string ledger=JsonUtility.ToJson(snow.Grid);
    tracks.RecordContact(0,new Vector3(0,5,0),Vector3.forward,.4f);tracks.RecordContact(0,new Vector3(0,5,0),Vector3.forward,.4f);tracks.RefreshMesh();Require(tracks.VisibleStamps==0,"stationary tyre leaves no repeated marks");
    tracks.RecordContact(0,new Vector3(0,5,2),Vector3.forward,.4f);tracks.RefreshMesh();Require(tracks.VisibleStamps>=7,"moving tyre creates spaced prints");
    Require(ledger==JsonUtility.ToJson(snow.Grid),"tracks changed depth/credit/accounting");
    int before=tracks.VisibleStamps;tracks.RecordContact(0,new Vector3(0,5,20),Vector3.forward,.4f);tracks.RefreshMesh();Require(before==tracks.VisibleStamps,"teleport drew marks");
    tracks.BreakContact(0);tracks.RecordContact(0,new Vector3(0,5,21),Vector3.forward,.4f);tracks.RefreshMesh();Require(before==tracks.VisibleStamps,"airborne gap bridged");
    tracks.RecordContact(0,new Vector3(0,5,19),Vector3.forward,.4f);tracks.RefreshMesh();Require(tracks.VisibleStamps>before,"reverse travel missing");
    tracks.RecordContact(1,new Vector3(5,5,5),Vector3.right,.6f);tracks.RecordContact(1,new Vector3(7,5,5),Vector3.right,.6f);tracks.RefreshMesh();Require(tracks.GetComponent<MeshFilter>().sharedMesh.vertexCount>0,"steered wheel marks missing");
    for(int i=0;i<200;i++)tracks.RecordContact(1,new Vector3(7,5,5+i*.25f),Vector3.forward,.6f);
    tracks.RefreshMesh();Require(tracks.VisibleStamps<=128,"ring capacity exceeded");
    Array.Clear(snow.Grid.depth,0,snow.Grid.depth.Length);tracks.RefreshMesh();Require(tracks.VisibleStamps==0,"cleared snow retained floating marks");
    for(int i=0;i<snow.Grid.depth.Length;i++){snow.Grid.depth[i]=.3f;snow.Grid.blocked[i]=true;}
    tracks.RecordContact(2,new Vector3(1,5,1),Vector3.forward,.4f);tracks.RecordContact(2,new Vector3(1,5,3),Vector3.forward,.4f);tracks.RefreshMesh();Require(tracks.VisibleStamps==0,"blocked cells marked");
    Array.Clear(snow.Grid.blocked,0,snow.Grid.blocked.Length);tracks.RecordContact(2,new Vector3(1,5,1),Vector3.forward,.4f);tracks.RecordContact(2,new Vector3(1,5,3),Vector3.forward,.4f);tracks.AdvanceForChecks(101);Require(tracks.VisibleStamps==0,"expired marks persist");
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  static void Preview(Scene scene,SnowTyreTracks authored){
   var session=All<PrototypeSession>(scene).Single();var view=session.CameraRig.GetComponent<Camera>();
   var oldGrid=authored.snow.Grid;var p=view.transform.position;var r=view.transform.rotation;bool ortho=view.orthographic;float fov=view.fieldOfView;
   var rt=new RenderTexture(960,720,24);var oldTarget=view.targetTexture;var active=RenderTexture.active;Texture2D image=null;
   try{
    authored.snow.Grid=authored.snow.seed.grid;
    for(int wheel=0;wheel<4;wheel++){
     float x=-1+(wheel%2)*2;
     for(int i=0;i<100;i++){float z=-20+i*.1f;authored.RecordContact(wheel,new Vector3(x+Mathf.Sin(i*.025f)*1.5f,5,z),new Vector3(Mathf.Cos(i*.025f)*.375f,0,1),authored.wheelWidths[wheel]);}
    }
    authored.RefreshMesh();view.orthographic=false;view.fieldOfView=48;
    view.transform.position=new Vector3(8,15,-24);view.transform.LookAt(new Vector3(0,5,-15));view.targetTexture=rt;view.Render();
    RenderTexture.active=rt;image=new Texture2D(960,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,960,720),0,0);image.Apply();File.WriteAllBytes("Logs/TyreTracks/Preview.png",image.EncodeToPNG());
   }finally{authored.Clear();authored.snow.Grid=oldGrid;view.targetTexture=oldTarget;RenderTexture.active=active;view.transform.SetPositionAndRotation(p,r);view.orthographic=ortho;view.fieldOfView=fov;rt.Release();UnityEngine.Object.DestroyImmediate(rt);if(image)UnityEngine.Object.DestroyImmediate(image);}
  }
 }
}
