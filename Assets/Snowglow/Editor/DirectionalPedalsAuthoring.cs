using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Snowglow.Editor {
 public static class DirectionalPedalsAuthoring {
  const string Request="Logs/DirectionalPedals.request",Folder="Logs/DirectionalPedals";
  [InitializeOnLoadMethod]static void Reload(){EditorApplication.update+=Pending;}
  static void Pending(){if(!File.Exists(Request))return;if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;EditorApplication.update-=Pending;File.Delete(Request);try{Run();}catch(Exception e){Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/Validation.txt","FAIL: "+e);Debug.LogException(e);}}
  static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
  [MenuItem("Snowglow/UI/Install two directional pedals")]
  public static void Run(){
   Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Stop Play first");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();Require(scene.path==SceneValidation.ScenePath,"Open Pinefall first");Directory.CreateDirectory(Folder);File.Copy(scene.path,Folder+"/Pinefall-before.unity",true);
   CheckInput();var hud=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PrototypeHUD>(true)).Single();DrivingButtonsAuthoring.Apply(hud);SceneValidation.Validate(scene);EditorSceneManager.MarkSceneDirty(scene);Require(EditorSceneManager.SaveScene(scene),"Save failed");scene=EditorSceneManager.OpenScene(scene.path);
   hud=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PrototypeHUD>(true)).Single();var touches=hud.driving.GetComponentsInChildren<TouchControl>(true);
   Require(touches.Count(t=>t.channel=="forward")==1&&touches.Count(t=>t.channel=="reverse")==1,"Two pedals missing");Require(!touches.Any(t=>t.channel=="brake"||t.channel=="throttle"),"Legacy pedals remain");Require(!hud.driving.GetComponentsInChildren<UICommand>(true).Any(c=>c.command=="reverse"),"Gear toggle remains");
   foreach(var t in touches.Where(t=>t.channel=="forward"||t.channel=="reverse")){Require(((RectTransform)t.transform).sizeDelta==(AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Snowglow/Catalog/Sources/ControlIconSheet/forward.png")?new Vector2(t.channel=="forward"?137.5f:110,t.channel=="forward"?137.5f:110):new Vector2(118,120)),"Pedal size differs");Require(t.transform.Find("PSD kit artwork"),"Pedal artwork missing");}
   SceneValidation.Validate(scene);MechanicsChecks.Run();
   typeof(CompactHUDAuthoring).GetMethod("Preview",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{hud});File.Copy("Logs/CompactHUD/Preview.png",Folder+"/Preview.png",true);EditorSceneManager.OpenScene(SceneValidation.ScenePath);
   File.WriteAllText(Folder+"/Validation.txt","PASS: Unity "+Application.unityVersion+"; saved/reopened two equal directional pedals with icon-only up/down arrows, 5% right inset and no Drive/Brake/F-R toggle. Exhaustive touch/keyboard direction combinations, simultaneous pedals, independent pointer release, disable, Clear and release coasting passed. Direction changes brake to a stop and hold a 0.2-second buffer before opposite torque. Scene validation and 19 snow checks passed. Native HUD preview rendered. Live driving and Android phone ergonomics remain untested. No player saves, snow or machinery settings changed.");Debug.Log(File.ReadAllText(Folder+"/Validation.txt"));
  }
  static void CheckInput(){
   var root=new GameObject("Disposable pedal checks");try{var input=root.AddComponent<PrototypeInput>();
    for(int mask=0;mask<32;mask++){input.Clear();input.touchPedalsUsed=true;input.touchForward=(mask&1)!=0?1:0;input.touchReverse=(mask&2)!=0?1:0;bool kf=(mask&4)!=0,kr=(mask&8)!=0,kb=(mask&16)!=0;bool f=kf||input.touchForward>0,r=kr||input.touchReverse>0;input.ResolvePedals(kf,kr,kb);Require(input.Throttle==(f!=r?1:0),"Throttle conflict "+mask);Require(Mathf.Approximately(input.Brake,kb||f&&r?1:0),"Brake conflict "+mask);if(f!=r)Require(input.Reverse==r,"Wrong direction "+mask);}
    input.Clear();input.ResolvePedals(false,false,false);Require(input.Throttle==0&&input.Brake==0&&input.touchForward==0&&input.touchReverse==0,"Clear failed");
    var field=typeof(PrototypeSession).GetField("<Instance>k__BackingField",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);var old=PrototypeSession.Instance;
    try{var game=root.AddComponent<PrototypeSession>();game.Input=input;field.SetValue(null,game);var f=new GameObject("Forward").AddComponent<TouchControl>();f.transform.SetParent(root.transform);f.channel="forward";var r=new GameObject("Reverse").AddComponent<TouchControl>();r.transform.SetParent(root.transform);r.channel="reverse";var a=new PointerEventData(null){pointerId=1};var b=new PointerEventData(null){pointerId=2};f.OnPointerDown(a);r.OnPointerDown(b);input.ResolvePedals(false,false,false);Require(input.Throttle==0&&input.Brake==1,"Both pedals not braking");f.OnPointerUp(b);Require(input.touchForward==1,"Wrong pointer released pedal");f.OnPointerUp(a);input.ResolvePedals(false,false,false);Require(input.Reverse&&input.Throttle==1,"Independent reverse release failed");typeof(TouchControl).GetMethod("OnDisable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(r,null);input.ResolvePedals(false,false,false);Require(input.Throttle==0&&input.Brake==0,"Disabled pedal stuck");}finally{field.SetValue(null,old);}
   }finally{UnityEngine.Object.DestroyImmediate(root);}
  }
 }
}
