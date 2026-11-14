using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Snowglow.Editor {
 public static class JobGuidanceChecks {
  static int checks;
  static void Require(bool value,string reason){checks++;if(!value)throw new InvalidOperationException("Job guidance: "+reason);}
  public static void Run(PrototypeSession source){
   checks=0;var saved=source.gameObject.scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<JobGuidance>(true)).Single();
   Require(saved.venues.Length==3&&saved.arrow&&saved.vehicleHeights.Length==source.Fleet.Length,"saved bindings");
   Require(saved.GetComponentsInChildren<Collider>(true).Length==0&&saved.GetComponentsInChildren<Rigidbody>(true).Length==0,"visuals have no physics");
   Require(!ShaderUtil.ShaderHasError(Shader.Find("Snowglow/Job Hologram")),"hologram shader compiles");
   var legacy=source.gameObject.scene.GetRootGameObjects().FirstOrDefault(x=>x.name=="07 Contract reference markers");Require(!legacy||!legacy.activeSelf,"legacy boundaries hidden");
   // Exhaustively compare every grid cell against the pre-guidance contract rules.
   for(int kind=0;kind<3;kind++){
    bool identical=true;var bounds=SnowField.ContractBounds(kind);
    for(int z=0;z<SnowVolumeGrid.Size;z++)for(int x=0;x<SnowVolumeGrid.Size;x++){
     float px=SnowVolumeGrid.Origin+(x+.5f)*SnowVolumeGrid.Cell,pz=SnowVolumeGrid.Origin+(z+.5f)*SnowVolumeGrid.Cell;
     bool original=kind==1?px>77&&px<108&&pz>9&&pz<33:kind==2?px>-35&&px<35&&Mathf.Abs(pz)<4.5f:px>-59&&px<-29&&Mathf.Abs(pz+65)<4.5f;
     identical&=original==(px>bounds.xMin&&px<bounds.xMax&&pz>bounds.yMin&&pz<bounds.yMax);
    }
    Require(identical,"contract "+kind+" progress footprint unchanged");
    Require(saved.venues[kind].points.All(p=>Mathf.Abs(p.x-bounds.xMin)<.001f||Mathf.Abs(p.x-bounds.xMax)<.001f||Mathf.Abs(p.z-bounds.yMin)<.001f||Mathf.Abs(p.z-bounds.yMax)<.001f),"hologram follows target edges");
   }
   var preview=EditorSceneManager.NewPreviewScene();var fixture=new GameObject("Disposable guidance checks");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fixture,preview);
   var visual=UnityEngine.Object.Instantiate(saved.gameObject);var guidance=visual.GetComponent<JobGuidance>();
   try{
    var game=fixture.AddComponent<PrototypeSession>();game.Profile=new ProfileSave{inJob=true,active=0};game.Panel="";
    var snowObject=new GameObject("Read-only seed snow");snowObject.transform.SetParent(fixture.transform,false);game.Snow=snowObject.AddComponent<SnowField>();game.Snow.Grid=source.Snow.seed.grid;
    game.Fleet=new MachineController[2];for(int i=0;i<2;i++){var go=new GameObject("Visual test vehicle "+i);go.transform.SetParent(fixture.transform,false);game.Fleet[i]=go.AddComponent<MachineController>();go.transform.position=source.Fleet[i].transform.position;}
    for(int kind=0;kind<3;kind++){
     game.Snow.SetContract(kind);game.Profile.job=kind;var bounds=SnowField.ContractBounds(kind);var center=new Vector3(bounds.center.x,SnowField.Ground,bounds.center.y);
     game.Fleet[0].transform.position=new Vector3(bounds.xMin-35,SnowField.Ground,bounds.center.y);guidance.Hide();guidance.Step(game,1);
     Require(guidance.arrow.gameObject.activeSelf,"far arrow "+kind);Require(guidance.venues.Count(v=>v.root.activeSelf)==1&&guidance.venues[kind].root.activeSelf,"only active venue");
     Require(Vector3.Dot(guidance.arrow.forward,Vector3.right)>.999f,"arrow points to nearest venue edge");
     for(int i=0;i<guidance.venues[kind].points.Length;i++){var point=guidance.venues[kind].outline.GetPosition(i);Require(Mathf.Abs(point.y-(SnowField.Ground+game.Snow.Depth(point)+.09f))<.001f,"outline follows snow surface");}
     game.Fleet[0].transform.position=new Vector3(bounds.xMin-10,SnowField.Ground,bounds.center.y);guidance.Step(game,1);Require(!guidance.arrow.gameObject.activeSelf,"arrival hides arrow");
     game.Fleet[0].transform.position=new Vector3(bounds.xMin-12,SnowField.Ground,bounds.center.y);guidance.Step(game,1);Require(!guidance.arrow.gameObject.activeSelf,"arrival hysteresis prevents flicker");
     game.Fleet[0].transform.position=new Vector3(bounds.xMin-15,SnowField.Ground,bounds.center.y);guidance.Step(game,1);Require(guidance.arrow.gameObject.activeSelf,"leaving venue restores direction");
     game.Fleet[0].transform.position=center;guidance.Step(game,1);Require(!guidance.arrow.gameObject.activeSelf&&guidance.venues[kind].root.activeSelf,"inside: boundary remains, arrow hides");
     Capture(source,"venue-"+kind,center+new Vector3(32,38,42),center,Mathf.Max(bounds.width*.35f+10,bounds.height*.5f+10));
    }
    game.Snow.SetContract(1);game.Profile.job=1;game.Profile.active=0;game.Fleet[0].transform.position=source.Fleet[0].transform.position;guidance.Hide();guidance.Step(game,1);
    var position=game.Fleet[0].transform.position;Require(guidance.arrow.position.y>position.y+guidance.vehicleHeights[0],"arrow clears tractor roof");Capture(source,"direction-arrow",position+new Vector3(12,12,14),position+Vector3.up*2.5f,10);
    game.Profile.active=1;guidance.Step(game,1);Require((guidance.arrow.position-game.Fleet[1].transform.position).sqrMagnitude<100&&guidance.arrow.position.y>game.Fleet[1].transform.position.y+guidance.vehicleHeights[1],"arrow follows selected loader");
    game.Panel="pause";guidance.Step(game,1);Require(!guidance.arrow.gameObject.activeSelf&&guidance.venues.All(v=>!v.root.activeSelf),"menu hides guidance");
    game.Panel="";game.Profile.freeWork=true;guidance.Step(game,1);Require(!guidance.arrow.gameObject.activeSelf&&guidance.venues.All(v=>!v.root.activeSelf),"Free Work has no false venue");
    game.Profile.freeWork=false;game.Profile.inJob=false;guidance.Step(game,1);Require(!guidance.arrow.gameObject.activeSelf&&guidance.venues.All(v=>!v.root.activeSelf),"completed/no job hides guidance");
    File.WriteAllText("Logs/JobGuidance/Checks.txt","PASS: "+checks+" saved-visual, unchanged contract footprint, direction, arrival hysteresis, departure, snow seating, active vehicle, menu/free-work/completion checks. Seed grid was read-only. No Play entry or player-save writes.");
   }finally{UnityEngine.Object.DestroyImmediate(visual);EditorSceneManager.ClosePreviewScene(preview);}
  }
  static void Capture(PrototypeSession source,string name,Vector3 position,Vector3 target,float size){
   var obj=new GameObject("Temporary guidance capture");var camera=obj.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=size;camera.nearClipPlane=.2f;camera.farClipPlane=1000;camera.cullingMask=~((1<<5)|(1<<12));camera.clearFlags=CameraClearFlags.Skybox;camera.allowHDR=true;obj.transform.position=position;obj.transform.LookAt(target);obj.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;
   var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGBHalf);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);var old=RenderTexture.active;
   try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes("Logs/JobGuidance/"+name+".png",texture.EncodeToPNG());}
   finally{RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
  }
 }
}
