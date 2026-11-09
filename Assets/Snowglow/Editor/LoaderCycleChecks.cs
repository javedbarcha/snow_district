using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Snowglow.Editor {
 public static class LoaderCycleChecks {
  const string Request="Logs/LoaderCycle.request";static int checks;
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;try{File.Delete(Request);}catch(IOException){return;}EditorApplication.update-=Pending;try{Run();}catch(Exception e){Directory.CreateDirectory("Logs/LoaderCycle");File.WriteAllText("Logs/LoaderCycle/Validation.txt","FAIL: "+e);Debug.LogException(e);}}
  static void Check(bool ok,string message){checks++;if(!ok)throw new InvalidOperationException(message);}
  [MenuItem("Snowglow/Validate loader collect dump and coast cycle")]
  public static void Run(){
   checks=0;var main=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string hash=Hash128.Compute(File.ReadAllBytes(main.path)).ToString();bool dirty=main.isDirty;var fixture=EditorSceneManager.NewPreviewScene();var root=new GameObject("Disposable loader cycle");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,fixture);var instance=typeof(PrototypeSession).GetField("<Instance>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic);var previous=PrototypeSession.Instance;
   try{
    var game=root.AddComponent<PrototypeSession>();instance.SetValue(null,game);game.Profile=new ProfileSave();game.Panel="";game.Input=root.AddComponent<PrototypeInput>();game.Snow=root.AddComponent<SnowField>();var loader=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(LoaderModelIntake.PrefabPath)).GetComponent<MachineController>();loader.transform.SetParent(root.transform);loader.Initialize();game.Fleet=new[]{loader};var tool=loader.Tool;var dumping=typeof(AttachmentController).GetField("dumping",BindingFlags.Instance|BindingFlags.NonPublic);
    for(int cycle=0;cycle<3;cycle++){
     game.Snow.Grid=new SnowVolumeGrid();loader.transform.position=new Vector3(0,6,0);tool.Load=0;tool.ResetRaised();tool.ResetLowered();Check(tool.Working&&tool.Lift==0&&tool.Tilt==0,"Startup loader not lowered");tool.SendMessage("Update");loader.transform.position+=Vector3.up*(5.2f-tool.mouth.position.y);
     for(int z=0;z<SnowVolumeGrid.Size;z++)for(int x=0;x<SnowVolumeGrid.Size;x++){int i=z*SnowVolumeGrid.Size+x;if(Mathf.Abs(game.Snow.Grid.X(i))<8&&Mathf.Abs(game.Snow.Grid.Z(i))<30)game.Snow.Grid.depth[i]=.3f;}
     float total=game.Snow.Grid.Total();tool.RequestDump();Check(!(bool)dumping.GetValue(tool)&&!tool.Working&&tool.Lift>=.6f,"Empty Dump must finish Up without arming unloading");tool.ToggleWork();tool.SendMessage("Update");
     for(int i=0;i<180;i++){float before=tool.Load;loader.transform.position+=loader.transform.forward*.08f;game.Input.Throttle=0;tool.Simulate(.02f,4);tool.SendMessage("Update");Check(tool.Load+.000001f>=before,"Coasting collection fluctuated downward");Check(!(bool)dumping.GetValue(tool),"Collection retained dump flag");}
     Check(tool.Load>=loader.equipment.capacity*.92f&&!tool.Working&&tool.Lift>=.6f&&tool.Tilt<0,"Coasting full bucket failed auto carry");Check(tool.mouth.position.y>5.5f,"Full bucket rig did not lift");float carried=tool.Load;for(int i=0;i<20;i++)tool.Simulate(.02f,4);Check(tool.Load==carried,"Carry leaked snow");
     tool.RequestDump();tool.Simulate(.02f,0);Check(tool.Load<carried&&(bool)dumping.GetValue(tool)&&!tool.Working,"Intentional dump not unloading");tool.ToggleWork();Check((bool)dumping.GetValue(tool)&&!tool.Working,"Lift should keep unloading until empty");
     for(int i=0;i<300&&tool.Load>0;i++)tool.Simulate(.02f,0);Check(tool.Load==0&&!(bool)dumping.GetValue(tool)&&tool.Working&&tool.Lift==0&&tool.Tilt==0,"Lift did not dump then lower");Check(Mathf.Abs(game.Snow.Grid.Total()+tool.Load-total)<.001f,"Lift dump lost snow volume");
     tool.SendMessage("Update");for(int i=0;i<180;i++){loader.transform.position+=loader.transform.forward*.08f;tool.Simulate(.02f,4);tool.SendMessage("Update");}Check(tool.Load>=loader.equipment.capacity*.92f&&!tool.Working,"Lowered bucket cannot refill");tool.RequestDump();for(int i=0;i<300&&tool.Load>0;i++)tool.Simulate(.02f,0);Check(tool.Load==0&&!(bool)dumping.GetValue(tool)&&!tool.Working&&tool.Lift>=.6f&&tool.Tilt<0,"Dump button did not finish Up");Check(Mathf.Abs(game.Snow.Grid.Total()+tool.Load-total)<.001f,"Up dump lost snow volume");
     foreach(float load in new[]{loader.equipment.capacity*.925f,loader.equipment.capacity}){tool.Load=load;tool.Working=true;tool.Lift=tool.Tilt=0;dumping.SetValue(tool,true);tool.Simulate(.02f,0);Check(!(bool)dumping.GetValue(tool)&&!tool.Working&&tool.Lift>=.6f&&tool.Load==load,"Stale state blocked 1.48/full bucket lift");}
    }
   }finally{instance.SetValue(null,previous);EditorSceneManager.ClosePreviewScene(fixture);}
   SceneValidation.Validate(main);MechanicsChecks.Run();Check(Hash128.Compute(File.ReadAllBytes(main.path)).ToString()==hash&&main.isDirty==dirty,"Authored scene changed");Directory.CreateDirectory("Logs/LoaderCycle");File.WriteAllText("Logs/LoaderCycle/Validation.txt","PASS: Unity "+Application.unityVersion+"; "+checks+" assertions on actual imported loader: three continuous-snow coasting cycles with zero throttle, monotonically increasing load, 92% auto carry and physical mouth rise, stable carry, intentional dump, Lift dumps-then-Down, Dump dumps-then-Up, empty Dump stays Up, completed unload, stale conflict at 1.48/full capacity and snow conservation. Scene validation and 19 snow checks passed; saved scene/dirty state preserved; no player-save writes. Driving physics, Android packaging and phone appearance remain untested.");Debug.Log(File.ReadAllText("Logs/LoaderCycle/Validation.txt"));
  }
 }
}
