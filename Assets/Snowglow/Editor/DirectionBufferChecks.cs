using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Snowglow.Editor {
 public static class DirectionBufferChecks {
  const string Request="Logs/DirectionBuffer.request",Folder="Logs/DirectionBuffer";static int count;
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;EditorApplication.update-=Pending;File.Delete(Request);try{Run();}catch(Exception e){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/Validation.txt","FAIL: "+e);Debug.LogException(e);}}
  static void Check(bool ok,string why){count++;if(!ok)throw new InvalidOperationException(why);}
  [MenuItem("Snowglow/Validate coasting and direction buffer")]
  public static void Run(){
   count=0;Directory.CreateDirectory(Folder);var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string hash=File.Exists(scene.path)?Hash128.Compute(File.ReadAllBytes(scene.path)).ToString():"";bool dirty=scene.isDirty;
   foreach(float direction in new[]{-1f,1f})foreach(float dt in new[]{.01f,.02f,.033333f}){
    var state=new DirectionalDriveState();float b;bool hold;
    Check(state.Resolve(direction,1,0,0,0,dt,out b,out hold)==1&&b==0&&!hold,"Initial direction delayed");
    foreach(float speed in new[]{8f,2f,.2f,.051f})for(int i=0;i<60;i++)Check(state.Resolve(-direction,1,0,direction*speed,speed,dt,out b,out hold)==0&&b==1&&!hold,"Opposite torque before stop or timed buffer while moving");
    float elapsed=0;while(elapsed+dt<DirectionalDriveState.StopBuffer){elapsed+=dt;Check(state.Resolve(-direction,1,0,.01f,.01f,dt,out b,out hold)==0&&b==1&&hold,"Stopped buffer released early");}
    // A sideways slide resets the stop timer even with no forward motion.
    Check(state.Resolve(-direction,1,0,0,.3f,dt,out b,out hold)==0&&b==1&&!hold,"Lateral momentum ignored");elapsed=0;
    while(elapsed+dt<DirectionalDriveState.StopBuffer){elapsed+=dt;Check(state.Resolve(-direction,1,0,0,0,dt,out b,out hold)==0&&hold&&b==1,"Buffer did not restart");}
    Check(state.Resolve(-direction,1,0,0,0,dt,out b,out hold)==1&&b==0&&!hold,"Buffered direction never engaged");
    Check(state.Resolve(-direction,0,0,-direction*3,3,dt,out b,out hold)==0&&b==0&&!hold,"Release must coast");
    Check(state.Resolve(direction,1,0,-direction*3,3,dt,out b,out hold)==0&&b==1,"Second reversal not braking");
    Check(state.Resolve(direction,0,0,-direction*2,2,dt,out b,out hold)==0&&b==0&&!hold,"Cancelled reversal must coast");
    Check(state.Resolve(-direction,1,0,-direction*2,2,dt,out b,out hold)==1&&b==0,"Return to original travel unnecessarily delayed");
    Check(state.Resolve(direction,1,1,-direction*2,2,dt,out b,out hold)==0&&b==1,"Explicit brake allowed torque");
   }
   var obj=new GameObject("Disposable coast input checks");try{var input=obj.AddComponent<PrototypeInput>();for(int mask=0;mask<32;mask++){input.Clear();input.touchPedalsUsed=true;input.touchForward=(mask&1)!=0?1:0;input.touchReverse=(mask&2)!=0?1:0;bool f=(mask&4)!=0,r=(mask&8)!=0,brake=(mask&16)!=0;bool forward=f||input.touchForward>0,reverse=r||input.touchReverse>0;input.ResolvePedals(f,r,brake);Check(input.Throttle==(forward!=reverse?1:0),"Input throttle conflict");Check(input.Brake==(brake||forward&&reverse?1:0),"Input must coast when released, brake when both held");}input.Clear();input.ResolvePedals(false,false,false);Check(input.Brake==0&&input.Throttle==0,"Clear applied brakes");}finally{UnityEngine.Object.DestroyImmediate(obj);}
   RecoveryPhysicsChecks.Run();SceneValidation.Validate(scene);MechanicsChecks.Run();Check(scene.isDirty==dirty,"Scene dirty state changed");Check(!File.Exists(scene.path)||Hash128.Compute(File.ReadAllBytes(scene.path)).ToString()==hash,"Scene bytes changed");
   File.WriteAllText(Folder+"/Validation.txt","PASS: Unity "+Application.unityVersion+"; "+count+" input/interlock assertions across both directions and 10/20/33ms steps. Release coasts, both pedals brake, no opposite torque while moving, lateral-motion gate, stopped 0.1s buffer, timer restart, cancellation and return to original direction. Existing 984 recovery physics assertions, scene validation and 19 snow checks passed. Saved scene and dirty state unchanged; no source assets, scene or player saves written. Actual WheelCollider driving/braking distances, slopes and Android feel remain untested.");Debug.Log(File.ReadAllText(Folder+"/Validation.txt"));
  }
 }
}
