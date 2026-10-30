using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Snowglow {
 public enum CameraMode { Isometric, Follow, Orbit, Scenic }
 public sealed class CameraRig : MonoBehaviour {
  public Camera view;public Terrain terrain;public CameraMode mode;
  [FormerlySerializedAs("Follow"),SerializeField]bool legacyFollow;
  public float groundClearance=1,collisionRadius=.4f;
  public float orbitDistance=15,orbitPitch=25,scenicDistance=40,scenicPitch=18,scenicSpeed=4;
  public float Yaw {get;private set;}public float Pitch {get;private set;}public float Distance {get;private set;}
  public bool Interactive=>mode==CameraMode.Orbit||mode==CameraMode.Scenic;
  public string ModeName=>mode.ToString().ToUpperInvariant();
  public bool Follow {get=>mode==CameraMode.Follow;set=>SetMode(value?CameraMode.Follow:CameraMode.Isometric);}
  Vector3 velocity;Transform lastTarget;bool initialized,resetRequested=true,mouseCaptured;
  float manualHold,pinchDistance;int pinchA=-1,pinchB=-1;
  readonly Dictionary<int,Vector2> fingers=new Dictionary<int,Vector2>();
  readonly List<int> released=new List<int>();readonly List<RaycastResult> uiHits=new List<RaycastResult>();
  readonly RaycastHit[] obstacles=new RaycastHit[32];PointerEventData pointer;EventSystem pointerEvents;
  const int WorldMask=(1<<0)|(1<<9);
  public void Toggle()=>SetMode((CameraMode)(((int)mode+1)%4));
  public void SetMode(CameraMode value){mode=value;legacyFollow=mode==CameraMode.Follow;resetRequested=true;velocity=Vector3.zero;ClearGesture();}
  public void ResetView(){resetRequested=true;manualHold=0;ClearGesture();}
  public void Zoom(float steps){if(!Interactive||steps==0)return;Distance=Mathf.Clamp(Distance*Mathf.Exp(-steps*.12f),mode==CameraMode.Orbit?7:22,mode==CameraMode.Orbit?42:65);manualHold=6;}
  public void Rotate(Vector2 degrees){if(!Interactive||degrees.sqrMagnitude==0)return;Yaw=Mathf.Repeat(Yaw+degrees.x,360);Pitch=Mathf.Clamp(Pitch+degrees.y,5,75);manualHold=6;}
  void Awake(){if(legacyFollow&&mode==CameraMode.Isometric)mode=CameraMode.Follow;}
  void OnDisable()=>ClearGesture();
  void OnApplicationFocus(bool focused){if(!focused)ClearGesture();}
  void ClearGesture(){mouseCaptured=false;fingers.Clear();pinchDistance=0;pinchA=pinchB=-1;}
  bool OverUI(Vector2 position){
   var events=EventSystem.current;if(!events)return false;
   if(pointer==null||pointerEvents!=events){pointer=new PointerEventData(events);pointerEvents=events;}
   pointer.position=position;uiHits.Clear();events.RaycastAll(pointer,uiHits);return uiHits.Count>0;
  }
  void ReadInput(float dt){
   var k=Keyboard.current;if(k!=null){
    Rotate(new Vector2((k.lKey.isPressed?1:0)-(k.jKey.isPressed?1:0),(k.kKey.isPressed?1:0)-(k.iKey.isPressed?1:0))*70*dt);
    float zoom=(k.equalsKey.isPressed||k.numpadPlusKey.isPressed?1:0)-(k.minusKey.isPressed||k.numpadMinusKey.isPressed?1:0);if(zoom!=0)Zoom(zoom*5*dt);
    if(k.homeKey.wasPressedThisFrame)ResetView();
   }
   // A finger beginning on a driving/HUD control never becomes a camera finger.
   var screen=Touchscreen.current;bool touching=false;int first=-1,second=-1;Vector2 a=default,b=default,previousA=default;bool aKnown=false;
   if(screen!=null){
    released.Clear();foreach(var pair in fingers)released.Add(pair.Key);
    foreach(var touch in screen.touches){if(!touch.press.isPressed)continue;touching=true;int id=touch.touchId.ReadValue();var position=touch.position.ReadValue();
     bool known=fingers.TryGetValue(id,out var previous);
     if(!known&&(touch.phase.ReadValue()!=UnityEngine.InputSystem.TouchPhase.Began||OverUI(position)))continue;
     fingers[id]=position;released.Remove(id);
     if(first<0){first=id;a=position;previousA=previous;aKnown=known;}else if(second<0){second=id;b=position;}
    }
    foreach(int id in released)fingers.Remove(id);
   }
   float pixels=Mathf.Max(240,Screen.height);
   if(second>=0){
    float span=Vector2.Distance(a,b);if(pinchA==first&&pinchB==second&&pinchDistance>10&&span>10)Zoom(Mathf.Log(span/pinchDistance)/.12f);
    pinchA=first;pinchB=second;pinchDistance=span;
   }else{
    if(first>=0&&aKnown&&pinchDistance==0){var delta=(a-previousA)/pixels;Rotate(new Vector2(delta.x*180,-delta.y*110));}
    pinchDistance=0;pinchA=pinchB=-1;
   }
   var mouse=Mouse.current;if(mouse==null||touching){mouseCaptured=false;return;}
   if(mouse.rightButton.wasPressedThisFrame)mouseCaptured=!OverUI(mouse.position.ReadValue());
   if(!mouse.rightButton.isPressed)mouseCaptured=false;
   if(mouseCaptured){var delta=mouse.delta.ReadValue()/pixels;Rotate(new Vector2(delta.x*180,-delta.y*110));}
   float scroll=mouse.scroll.ReadValue().y;
   if(InputSystem.settings.scrollDeltaBehavior==InputSettings.ScrollDeltaBehavior.KeepPlatformSpecificInputRange&&(Application.platform==RuntimePlatform.WindowsEditor||Application.platform==RuntimePlatform.WindowsPlayer))scroll/=120;
   if(scroll!=0&&!OverUI(mouse.position.ReadValue()))Zoom(scroll);
  }
  float Ground(Vector3 position){
   if(!terrain)terrain=Terrain.activeTerrain;
   return terrain?terrain.SampleHeight(position)+terrain.transform.position.y:SnowField.Ground;
  }
  Vector3 AvoidObstacles(Transform target,Vector3 focus,Vector3 position){
   var offset=position-focus;float distance=offset.magnitude;if(distance<.001f)return position;
   int count=Physics.SphereCastNonAlloc(focus,collisionRadius,offset/distance,obstacles,distance,WorldMask,QueryTriggerInteraction.Ignore);float nearest=distance;
   for(int i=0;i<count;i++)if(obstacles[i].collider&&!obstacles[i].collider.transform.IsChildOf(target))nearest=Mathf.Min(nearest,Mathf.Max(.75f,obstacles[i].distance-.15f));
   return focus+offset/distance*nearest;
  }
  Vector3 AboveGround(Vector3 position,Quaternion rotation){
   float lift=Ground(position)+Mathf.Max(.5f,groundClearance)-position.y;
   float halfY=view.nearClipPlane*Mathf.Tan(view.fieldOfView*Mathf.Deg2Rad*.5f),halfX=halfY*view.aspect;
   if(view.orthographic){halfY=view.orthographicSize;halfX=halfY*view.aspect;}
   // Protect the near-plane corners too, including on steep terrain and wide screens.
   for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2){var corner=position+rotation*new Vector3(x*halfX,y*halfY,view.nearClipPlane);lift=Mathf.Max(lift,Ground(corner)+.2f-corner.y);}
   if(lift>0)position.y+=lift;return position;
  }
  void LateUpdate(){var g=PrototypeSession.Instance;if(g==null||g.Active==null||!view)return;UpdateForTarget(g.Active.transform,Time.unscaledDeltaTime,g.IsRunning);}
  public void UpdateForTarget(Transform target,float dt,bool controls){
   if(!view||!target)return;dt=Mathf.Clamp(dt,.0001f,.1f);bool switched=lastTarget!=target;lastTarget=target;
   if(resetRequested||switched){Yaw=Mathf.Repeat(target.eulerAngles.y+(mode==CameraMode.Scenic?45:0),360);Pitch=Mathf.Clamp(mode==CameraMode.Scenic?scenicPitch:orbitPitch,5,75);Distance=Mathf.Clamp(mode==CameraMode.Scenic?scenicDistance:orbitDistance,mode==CameraMode.Scenic?22:7,mode==CameraMode.Scenic?65:42);manualHold=0;resetRequested=false;ClearGesture();}
   if(Interactive&&controls){ReadInput(dt);if(!resetRequested&&mode==CameraMode.Scenic&&manualHold<=0)Yaw=Mathf.Repeat(Yaw+scenicSpeed*dt,360);manualHold=Mathf.Max(0,manualHold-dt);}else ClearGesture();
   view.orthographic=mode==CameraMode.Isometric;view.orthographicSize=Mathf.Max(17,22/Mathf.Max(.6f,view.aspect));view.fieldOfView=mode==CameraMode.Orbit?52:mode==CameraMode.Scenic?58:60;
   Vector3 focus=target.position+Vector3.up*1.6f;
   Vector3 wanted=mode==CameraMode.Isometric?target.position+new Vector3(-24,30,-24):mode==CameraMode.Follow?target.position-target.forward*13+Vector3.up*6:focus+Quaternion.Euler(Pitch,Yaw,0)*Vector3.back*Distance;
   wanted=AvoidObstacles(target,focus,wanted);Quaternion looking=mode==CameraMode.Isometric?Quaternion.Euler(41,45,0):Quaternion.LookRotation(focus-wanted);
   wanted=AboveGround(wanted,looking);
   Vector3 position=(!initialized||switched||(transform.position-wanted).sqrMagnitude>160*160)?wanted:Vector3.SmoothDamp(transform.position,wanted,ref velocity,.18f,1000,dt);
   position=AvoidObstacles(target,focus,position);looking=mode==CameraMode.Isometric?Quaternion.Euler(41,45,0):Quaternion.LookRotation(focus-position);
   var rotation=initialized?Quaternion.Slerp(transform.rotation,looking,1-Mathf.Exp(-dt*9)):looking;
   for(int pass=0;pass<3;pass++)position=AboveGround(position,rotation);
   transform.SetPositionAndRotation(position,rotation);initialized=true;
  }
 }
}
