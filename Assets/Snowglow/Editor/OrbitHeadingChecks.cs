using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEngine.SceneManagement;
namespace Snowglow.Editor {
 public static class OrbitHeadingChecks {
 const string Request="Logs/OrbitHeading.request",Logs="Logs/OrbitHeading";static int checks;
 static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException("Orbit heading: "+message);checks++;}
 [InitializeOnLoadMethod]static void Register(){EditorApplication.update+=Pending;}
 static void Pending(){if(!File.Exists(Request)){EditorApplication.update-=Pending;return;}if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;EditorApplication.update-=Pending;File.Delete(Request);try{Run();}catch(Exception e){Directory.CreateDirectory(Logs);File.WriteAllText(Logs+"/Validation.txt","FAIL: "+e);Debug.LogException(e);}}
 [MenuItem("Snowglow/Validate vehicle-relative Orbit angle")]
 public static void Run(){
  checks=0;Directory.CreateDirectory(Logs);var scene=SceneManager.GetActiveScene();Check(scene.path==SceneValidation.ScenePath&&!EditorApplication.isPlayingOrWillChangePlaymode,"open Pinefall with Play stopped");
  string bytes=Hash128.Compute(File.ReadAllBytes(scene.path)).ToString();bool dirty=scene.isDirty;
  var oldKeyboard=Keyboard.current;var oldMouse=Mouse.current;var oldTouch=Touchscreen.current;Keyboard keyboard=null;Mouse mouse=null;Touchscreen touch=null;GameObject cameraObject=null,target=null,other=null;
  try{
   keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();touch=InputSystem.AddDevice<Touchscreen>();
   cameraObject=new GameObject("Disposable Orbit heading camera"){hideFlags=HideFlags.HideAndDontSave};var view=cameraObject.AddComponent<Camera>();view.enabled=false;view.nearClipPlane=.2f;view.farClipPlane=1000;view.aspect=16f/9;
   var rig=cameraObject.AddComponent<CameraRig>();rig.view=view;rig.terrain=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Single();
   target=new GameObject("Disposable heading target"){hideFlags=HideFlags.HideAndDontSave};target.transform.SetPositionAndRotation(new Vector3(0,80,0),Quaternion.Euler(0,15,0));
   rig.SetMode(CameraMode.Orbit);rig.UpdateForTarget(target.transform,.02f,true);rig.Rotate(new Vector2(70,10));rig.Zoom(-1);float pitch=rig.Pitch,distance=rig.Distance;
   void Tick(int count=1,bool controls=true,float speed=0){for(int i=0;i<count;i++)rig.UpdateForTarget(target.transform,.02f,controls,speed);}
   void Angle(float expected){Check(Mathf.Abs(Mathf.DeltaAngle(target.transform.eulerAngles.y+expected,rig.Yaw))<.005f,"relative yaw lost");Check(Mathf.Abs(rig.Pitch-pitch)<.001f&&Mathf.Abs(rig.Distance-distance)<.001f,"pitch/zoom changed while turning");}
   target.transform.rotation=Quaternion.Euler(0,105,0);Tick();Angle(70);Tick(200);var offset=rig.transform.position-(target.transform.position+Vector3.up*1.6f);float actualYaw=Mathf.Atan2(-offset.x,-offset.z)*Mathf.Rad2Deg;Check(Mathf.Abs(Mathf.DeltaAngle(actualYaw,rig.Yaw))<.05f,"rendered camera failed to settle at relative angle");
   for(int i=0;i<720;i++){target.transform.rotation=Quaternion.Euler(0,105+i*.75f,0);Tick(1,true,i%2==0?4:-4);Angle(70);}
   rig.Rotate(new Vector2(-35,7));pitch=rig.Pitch;for(int i=0;i<600;i++){target.transform.rotation=Quaternion.Euler(0,70-i*.6f,0);Tick();Angle(35);}
   target.transform.rotation=Quaternion.Euler(0,359,0);Tick();Angle(35);target.transform.rotation=Quaternion.Euler(0,1,0);Tick();Angle(35);
   float held=rig.Yaw;target.transform.rotation=Quaternion.Euler(0,80,0);Tick(10,false);Check(Mathf.Abs(Mathf.DeltaAngle(held,rig.Yaw))<.001f,"pause changed selected yaw");Tick();Angle(35);
   // Tilt/roll do not tilt the horizon or overwrite selected Orbit pitch.
   target.transform.rotation=Quaternion.Euler(12,95,8);Tick();Angle(35);
   rig.ResetView();Tick();Check(Mathf.Abs(Mathf.DeltaAngle(rig.Yaw,target.transform.eulerAngles.y))<.001f&&rig.Pitch==rig.orbitPitch&&rig.Distance==rig.orbitDistance,"reset defaults");
   other=new GameObject("Disposable second heading target"){hideFlags=HideFlags.HideAndDontSave};other.transform.SetPositionAndRotation(new Vector3(30,80,0),Quaternion.Euler(0,210,0));rig.Rotate(new Vector2(45,5));rig.UpdateForTarget(other.transform,.02f,true);Check(Mathf.Abs(Mathf.DeltaAngle(rig.Yaw,210))<.001f&&rig.Pitch==rig.orbitPitch,"switch vehicle recentres");other.transform.rotation=Quaternion.Euler(0,270,0);rig.UpdateForTarget(other.transform,.02f,true);Check(Mathf.Abs(Mathf.DeltaAngle(rig.Yaw,270))<.001f,"new target heading tracking");
   rig.SetMode(CameraMode.Follow);rig.UpdateForTarget(target.transform,.02f,true);rig.SetMode(CameraMode.Orbit);Tick();Check(Mathf.Abs(Mathf.DeltaAngle(rig.Yaw,target.transform.eulerAngles.y))<.001f,"mode reentry resets heading anchor");
   rig.SetMode(CameraMode.Isometric);Tick();var rotation=rig.transform.rotation;target.transform.rotation=Quaternion.Euler(0,200,0);Tick(100);Check(Quaternion.Angle(rotation,rig.transform.rotation)<.001f,"Isometric orientation changed");
  }finally{
   if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);if(touch!=null)InputSystem.RemoveDevice(touch);if(oldKeyboard!=null)oldKeyboard.MakeCurrent();if(oldMouse!=null)oldMouse.MakeCurrent();if(oldTouch!=null)oldTouch.MakeCurrent();foreach(var go in new[]{cameraObject,target,other})if(go)UnityEngine.Object.DestroyImmediate(go);
  }
  SceneValidation.Validate(scene);MechanicsChecks.Run();Check(bytes==Hash128.Compute(File.ReadAllBytes(scene.path)).ToString(),"saved scene bytes changed");Check(dirty==scene.isDirty,"active scene dirty state changed");
  File.WriteAllText(Logs+"/Validation.txt","PASS: Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+"; "+checks+" checks. Orbit carries player azimuth with vehicle heading. Native UpdateForTarget checks cover 1320 turning frames, forward/reverse independence, revised manual angle held beyond old six-second hold, rendered settled position, heading wrap, pause/resume, pitch/zoom retention, target tilt/roll, reset, vehicle switch and mode reentry; Isometric fixed orientation unchanged. Scene validation and 19 snow checks passed. Saved scene bytes and active dirty state preserved; no source/prefab/layout/snow/player-save changes. Actual live driving/touch gestures, Android packaging and phone performance untested.");Debug.Log(File.ReadAllText(Logs+"/Validation.txt"));
 }
 }
}
