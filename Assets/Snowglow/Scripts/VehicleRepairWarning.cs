using UnityEngine;
using UnityEngine.UI;
namespace Snowglow {
 // One saved text element is reused for every accelerator press.
 public sealed class VehicleRepairWarning:MonoBehaviour {
  public Text label;public float[] vehicleHeights;public float duration=1.25f,rise=36;
  bool pressed;MachineController previous;float age=10;
  public int PulseCount {get;private set;}
  public bool Animating=>age<duration;
  public float Opacity=>Animating?1-Mathf.SmoothStep(0,1,Mathf.Clamp01(age/duration)):0;
  public void Step(PrototypeSession game,float dt){
   if(!game||game.Profile==null||!game.IsRunning){Clear();return;}
   var machine=game.Active;bool down=game.Input.Throttle>.02f;
   if(machine!=previous){pressed=false;age=duration;previous=machine;}
   if(down&&!pressed&&machine.Condition<=0){age=0;PulseCount++;}
   else age+=Mathf.Max(0,dt);
   pressed=down;
   if(machine.Condition>0)age=duration;
   var color=label.color;color.a=Opacity;
   var camera=game.CameraRig?game.CameraRig.view:null;
   if(Animating&&camera){
    int index=game.Profile.active;float height=vehicleHeights!=null&&index<vehicleHeights.Length?vehicleHeights[index]:3.5f;
    Vector3 point=camera.WorldToScreenPoint(machine.transform.position+Vector3.up*height);
    var canvas=label.canvas;var parent=(RectTransform)label.transform.parent;
    if(point.z>0&&point.x>=0&&point.x<=Screen.width&&point.y>=0&&point.y<=Screen.height&&RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,point,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var local)){
     label.rectTransform.position=parent.TransformPoint(local+Vector2.up*(rise*Mathf.Clamp01(age/duration)));
    }else color.a=0;
   }else color.a=0;
   label.color=color;
  }
  public void Clear(){pressed=false;previous=null;age=duration;if(label){var color=label.color;color.a=0;label.color=color;}}
  void LateUpdate(){Step(PrototypeSession.Instance,Time.unscaledDeltaTime);}
  void OnDisable(){Clear();}
 }
}
