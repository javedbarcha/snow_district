using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace Snowglow.Editor {
 public static class CloudWindAuthoring {
 const string Request="Logs/CloudWind.request",Logs="Logs/CloudWind/PopulationRadius";static int checks;
 static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();
 static bool Cloud(CatalogPlacement p)=>(p.assetId??"").StartsWith("angular-cloud-");
 static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("Cloud wind: "+message);checks++;}
 [InitializeOnLoadMethod]static void Register(){EditorApplication.update+=Pending;}
 static void Pending(){if(!File.Exists(Request)){EditorApplication.update-=Pending;return;}if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;EditorApplication.update-=Pending;File.Delete(Request);try{Install();}catch(Exception e){Directory.CreateDirectory(Logs);File.WriteAllText(Logs+"/Validation.txt","FAIL: "+e);Debug.LogException(e);}}
 public static void Apply(Scene scene){
  CloudVisibilityAuthoring.Apply(scene);
  var clouds=All<CatalogPlacement>(scene).Where(Cloud).OrderBy(p=>p.placementId).ToArray();Check(clouds.Length==40,"expected 40 saved cloud instances");var parent=clouds[0].transform.parent;Check(clouds.All(p=>p.transform.parent==parent)&&parent.lossyScale==Vector3.one,"cloud group transform");
  var wind=parent.GetComponent<CloudWindController>();if(wind&&wind.distributionVersion==2){Validate(scene);return;}if(!wind)wind=parent.gameObject.AddComponent<CloudWindController>();
  wind.centre=new Vector3(0,DistrictFactory.BaseHeight,0);if(wind.slots.Length!=40)wind.slots=clouds.Select(p=>{var a=CatalogPreparation.Library().Find(p.assetId);return new CloudWindSlot{initialCloud=p.transform,prefab=a.prefab,placementId=p.placementId,baselineScale=p.transform.localScale,sourceBounds=CatalogValidation.BoundsOf(a.prefab)};}).ToArray();wind.Initialize(711140);
  wind.distributionVersion=2;parent.name="Sky clouds | 10-15 active in 1500m radius";
  foreach(var slot in wind.slots){PrefabUtility.RecordPrefabInstancePropertyModifications(slot.initialCloud);PrefabUtility.RecordPrefabInstancePropertyModifications(slot.initialCloud.gameObject);}
  EditorUtility.SetDirty(wind);Validate(scene);
 }
 static void Validate(Scene scene){var systems=All<CloudWindController>(scene);Check(systems.Length==1,"single saved wind controller");var w=systems[0];Check(w.slots.Length==40&&w.radius==1500&&w.minPopulation==10&&w.maxPopulation==15&&w.distributionVersion==2,"saved sky volume");Check(w.minScale==.5f&&w.maxScale==3&&w.minHeight==250&&w.maxHeight==450&&w.minSpeed==3&&w.maxSpeed==8&&w.minChangeSeconds==300&&w.maxChangeSeconds==900&&w.turnSeconds==20,"saved wind tuning");
  Check(w.slots.Count(s=>s.initialCloud.gameObject.activeSelf)>=10&&w.slots.Count(s=>s.initialCloud.gameObject.activeSelf)<=15,"saved active count");Check(w.slots.Where(s=>s.initialCloud.gameObject.activeSelf).All(s=>!w.NeedsReplacement(s.initialCloud.position)),"saved disk bounds");
  Check(w.slots.Select(s=>s.placementId).Distinct().Count()==40,"unique slot identities");foreach(var slot in w.slots){Check(slot.initialCloud&&slot.prefab&&AssetDatabase.Contains(slot.prefab),"saved cloud/prefab binding");float factor=slot.initialCloud.localScale.x/slot.baselineScale.x;Check(factor>=.5f&&factor<=3&&Vector3.Distance(slot.initialCloud.localScale,slot.baselineScale*factor)<.001f,"uniform scale range");var rs=slot.initialCloud.GetComponentsInChildren<Renderer>(true);Check(rs.Length==1&&rs.All(r=>r.shadowCastingMode==ShadowCastingMode.Off&&r.sharedMaterial.renderQueue==3000),"transparent unshadowed shared cloud renderer");var b=rs[0].bounds;Check(b.min.y>=w.centre.y+w.minHeight-.01f&&b.max.y<=w.centre.y+w.maxHeight+.01f,"full cloud height limits");Check(slot.initialCloud.GetComponentsInChildren<Collider>(true).Length==0&&slot.initialCloud.GetComponentsInChildren<Rigidbody>(true).Length==0,"cloud physics added");}
 }
 static void Tests(CloudWindController source){
  var random=new System.Random(7);var m=new CloudWindMotion(random,3,8,300,900,20);Check(m.Interval>=300&&m.Interval<=900,"initial wind interval");float yaw=m.Yaw;m.Advance(m.Remaining-.1f);Check(m.Changes==0&&Mathf.Abs(Mathf.DeltaAngle(yaw,m.Yaw))<.001f,"early wind change");m.Advance(.2f);Check(m.Changes==1,"wind change scheduling");Check(Mathf.Abs(Mathf.DeltaAngle(yaw,m.Yaw))<1,"wind turned instantly");
  for(int i=0;i<1000;i++){m.Advance(m.Remaining);Check(m.Interval>=300&&m.Interval<=900&&m.Speed>=3-.001f&&m.Speed<=8+.001f&&Mathf.Abs(m.Direction.y)<.0001f&&Mathf.Abs(m.Direction.magnitude-1)<.001f,"wind range/direction");m.Advance(20);Check(Mathf.Abs(Mathf.DeltaAngle(m.Yaw,m.TargetYaw))<.01f,"turn did not settle");}
  GameObject root=null;try{
   root=new GameObject("Disposable cloud wind fixture"){hideFlags=HideFlags.HideAndDontSave};var w=root.AddComponent<CloudWindController>();w.centre=source.centre;w.slots=source.slots.Select(s=>{var go=UnityEngine.Object.Instantiate(s.prefab,root.transform);go.hideFlags=HideFlags.HideAndDontSave;go.transform.localScale=s.baselineScale;go.transform.position=source.centre+Vector3.up*300;return new CloudWindSlot{initialCloud=go.transform,prefab=s.prefab,placementId=s.placementId,baselineScale=s.baselineScale,sourceBounds=s.sourceBounds};}).ToArray();w.Initialize(19);
   for(int seed=0;seed<100;seed++){
    w.Initialize(seed);Check(w.ActiveCount>=10&&w.ActiveCount<=15,"initial population across seeds");
    Check(w.slots.Where(s=>s.current).Any(s=>new Vector2(s.current.position.x-w.centre.x,s.current.position.z-w.centre.z).magnitude<151),"central coverage across seeds");
    Check(w.slots.Where(s=>s.current).All(s=>!w.NeedsReplacement(s.current.position)),"starting disk across seeds");
   }
   w.Initialize(19);Check(w.ActiveCount>=10&&w.ActiveCount<=15,"initial population");
   Check(w.slots.Where(s=>s.current).Any(s=>new Vector2(s.current.position.x-w.centre.x,s.current.position.z-w.centre.z).magnitude<151),"central starting coverage");
   Check(w.slots.Where(s=>s.current).Any(s=>new Vector2(s.current.position.x-w.centre.x,s.current.position.z-w.centre.z).magnitude>1250),"outer starting coverage");
   foreach(var slot in w.slots)for(int i=0;i<50;i++){
    var pose=w.EntryPose(slot);var p=pose.position-w.centre;p.y=0;
    Check(Mathf.Abs(p.magnitude-1498)<.01f&&Vector3.Dot(p,w.Wind.Direction)<0,"upwind circular entry");
    Check(!w.NeedsReplacement(pose.position),"entry expires immediately");
    float scale=pose.scale.x/slot.baselineScale.x;Check(scale>=.5f&&scale<=3,"spawn size");
    Check(pose.position.y+slot.sourceBounds.min.y*pose.scale.y>=w.centre.y+250-.001f&&pose.position.y+slot.sourceBounds.max.y*pose.scale.y<=w.centre.y+450+.001f,"full height band");
   }
   var first=w.slots.First(s=>s.current);var initial=first.current.position;float speed=w.Wind.Speed;w.Simulate(1,true);
   Check(Vector3.Distance(first.current.position,initial+w.Wind.Direction*speed)<.01f,"wind motion");
   initial=first.current.position;float remaining=w.Wind.Remaining;w.Simulate(100,false);Check(first.current.position==initial&&remaining==w.Wind.Remaining,"pause gates");
   int least=15,most=10;bool delayed=false;int departures=0;
   for(int i=0;i<7200;i++){
    int before=w.ActiveCount;w.Simulate(1,true);int count=w.ActiveCount;least=Mathf.Min(least,count);most=Mathf.Max(most,count);if(count<before){delayed=true;departures++;}
    Check(count>=10&&count<=15,"population bounds over two hours");
    foreach(var slot in w.slots.Where(s=>s.current))Check(!w.NeedsReplacement(slot.current.position),"live disk bound");
    Check(root.GetComponentsInChildren<Renderer>().Length==count,"active renderer bound");
   }
   Check(least<most&&delayed&&departures>0&&w.RespawnCount>0,"population varies with delayed arrivals");
   first=w.slots.First(s=>s.current);var retired=first.current;retired.position=w.centre+Vector3.right*1501+Vector3.up*300;w.Simulate(.01f,true);Check(!retired,"radial expiry destroys root");
   Check(root.GetComponentsInChildren<Renderer>(true).Length<=40,"bounded inactive reservoir");
   Check(root.GetComponentsInChildren<Renderer>(true).Select(r=>r.sharedMaterial).Distinct().Count()==1,"shared material");

  }finally{if(root)UnityEngine.Object.DestroyImmediate(root);}
 }
 [MenuItem("Snowglow/Environment/Install wind-driven cloud lifecycle")]
 public static void Install(){
  checks=0;Directory.CreateDirectory(Logs);var scene=SceneManager.GetActiveScene();Check(scene.path==SceneValidation.ScenePath&&!EditorApplication.isPlayingOrWillChangePlaymode,"open Pinefall with Play stopped");if(!File.Exists(Logs+"/Pinefall-before.unity"))EditorSceneManager.SaveScene(scene,Logs+"/Pinefall-before.unity",true);if(scene.isDirty)EditorSceneManager.SaveScene(scene,Logs+"/Pinefall-unsaved-before.unity",true);
  var poses=All<CatalogPlacement>(scene).Where(p=>!Cloud(p)&&!string.IsNullOrEmpty(p.placementId)).ToDictionary(p=>p.placementId,p=>p.transform.localToWorldMatrix.ToString("R"));var session=All<PrototypeSession>(scene).Single();var fleet=session.Fleet.Select(m=>m.transform.localToWorldMatrix.ToString("R")).ToArray();var hashes=All<MeshFilter>(scene).Where(f=>f.sharedMesh).Select(f=>AssetDatabase.GetAssetPath(f.sharedMesh)).Concat(new[]{AssetDatabase.GetAssetPath(session.Snow.seed),AssetDatabase.GetAssetPath(All<Terrain>(scene).Single().terrainData)}).Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal)).Distinct().ToDictionary(p=>p,p=>AssetDatabase.GetAssetDependencyHash(p));
  Apply(scene);var w=All<CloudWindController>(scene).Single();var oldPoses=w.slots.Select(s=>s.initialCloud.localToWorldMatrix.ToString("R")).ToArray();Apply(scene);Check(oldPoses.SequenceEqual(w.slots.Select(s=>s.initialCloud.localToWorldMatrix.ToString("R"))),"repeat authoring rerandomized/compounded scale");Tests(w);SceneValidation.Validate(scene);MechanicsChecks.Run();EditorSceneManager.MarkSceneDirty(scene);Check(EditorSceneManager.SaveScene(scene),"save failed");scene=EditorSceneManager.OpenScene(SceneValidation.ScenePath);Validate(scene);SceneValidation.Validate(scene);session=All<PrototypeSession>(scene).Single();foreach(var p in All<CatalogPlacement>(scene).Where(p=>!Cloud(p)&&!string.IsNullOrEmpty(p.placementId)))Check(poses[p.placementId]==p.transform.localToWorldMatrix.ToString("R"),"non-cloud pose changed");Check(fleet.SequenceEqual(session.Fleet.Select(m=>m.transform.localToWorldMatrix.ToString("R"))),"fleet moved");foreach(var h in hashes)Check(h.Value==AssetDatabase.GetAssetDependencyHash(h.Key),"protected mesh/terrain/snow changed");
  Preview(scene);Overview(scene);EditorSceneManager.OpenScene(SceneValidation.ScenePath);
  File.WriteAllText(Logs+"/Validation.txt","PASS: Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+"; "+checks+" checks. Random startup disk scatter with guaranteed central coverage; 1500m root radius; 10-15 active clouds. Random upwind arc entries, 5-20 second arrival opportunities, 30-90 second population targets; minimum-only immediate replenishment. Two-hour disposable native simulation verifies bounded varying population, radial destruction, delayed arrivals, motion and pause. Original scale baselines retained, 0.5-3x size and full mesh altitude 250-450m; wind 3-8m/s, 300-900s targets, 20s transitions unchanged. Saved/reopened, scene and 19 snow checks passed; protected assets and non-cloud/fleet poses unchanged. Forty authored slots retain inactive source roots; active rendering 10-15 clouds, 3320-4980 triangles. Live Play, Android packaging and phone costs untested.");Debug.Log(File.ReadAllText(Logs+"/Validation.txt"));
 }
 static void Overview(Scene scene){
  var camera=All<Camera>(scene).First();var objects=All<CloudWindController>(scene).Single().GetComponentsInChildren<Transform>(true);var layers=objects.Select(t=>t.gameObject.layer).ToArray();
  var rt=new RenderTexture(1200,1200,24);var previous=RenderTexture.active;Texture2D image=null;bool fog=RenderSettings.fog;
  try{
   foreach(var t in objects)t.gameObject.layer=30;
   camera.cullingMask=1<<30;camera.layerCullDistances=new float[32];camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.32f,.48f,.58f);camera.orthographic=true;camera.orthographicSize=1600;camera.aspect=1;camera.nearClipPlane=.1f;camera.farClipPlane=4000;camera.transform.SetPositionAndRotation(new Vector3(0,2000,0),Quaternion.Euler(90,0,0));RenderSettings.fog=false;
   camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image=new Texture2D(1200,1200,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,1200),0,0);image.Apply();File.WriteAllBytes(Logs+"/Clouds-overhead.png",image.EncodeToPNG());
  }finally{for(int i=0;i<objects.Length;i++)objects[i].gameObject.layer=layers[i];RenderSettings.fog=fog;camera.targetTexture=null;RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);if(image)UnityEngine.Object.DestroyImmediate(image);}
 }
 static void Preview(Scene scene){var camera=All<Camera>(scene).First();camera.orthographic=false;camera.fieldOfView=60;camera.aspect=16f/9;foreach(int yaw in new[]{0,90,180,270}){camera.transform.SetPositionAndRotation(new Vector3(0,12,0),Quaternion.Euler(-12,yaw,0));var rt=new RenderTexture(1280,720,24);var previous=RenderTexture.active;Texture2D image=null;try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(Logs+"/Sky-"+yaw+".png",image.EncodeToPNG());}finally{camera.targetTexture=null;RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);if(image)UnityEngine.Object.DestroyImmediate(image);}}}
 }
}
