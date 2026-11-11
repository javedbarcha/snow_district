using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Snowglow.Editor {
 public static class MenuRecoveryChecks {
  static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new InvalidOperationException(why);}
  public static void Run(){
   checks=0;var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var source=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<PrototypeSession>(true)).Single();var fixture=EditorSceneManager.NewPreviewScene();var root=new GameObject("Disposable menu/recovery checks");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,fixture);var field=typeof(PrototypeSession).GetField("<Instance>k__BackingField",BindingFlags.Static|BindingFlags.NonPublic);var previous=PrototypeSession.Instance;float scale=Time.timeScale;
   try{
    var game=root.AddComponent<PrototypeSession>();field.SetValue(null,game);game.Profile=new ProfileSave{inJob=true};game.Input=root.AddComponent<PrototypeInput>();game.Snow=root.AddComponent<SnowField>();game.HUD=root.AddComponent<PrototypeHUD>();game.HUD.panels=new GameObject[0];game.HUD.names=new string[0];game.HUD.driving=new GameObject("Driving");game.HUD.driving.transform.SetParent(root.transform);game.HUD.menuRoot=new GameObject("Menu");game.HUD.menuRoot.transform.SetParent(root.transform);
    game.Fleet=source.Fleet.Select(m=>{var clone=UnityEngine.Object.Instantiate(m.gameObject);clone.transform.SetParent(root.transform);clone.transform.position=new Vector3(-95,6,-90);var result=clone.GetComponent<MachineController>();result.Initialize();result.Owned=true;return result;}).ToArray();
    for(int i=0;i<game.Fleet.Length;i++){
     game.Profile.active=i;var machine=game.Active;machine.SetActiveMachine(true);machine.Body.position=new Vector3(20,6,20);machine.transform.position=machine.Body.position;machine.Fuel=31;machine.Condition=43;machine.Tool.Working=true;machine.Tool.Lift=0;machine.Tool.Load=machine.definition.loader?.8f:0;
     game.Snow.Grid=new SnowVolumeGrid();float carried=machine.Tool.Load;game.Snow.Grid.initialVolume=carried;var pickup=machine.Tool.mouth.position;game.Panel="service";Time.timeScale=0;game.Input.touchForward=1;
     game.Recover();Check(game.Panel==""&&Time.timeScale==1&&game.HUD.driving.activeSelf&&!game.HUD.menuRoot.activeSelf,"Recovery did not resume");Check(machine.AtDepot&&machine.Body.linearVelocity==Vector3.zero,"Recovery location/velocity");Check(!machine.Tool.Working&&machine.Tool.Lift>=.6f,"Recovered attachment not raised");Check(machine.Fuel==31&&machine.Condition==43,"Recovery serviced vehicle");Check(game.Input.touchForward==0&&game.Input.Throttle==0,"Recovery input stuck");Check(machine.Tool.Load<.00001f,"Bucket did not unload");Check(Mathf.Abs(game.Snow.Grid.Total()-carried)<.0001f,"Recovery lost snow volume");if(carried>0){int p=game.Snow.Grid.Index(pickup.x,pickup.z);Check(p>=0&&game.Snow.Grid.depth.Any(d=>d>0),"Pickup snow absent");Check(game.Snow.Depth(machine.transform.position)==0,"Snow deposited at depot");}
    }
    foreach(int index in Enumerable.Range(0,game.Fleet.Length)){game.Panel="garage";Time.timeScale=0;game.Switch(index);Check(game.Profile.active==index&&game.Panel==""&&Time.timeScale==1,"Selection did not resume");Check(game.HUD.driving.activeSelf&&!game.HUD.menuRoot.activeSelf,"Selection view incorrect");}
    game.Panel="garage";int other=(game.Profile.active+1)%game.Fleet.Length;game.Fleet[other].Owned=false;game.Switch(other);Check(game.Panel=="garage","Rejected selection closed menu");
    game.Snow.Grid=new SnowVolumeGrid();var pos=new Vector3(30,6,30);int center=game.Snow.Grid.Index(pos.x,pos.z);game.Snow.Grid.blocked[center]=true;float accepted=game.Snow.DropRecoveryLoad(pos,1.6f);Check(Mathf.Abs(accepted-1.6f)<.0001f&&Mathf.Abs(game.Snow.Grid.Total()-1.6f)<.0001f,"Blocked discharge lost volume");
    Directory.CreateDirectory("Logs/MenuRecovery");File.WriteAllText("Logs/MenuRecovery/Validation.txt","PASS: "+checks+" actual-session fixture assertions: tractor/loader recover and resume, attachment raised, pickup snow volume conserved, depot not used for unloading, fuel/condition unchanged, controls clear, all fleet selections resume, denied selection stays in menu, blocked discharge conservation. Disposable preview scene; session ready=false prevents player-save writes. Native phone/live acceptance separate.");
   }finally{field.SetValue(null,previous);Time.timeScale=scale;EditorSceneManager.ClosePreviewScene(fixture);}
  }
 }
}
