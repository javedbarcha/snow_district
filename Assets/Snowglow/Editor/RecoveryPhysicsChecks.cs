using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Snowglow.Editor {
 public static class RecoveryPhysicsChecks {
  const string Request="Logs/RecoveryPhysics.request";
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){if(!File.Exists(Request)){EditorApplication.update-=Pending;return;}if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}if(EditorApplication.isPlayingOrWillChangePlaymode)return;EditorApplication.update-=Pending;File.Delete(Request);try{Run();}catch(Exception e){Directory.CreateDirectory("Logs/RecoveryPhysics");File.WriteAllText("Logs/RecoveryPhysics/Validation.txt","FAIL: "+e);Debug.LogException(e);}}
  static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException("Recovery: "+message);}
  [MenuItem("Snowglow/Run recovery physics checks")]
  public static void Run(){
   Directory.CreateDirectory("Logs/RecoveryPhysics");var main=SceneManager.GetActiveScene();var session=main.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PrototypeSession>(true)).Single();
   var fixture=EditorSceneManager.NewPreviewScene();var physics=fixture.GetPhysicsScene();bool sync=Physics.autoSyncTransforms;int assertions=0;
   try{
    Require(physics.IsValid()&&!physics.Equals(Physics.defaultPhysicsScene),"preview scene must isolate physics");
    Physics.autoSyncTransforms=false;
    foreach(var source in session.Fleet){
     var clone=UnityEngine.Object.Instantiate(source.gameObject);SceneManager.MoveGameObjectToScene(clone,fixture);var machine=clone.GetComponent<MachineController>();machine.Initialize();machine.Body.useGravity=false;
     var depot=machine.transform.position+Vector3.up*.3f;float load=machine.definition.loader ? .8f : 0;machine.Tool.Load=load;
     foreach(bool active in new[]{true,false}){
      machine.SetActiveMachine(true);machine.Body.position=depot+new Vector3(35,2,20);machine.Body.rotation=Quaternion.Euler(0,120,0);machine.Body.linearVelocity=new Vector3(5,0,3);machine.Body.angularVelocity=Vector3.up;physics.Simulate(.02f);
      // Exercise stored wheel spin and force/torque queued immediately before recovery.
      foreach(var wheel in machine.wheels){wheel.rotationSpeed=720;wheel.motorTorque=1000;}
      machine.Body.AddForce(new Vector3(20000,0,12000));machine.Body.AddTorque(Vector3.up*15000);
      machine.SetActiveMachine(active);machine.RecoverToDepot();
      Require(machine.Body.linearVelocity==Vector3.zero&&machine.Body.angularVelocity==Vector3.zero,"recovery velocity not zero");assertions++;
      Require(machine.wheels.All(w=>Mathf.Abs(w.rotationSpeed)<.001f&&w.motorTorque==0),"wheel momentum not cleared");assertions++;
      Require(Vector3.Distance(machine.Body.position,depot)<.001f,source.name+" physics pose did not teleport");assertions++;
      Require(machine.Body.isKinematic==!active&&machine.Body.interpolation==RigidbodyInterpolation.Interpolate,"activity/interpolation changed");assertions++;
      Require(machine.Tool.Load==load,"bucket load changed");assertions++;
      for(int i=0;i<120;i++){physics.Simulate(.02f);Require(Vector3.Distance(machine.Body.position,depot)<.02f,"snapped back after physics tick "+i);assertions++;Require(machine.Body.linearVelocity.sqrMagnitude<.000001f&&machine.Body.angularVelocity.sqrMagnitude<.000001f,"momentum returned on physics tick "+i);assertions++;}
      machine.SetActiveMachine(true);physics.Simulate(.02f);Require(machine.Body.linearVelocity.sqrMagnitude<.001f&&Vector3.Distance(machine.Body.position,depot)<.02f,"stale momentum after activation");assertions++;
     }
     machine.SetActiveMachine(true);var resumePosition=depot+new Vector3(25,3,15);var resumeRotation=Quaternion.Euler(0,125,0);machine.Body.position=resumePosition;machine.Body.rotation=resumeRotation;machine.Body.linearVelocity=new Vector3(7,0,3);machine.Body.angularVelocity=Vector3.up*2;foreach(var wheel in machine.wheels){wheel.motorTorque=1000;wheel.rotationSpeed=720;wheel.steerAngle=30;}machine.Body.AddForce(Vector3.forward*25000);machine.Body.AddTorque(Vector3.up*16000);machine.ResetMotion();
     Require(Vector3.Distance(machine.Body.position,resumePosition)<.001f&&Quaternion.Angle(machine.Body.rotation,resumeRotation)<.001f,"resume must retain pose");assertions++;Require(machine.Tool.Load==load,"resume retains bucket");assertions++;
     for(int i=0;i<60;i++){physics.Simulate(.02f);Require(Vector3.Distance(machine.Body.position,resumePosition)<.02f&&machine.Body.linearVelocity.sqrMagnitude<.000001f&&machine.Body.angularVelocity.sqrMagnitude<.000001f,"resume queued motion returned at step "+i);assertions++;}
     UnityEngine.Object.DestroyImmediate(clone);
    }
    SceneValidation.Validate(main);MechanicsChecks.Run();File.WriteAllText("Logs/RecoveryPhysics/Validation.txt","PASS: Unity "+Application.unityVersion+"; "+assertions+" assertions on actual tractor/loader clones in isolated 3D physics. Zero linear/angular velocity and wheel rotation, queued force/torque cancellation, active/inactive recovery, interpolated Rigidbody, autoSyncTransforms=false, loaded bucket retained, 120 post-recovery steps per case, reactivation without stale momentum, and resume ResetMotion with retained pose/load plus 60 physics steps per vehicle. Main scene validation and 19 snow ledger checks passed. No scene assets or player saves written. Full live driving/recovery click and device validation remain untested.");Debug.Log(File.ReadAllText("Logs/RecoveryPhysics/Validation.txt"));
   }finally{Physics.autoSyncTransforms=sync;EditorSceneManager.ClosePreviewScene(fixture);SceneManager.SetActiveScene(main);}
  }
 }
}
