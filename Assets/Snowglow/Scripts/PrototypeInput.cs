using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
namespace Snowglow {
 public sealed class PrototypeInput : MonoBehaviour {
  [HideInInspector]public float touchSteer,touchThrottle,touchBrake,touchLift,touchTilt;
  public float Steer,Throttle,Brake,Lift,Tilt;public bool Reverse;
  public void Clear(){touchSteer=touchThrottle=touchBrake=touchLift=touchTilt=0;Steer=Throttle=Brake=Lift=Tilt=0;}
  void Update(){
   var game=PrototypeSession.Instance;
   // Arrow/Space gameplay keys must not also navigate/submit the last HUD button.
   var events=EventSystem.current;
   if(events!=null){events.sendNavigationEvents=game==null||!game.IsRunning;if(game!=null&&game.IsRunning&&events.currentSelectedGameObject!=null)events.SetSelectedGameObject(null);}
   var k=Keyboard.current;
   Steer=Mathf.Clamp(touchSteer+(k==null?0:(k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0)),-1,1);
   bool forward=k!=null&&(k.wKey.isPressed||k.upArrowKey.isPressed);
   bool reverse=k!=null&&(k.sKey.isPressed||k.downArrowKey.isPressed);
   // Direction keys select a gear and apply throttle. The physics controller brakes
   // before changing travel direction, so reversing never snaps vehicle velocity.
   if(forward!=reverse)Reverse=reverse;
   Throttle=forward&&reverse?0:Mathf.Max(touchThrottle,forward||reverse?1:0);
   Brake=Mathf.Max(touchBrake,k!=null&&(k.spaceKey.isPressed||(forward&&reverse))?1:0);
   Lift=Mathf.Clamp(touchLift+(k==null?0:(k.rKey.isPressed?1:0)-(k.fKey.isPressed?1:0)),-1,1);
   Tilt=Mathf.Clamp(touchTilt+(k==null?0:(k.tKey.isPressed?1:0)-(k.gKey.isPressed?1:0)),-1,1);
   if(k==null||game==null)return;
   if(k.escapeKey.wasPressedThisFrame)game.TogglePause();
   if(!game.IsRunning)return;
   if(k.xKey.wasPressedThisFrame&&!forward&&!reverse)Reverse=!Reverse;
   if(k.cKey.wasPressedThisFrame)game.CameraRig.Toggle();
   if(k.eKey.wasPressedThisFrame)game.Active.Tool.ToggleWork();
   if(k.qKey.wasPressedThisFrame)game.Active.Tool.CycleAngle();
   if(k.vKey.wasPressedThisFrame)game.Active.Tool.ToggleAuto();
   if(k.bKey.wasPressedThisFrame)game.Active.Tool.RequestDump();
   if(k.hKey.wasPressedThisFrame)game.Recover();
  }
 }
}
