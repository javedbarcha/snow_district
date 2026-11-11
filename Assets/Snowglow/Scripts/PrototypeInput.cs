using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
namespace Snowglow {
 public sealed class PrototypeInput : MonoBehaviour {
  [HideInInspector]public float touchSteer,touchThrottle,touchBrake,touchLift,touchTilt,touchLeft,touchRight,touchForward,touchReverse;
  [HideInInspector]public bool touchPedalsUsed;
  public float Steer,Throttle,Brake,Lift,Tilt;public bool Reverse;
  public void Clear(){touchSteer=touchThrottle=touchBrake=touchLift=touchTilt=touchLeft=touchRight=touchForward=touchReverse=0;touchPedalsUsed=false;Steer=Throttle=Brake=Lift=Tilt=0;}
  public void ResolvePedals(bool keyboardForward,bool keyboardReverse,bool keyboardBrake){
   bool forward=keyboardForward||touchForward>.02f,reverse=keyboardReverse||touchReverse>.02f;
   if(forward!=reverse)Reverse=reverse;
   Throttle=forward&&reverse?0:Mathf.Max(touchThrottle,forward||reverse?1:0);
   // Both pedals brake; releasing both leaves the machine coasting.
   Brake=Mathf.Max(touchBrake,keyboardBrake||forward&&reverse?1:0);
  }
  void Update(){
   var game=PrototypeSession.Instance;
   // Arrow/Space gameplay keys must not also navigate/submit the last HUD button.
   var events=EventSystem.current;
   if(events!=null){events.sendNavigationEvents=game==null||!game.IsRunning;if(game!=null&&game.IsRunning&&events.currentSelectedGameObject!=null)events.SetSelectedGameObject(null);}
   var k=Keyboard.current;
   Steer=Mathf.Clamp(touchSteer+touchRight-touchLeft+(k==null?0:(k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0)),-1,1);
   bool forward=k!=null&&(k.wKey.isPressed||k.upArrowKey.isPressed);
   bool reverse=k!=null&&(k.sKey.isPressed||k.downArrowKey.isPressed);
   ResolvePedals(forward,reverse,k!=null&&k.spaceKey.isPressed);
   Lift=Tilt=0; // Equipment lift/curl is automatic; manual controls removed.
   if(k==null||game==null)return;
   if(k.escapeKey.wasPressedThisFrame){if(game.HUD.productionMenu&&!game.IsRunning)game.HUD.productionMenu.Back();else game.TogglePause();}
   if(!game.IsRunning)return;
   if(k.xKey.wasPressedThisFrame&&!forward&&!reverse)Reverse=!Reverse;
   if(k.cKey.wasPressedThisFrame)game.CameraRig.Toggle();
   if(k.eKey.wasPressedThisFrame)game.Active.Tool.ToggleWork();
   if(k.qKey.wasPressedThisFrame)game.Active.Tool.CycleAngle();
   if(k.bKey.wasPressedThisFrame)game.Active.Tool.RequestDump();
   if(k.hKey.wasPressedThisFrame)game.Recover();
  }
 }
}
