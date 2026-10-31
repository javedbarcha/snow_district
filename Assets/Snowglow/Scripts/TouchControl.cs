using UnityEngine;
using UnityEngine.EventSystems;
namespace Snowglow {
 public sealed class TouchControl : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IDragHandler {
  public string channel;public float value=1;int pointer=int.MinValue;
  void Set(float v){var g=PrototypeSession.Instance;if(g==null)return;var i=g.Input;
   switch(channel){case "steer":i.touchSteer=v;break;case "throttle":i.touchThrottle=v;break;case "brake":i.touchBrake=v;break;case "steerLeft":i.touchLeft=v;break;case "steerRight":i.touchRight=v;break;}}
  public void OnPointerDown(PointerEventData e){if(pointer!=int.MinValue)return;pointer=e.pointerId;if(channel=="steer")OnDrag(e);else Set(value);}
  public void OnPointerUp(PointerEventData e){if(pointer!=e.pointerId)return;Set(0);pointer=int.MinValue;}
  public void OnDrag(PointerEventData e){if(channel!="steer"||e.pointerId!=pointer)return;RectTransform r=(RectTransform)transform;if(RectTransformUtility.ScreenPointToLocalPointInRectangle(r,e.position,e.pressEventCamera,out Vector2 p))Set(Mathf.Clamp(p.x/(r.rect.width*.42f),-1,1));}
  void OnDisable(){Set(0);pointer=int.MinValue;}
 }
}
