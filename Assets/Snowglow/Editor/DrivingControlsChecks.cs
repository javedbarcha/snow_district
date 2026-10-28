using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Snowglow.Editor {
 public static class DrivingControlsChecks {
  const string Request="Logs/DrivingControls.request";
  static void Check(bool condition,string name){if(!condition)throw new InvalidOperationException(name);}
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){
   if(!File.Exists(Request)){EditorApplication.update-=Pending;return;}
   if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
   EditorApplication.update-=Pending;File.Delete(Request);
   try{Run();}catch(Exception e){File.WriteAllText("Logs/DrivingControls.txt","FAIL: "+e);Debug.LogException(e);}
  }
  [MenuItem("Snowglow/Run driving input and loaded bucket checks")]
  public static void Run(){
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before checks.");
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();SceneValidation.Validate(scene);
   var source=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PrototypeSession>()).Single();
   var instance=typeof(PrototypeSession).GetField("<Instance>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic);
   var previous=PrototypeSession.Instance;var oldEvents=EventSystem.current;var oldKeyboard=Keyboard.current;float oldTime=Time.timeScale;
   GameObject fixture=null,probe=null;Keyboard keyboard=null;
   try{
    fixture=new GameObject("Driving controls regression fixture");fixture.hideFlags=HideFlags.HideAndDontSave;
    var session=fixture.AddComponent<PrototypeSession>();session.Profile=new ProfileSave();session.Panel="";session.Input=fixture.AddComponent<PrototypeInput>();
    instance.SetValue(null,session);
    probe=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(LoaderModelIntake.PrefabPath));probe.hideFlags=HideFlags.HideAndDontSave;
    var loader=probe.GetComponent<MachineController>();loader.Initialize();session.Fleet=new[]{loader};session.Profile.active=0;
    session.Snow=fixture.AddComponent<SnowField>();
    var events=fixture.AddComponent<EventSystem>();EventSystem.current=events;
    keyboard=InputSystem.AddDevice<Keyboard>();
    void Keys(params Key[] keys){InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));InputSystem.Update();session.Input.SendMessage("Update");}
    Keys(Key.W);Check(session.Input.Throttle==1&&!session.Input.Reverse,"W forward");
    Keys(Key.S);Check(session.Input.Throttle==1&&session.Input.Reverse,"S reverse");
    Keys(Key.UpArrow);Check(session.Input.Throttle==1&&!session.Input.Reverse,"Up forward");
    Keys(Key.DownArrow);Check(session.Input.Throttle==1&&session.Input.Reverse,"Down reverse");
    Keys(Key.A);Check(session.Input.Steer==-1,"A steering");Keys(Key.RightArrow);Check(session.Input.Steer==1,"arrow steering");
    Keys(Key.W,Key.S);Check(session.Input.Throttle==0&&session.Input.Brake==1,"opposing keys brake");
    events.SetSelectedGameObject(fixture);Keys(Key.Space);Check(session.Input.Brake==1&&!events.sendNavigationEvents&&events.currentSelectedGameObject==null,"Space brakes without HUD navigation/submit");
    session.Panel="pause";Keys();Check(events.sendNavigationEvents,"menu keyboard navigation restored");session.Panel="";
    var tool=loader.Tool;tool.Auto=true;tool.Working=true;tool.Load=loader.equipment.capacity;tool.Lift=tool.Tilt=0;tool.SendMessage("Update");float low=tool.mouth.position.y;
    tool.Simulate(.02f,0);tool.SendMessage("Update");
    Check(!tool.Working&&tool.Lift>=.6f&&tool.mouth.position.y>low+.3f,"stationary/restored full bucket auto-carry physically raises mouth");
    float load=tool.Load;tool.Lift=.9f;tool.Simulate(.02f,0);Check(tool.Lift>=.9f&&tool.Load==load,"auto carry never lowers a higher boom or changes load");
    tool.Load=loader.equipment.capacity*.5f;tool.Lift=0;tool.Tilt=0;tool.Working=true;tool.Simulate(.02f,0);Check(tool.Lift==0&&tool.Working,"partial bucket stays at work height");
    tool.Load=loader.equipment.capacity;tool.Auto=true;tool.Lift=.3f;Keys(Key.R);tool.Simulate(.5f,0);Check(!tool.Auto&&tool.Lift>.3f,"R takes manual control and lifts a full bucket");
    Keys(Key.F);tool.Simulate(1,0);Check(tool.Lift<.18f&&tool.Working,"F lowers and re-enables loading");
    Keys(Key.T);tool.Simulate(.1f,0);Check(session.Input.Tilt==1&&tool.Tilt>0,"T tilts");Keys(Key.G);Check(session.Input.Tilt==-1,"G curls");
    Keys();tool.RequestDump();tool.Simulate(.02f,0);Check(tool.Tilt==1&&tool.Lift>=tool.dumpMinimumLift,"dump retains tip pose with full bucket");
    MechanicsChecks.Run();
    File.WriteAllText("Logs/DrivingControls.txt","PASS: Unity "+Application.unityVersion+" / "+EditorUserBuildSettings.activeBuildTarget+". Injected Input System keyboard events exercised W/S/arrows/steering/brake/opposed keys/R/F/T/G; gameplay UI navigation disabled and menu navigation restored. Actual imported loader fixture passed stationary full-load auto carry, mouth elevation, partial-load work, higher-boom preservation, manual takeover/lower and dump pose checks. Scene structure and 19 snow ledger checks passed. No player saves or authored scene assets changed. Physical keyboard/Game view focus, continuous driving/loading, Android build and device behavior remain untested.");
    Debug.Log(File.ReadAllText("Logs/DrivingControls.txt"));
   }finally{
    instance.SetValue(null,previous);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(oldKeyboard!=null)oldKeyboard.MakeCurrent();
    if(probe)UnityEngine.Object.DestroyImmediate(probe);if(fixture)UnityEngine.Object.DestroyImmediate(fixture);EventSystem.current=oldEvents;Time.timeScale=oldTime;
   }
  }
 }
}
